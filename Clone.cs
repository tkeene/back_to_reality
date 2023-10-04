using Godot;
using System;

public class Clone : Spatial
{
    [Export]
    public float walkSpeed = 12.0f;
    [Export]
    public float carrySpeed = 2.0f;
    [Export]
    public float followPlayerSpeed = 3.0f;
    [Export]
    public float timeBetweenWorkAnimations = 1.0f;
    [Export]
    public float timeBetweenLightColorChanges = 1.0f;

    static AudioStream audioClipBzzt = ResourceLoader.Load<AudioStream>("res://Audio/bzzt.wav");
    static AudioStream audioClipClink = ResourceLoader.Load<AudioStream>("res://Audio/clink.wav");
    static AudioStream audioClipHuh = ResourceLoader.Load<AudioStream>("res://Audio/huh.wav");
    static AudioStream audioClipHup = ResourceLoader.Load<AudioStream>("res://Audio/hup.wav");
    static AudioStream audioClipWoohoo = ResourceLoader.Load<AudioStream>("res://Audio/woohoo.wav");
    static AudioStream audioClipYeah = ResourceLoader.Load<AudioStream>("res://Audio/yeah.wav");

    private AnimatedSprite3D myRenderer;
    private NavigationAgent myNavigationAgent;

    public enum EAiState
    {
        FirstFrame,
        WaitingForWakeup,
        FollowingPlayer,
        LookingForTask,
        BreakingObject,
        CarryingObject,
        DebugNavigateBackToPod,
    }
    public EAiState currentAiState = EAiState.WaitingForWakeup;
    private Vector2 currentAnimatorFacing;
    private Vector3 currentCommandTargetPosition;
    private Destroyable currentDestroyableTarget = null;
    private ShipPart currentCarriedObject = null;
    private float currentWorkAnimationTimer = 0.0f;

    private float lastFrameDelta = 0.0f;
    private static RandomNumberGenerator rng = new RandomNumberGenerator();
    const float kWakeupDistance = 3.0f;
    const float kFollowMaxDistance = 8.0f;
    private float timeToNextLightColorChange = 0.0f;
    private string myPersonality = "???";

    public override void _Ready()
    {
        myNavigationAgent = GetNode<NavigationAgent>("NavigationAgent");
        myRenderer = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        currentAnimatorFacing = Vector2.Up.Rotated(rng.RandfRange(0.0f, Mathf.Pi));
        SetCurrentAiState(EAiState.FirstFrame);

        myPersonality = PersonalitiesList.GetNextPersonality(GameManager.currentClonePersonality);
        GameManager.currentClonePersonality++;

        // https://godotforums.org/d/36037-solved-navigationobstacle-ignored-by-navigationagent/2
        // Apparently navigation agents will adjust their velocity, but it's not really accessible in 3.6.
        // This was apparently fixed in 4.1.
        myNavigationAgent.Connect("velocity_computed", this, "Signal_OnVelocityComputed");
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        switch (currentAiState)
        {
            case EAiState.FirstFrame:
                GameManager.instance.totalClonesInGame++;
                SetCurrentAiState(EAiState.WaitingForWakeup);
                break;
            case EAiState.WaitingForWakeup:
                if (Translation.DistanceTo(PlayerController.instance.Translation) < kWakeupDistance)
                {
                    SetCurrentAiState(EAiState.FollowingPlayer);
                    PlaySound(audioClipWoohoo);
                }
                break;
            case EAiState.FollowingPlayer:
                // TODO Walking animation facing
                if (Translation.DistanceTo(PlayerController.instance.Translation) > kFollowMaxDistance)
                {
                    Translation = (Translation + PlayerController.instance.Translation) * 0.5f;
                    Translation += new Vector3(rng.RandfRange(-2.0f, 2.0f),
                        0.0f,
                        rng.RandfRange(-2.0f, 2.0f));
                    FaceAnimatorTowardsTarget(PlayerController.instance.Translation - Translation, "move_");
                }
                else
                {
                    Vector3 followTarget = PlayerController.instance.Translation - GameManager.instance.gameCamera.Transform.basis.z * 0.2f;
                    Translation = Translation.MoveToward(followTarget, delta * followPlayerSpeed);
                }
                break;
            case EAiState.LookingForTask:
                Translation = Translation.MoveToward(currentCommandTargetPosition, delta * walkSpeed);
                if (Translation == currentCommandTargetPosition)
                {
                    bool foundSomething = false;
                    foreach (ShipPart shipPart in ShipPart.allShipParts)
                    {
                        if (IsInstanceValid(shipPart) && shipPart != null
                            && !shipPart.isRescued && shipPart.GlobalTranslation.DistanceTo(GlobalTranslation) < 4.0f)
                        {
                            currentCarriedObject = shipPart;
                            shipPart.currentlyCarriedBy.Add(this);
                            SetCurrentAiState(EAiState.CarryingObject);
                            foundSomething = true;
                            break;
                        }
                    }
                    if (!foundSomething && KoboldsKeep.Utils.GetOverlappingColliders(GetWorld().DirectSpaceState, Translation,
                        radius: 1.0f, GameManager.kInteractionMask, false, out Godot.Collections.Array results) > 0)
                    {
                        foreach (object hitResult in results)
                        {
                            object collider = (hitResult as Godot.Collections.Dictionary)["collider"];
                            if (Destroyable.collidersToDestroyable.TryGetValue(collider, out Destroyable targetDestroyable))
                            {
                                currentDestroyableTarget = targetDestroyable;
                                SetCurrentAiState(EAiState.BreakingObject);
                                //GD.Print("Destroying " + currentDestroyableTarget.Name);
                                foundSomething = true;
                                break;
                            }
                            else
                            {
                                GD.Print("Clone doesn't know what to do with a " + collider.GetType().ToString());
                            }
                        }
                    }
                    if (!foundSomething)
                    {
                        Vector3 playerToClone = Translation - PlayerController.instance.Translation;
                        SetCurrentAiState(EAiState.FollowingPlayer);
                        if (playerToClone.Length() > kFollowMaxDistance)
                        {
                            playerToClone = playerToClone.Normalized() * kFollowMaxDistance * 0.9f;
                        }
                        Translation = PlayerController.instance.Translation + playerToClone;
                        PlaySound(audioClipHuh);
                    }
                }
                break;
            case EAiState.BreakingObject:
                if (IsInstanceValid(currentDestroyableTarget) && currentDestroyableTarget != null)
                {
                    currentDestroyableTarget.currentHealth -= delta;
                    currentWorkAnimationTimer -= delta;
                    if (currentWorkAnimationTimer < 0.0f)
                    {
                        currentWorkAnimationTimer = timeBetweenWorkAnimations * rng.RandfRange(0.8f, 1.2f);
                        float distanceToWorkFrom = currentDestroyableTarget.radius;
                        float positionRadians = rng.RandfRange(0.0f, Mathf.Pi * 2.0f);
                        Vector3 offsetFromTarget = Vector3.Right.Rotated(Vector3.Up, positionRadians) * distanceToWorkFrom;
                        GlobalTranslation = currentDestroyableTarget.GlobalTranslation + offsetFromTarget + Vector3.Up * 2.0f;
                        FaceAnimatorTowardsTarget(offsetFromTarget, "work_");
                        PlaySound(audioClipClink);
                    }
                }
                else
                {
                    currentDestroyableTarget = null;
                    SetCurrentAiState(EAiState.FollowingPlayer);
                    Translation = PlayerController.instance.Translation + Vector3.Up * kFollowMaxDistance * 0.5f;
                }
                break;
            case EAiState.CarryingObject:
                if (IsInstanceValid(currentCarriedObject) && currentCarriedObject != null
                    && currentCarriedObject.GlobalTranslation.DistanceTo(GlobalTranslation) > 5.0f)
                {
                    // Dont get too far away from the carried object
                    GlobalTranslation = (GlobalTranslation + currentCarriedObject.GlobalTranslation) * 0.5f;
                }
                break;
            case EAiState.DebugNavigateBackToPod:
                if (GlobalTranslation.DistanceTo(EscapePod.instance.GlobalTranslation) <= 5.0f)
                {
                    SetCurrentAiState(EAiState.FollowingPlayer);
                    float distance = 0.9f * kFollowMaxDistance;
                    GlobalTranslation += new Vector3(rng.RandfRange(-distance, distance), 0.0f, rng.RandfRange(-distance, distance));
                }
                break;
        }
        timeToNextLightColorChange -= delta;
        if (timeToNextLightColorChange <= 0.0f)
        {
            timeToNextLightColorChange = timeBetweenLightColorChanges * rng.RandfRange(0.75f, 1.25f);
            Color color = Color.FromHsv(rng.RandfRange(0.0f, 1.0f), rng.RandfRange(0.25f, 0.75f), 1.0f);
            GetNode<Light>("OmniLight").LightColor = color;
            GetNode<AnimatedSprite3D>("AnimatedSprite3D").Modulate = color;
        }

        // TODO Remove these debug cheat commands
        //if (currentAiState == EAiState.FollowingPlayer
        //    && Input.IsKeyPressed((int)KeyList.F5))
        //{
        //    SetCurrentAiState(EAiState.DebugNavigateBackToPod);
        //}
        //if (currentAiState == EAiState.WaitingForWakeup
        //    && Input.IsKeyPressed((int)KeyList.F6))
        //{
        //    SetCurrentAiState(EAiState.FollowingPlayer);
        //}
    }

    public override void _PhysicsProcess(float delta)
    {
        lastFrameDelta = delta;
        if (Input.IsKeyPressed((int)KeyList.U))
        {
            GD.Print("Navigating to " + PlayerController.instance.Translation.ToString());
            myNavigationAgent.SetTargetLocation(PlayerController.instance.Translation);
        }

        // https://godotforums.org/d/36037-solved-navigationobstacle-ignored-by-navigationagent/2
        if (!myNavigationAgent.IsNavigationFinished())
        {
            Vector3 nextPosition = myNavigationAgent.GetNextLocation();
            Vector3 moveOffset = nextPosition - GlobalTranslation;
            Vector3 navigationAgentVelocity = moveOffset.Normalized() * walkSpeed;
            myNavigationAgent.SetVelocity(navigationAgentVelocity);
            navigationAgentVelocity *= delta;
        }
    }

    // https://godotforums.org/d/36037-solved-navigationobstacle-ignored-by-navigationagent/2
    public void Signal_OnVelocityComputed(Vector3 velocity)
    {
        if (!myNavigationAgent.IsNavigationFinished()
            && (currentAiState == EAiState.CarryingObject || currentAiState == EAiState.DebugNavigateBackToPod))
        {
            // This is a bit of a hack, but if we jitter the step size they'll be more likely to eventually get around obstacles.
            if (rng.RandiRange(0, 15) == 0)
            {
                velocity *= rng.RandiRange(2, 8);
                if (lastFrameDelta * velocity.Length() > 0.5f)
                {
                    velocity = velocity.Normalized() * 0.5f / lastFrameDelta;
                }
            }
            else
            {
                velocity *= rng.RandfRange(0.5f, 1.5f);
            }

            float rotationForAvoidanceRadians = Mathf.Deg2Rad(60.0f);
            Vector3 newPosition = Translation + velocity * lastFrameDelta;
            Vector3 rightPosition = Translation + velocity.Rotated(Vector3.Up, rotationForAvoidanceRadians) * lastFrameDelta;
            Vector3 leftPosition = Translation + velocity.Rotated(Vector3.Up, -rotationForAvoidanceRadians) * lastFrameDelta;
            float backstepSign = (rng.RandiRange(0, 1) % 2 == 0) ? 1 : -1;
            Vector3 backPositionA = Translation + velocity.Rotated(Vector3.Up, rotationForAvoidanceRadians * backstepSign * 2.0f) * lastFrameDelta;
            Vector3 backPositionB = Translation + velocity.Rotated(Vector3.Up, -rotationForAvoidanceRadians * backstepSign * 2.0f) * lastFrameDelta;
            Vector3 backPositionC = Translation - velocity * lastFrameDelta;
            for (int i = 0; i < 6; i++)
            {
                Vector3 positionToCheck = newPosition;
                switch (i)
                {
                    case 1:
                        positionToCheck = rightPosition;
                        break;
                    case 2:
                        positionToCheck = leftPosition;
                        break;
                    case 3:
                        positionToCheck = backPositionA;
                        break;
                    case 4:
                        positionToCheck = backPositionB;
                        break;
                    case 5:
                        positionToCheck = backPositionC;
                        break;
                }
                if (KoboldsKeep.Utils.Raycast(GetWorld().DirectSpaceState, positionToCheck, Vector3.Down,
                    GameManager.kTerrainMask, queryAreas: false, out Vector3 hitPoint, out object hitObject))
                {
                    Translation = hitPoint + Vector3.Up * 0.5f;
                    break;
                }
            }
        }
    }

    private void SetCurrentAiState(EAiState newState)
    {
        switch (newState)
        {
            case EAiState.FirstFrame:
            case EAiState.WaitingForWakeup:
                myNavigationAgent.SetTargetLocation(Translation);
                myNavigationAgent.MaxSpeed = walkSpeed;
                myRenderer.Animation = "idle_" + KoboldsKeep.Utils.GetCardinalDirection(currentAnimatorFacing);
                break;
            case EAiState.FollowingPlayer:
                myNavigationAgent.MaxSpeed = 0.0f;
                Translation = PlayerController.instance.Translation;
                GameManager.instance.OnCloneRecruited(this);
                break;
            case EAiState.LookingForTask:
                myNavigationAgent.MaxSpeed = 0.0f;
                // TODO Walking animation facing
                break;
            case EAiState.BreakingObject:
                myNavigationAgent.MaxSpeed = 0.0f;
                currentWorkAnimationTimer = 0.0f; // Immediately move to animate.
                break;
            case EAiState.CarryingObject:
                myNavigationAgent.MaxSpeed = carrySpeed;
                myNavigationAgent.SetTargetLocation(EscapePod.instance.Translation);
                break;
            case EAiState.DebugNavigateBackToPod:
                myNavigationAgent.MaxSpeed = walkSpeed;
                myNavigationAgent.SetTargetLocation(EscapePod.instance.Translation);
                break;
        }
        currentAiState = newState;
    }

    public void OnCommandedToFindWork(Vector3 targetPosition)
    {
        currentCommandTargetPosition = targetPosition;
        SetCurrentAiState(EAiState.LookingForTask);
        PlaySound(audioClipHup);
    }

    private void FaceAnimatorTowardsTarget(Vector3 worldOffsetFromTarget, string animationPrefix)
    {
        Vector3 cameraForward = -GameManager.instance.gameCamera.Transform.basis.z;
        cameraForward.y = 0.0f;
        Vector3 forwardProjection = worldOffsetFromTarget.Project(cameraForward);
        Vector3 rightProjection = worldOffsetFromTarget.Project(GameManager.instance.gameCamera.Transform.basis.x);
        currentAnimatorFacing = new Vector2(rightProjection.Length(), forwardProjection.Length());
        myRenderer.Animation = animationPrefix + KoboldsKeep.Utils.GetCardinalDirection(currentAnimatorFacing);
    }

    public void OnRecallOrdered()
    {
        currentDestroyableTarget = null;
        if (IsInstanceValid(currentCarriedObject) && currentCarriedObject != null)
        {
            currentCarriedObject.OnCloneStoppedCarrying(this);
        }
        currentCarriedObject = null;
        Vector3 currentPosition = GlobalTranslation;
        SetCurrentAiState(EAiState.FollowingPlayer);
        Vector3 offset = currentPosition - PlayerController.instance.GlobalTranslation;
        float distance = Mathf.Min(offset.Length(), kFollowMaxDistance * 0.9f);
        GlobalTranslation = PlayerController.instance.GlobalTranslation + offset.Normalized() * distance;
        PlaySound(audioClipBzzt);
    }

    public bool CanBeRecalled()
    {
        return currentAiState == EAiState.BreakingObject
            || currentAiState == EAiState.CarryingObject
            || currentAiState == EAiState.DebugNavigateBackToPod;
    }

    public void SetNumberOfClonesHelpingCarry(int count)
    {
        myNavigationAgent.MaxSpeed = count * carrySpeed;
    }

    public void OnPartDelivered()
    {
        SetCurrentAiState(EAiState.FollowingPlayer);
        PlaySound(audioClipYeah);
    }

    public string GetPersonality()
    {
        string value = "";
        bool firstLine = true;
        foreach (string bit in myPersonality.Split(';'))
        {
            if (firstLine)
            {
                value += "- ";
                value += bit;
                value += " -\n";
            }
            else
            {
                value += bit;
                value += "\n";
            }
            firstLine = false;
        }
        return value;
    }

    public void PlaySound(AudioStream stream)
    {
        AudioStreamPlayer3D audio = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
        audio.Stream = stream;
        audio.PitchScale = rng.RandfRange(0.7f, 3.0f);
        audio.Play();
    }
}
