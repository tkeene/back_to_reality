using Godot;
using System;
using System.Collections.Generic;

public class GameManager : Node
{
    [Export]
    private PackedScene healthBarPrefab = null;
    [Export]
    public int numberOfTitleCards = 2;
    [Export]
    public float defaultCameraZoom = 20.0f;
    [Export]
    public float zoomedOutCameraZoom = 40.0f;
    [Export]
    public float cameraZoomDurationSeconds = 0.5f;
    [Export]
    public List<string> tutorialCards;

    public static GameManager instance;
    public const uint kTerrainMask = 0x01;
    public const uint kInteractionMask = 0x02;
    public const uint kGravityWellMask = 0x04;
    public enum EGameState
    {
        TitleCards,
        Playing,
        Paused,
        EndOfGameCard
    }
    public EGameState currentGameState = EGameState.TitleCards;

    public Camera gameCamera;
    public bool wasLeftMouseHeldLastFrame = false;
    public bool isLeftMouseHeldThisFrame = false;
    public bool leftMouseWasClickedDownThisFrame = false;
    public bool leftMouseWasReleasedThisFrame = false;
    public bool wasRightMouseHeldLastFrame = false;
    public bool isRightMouseHeldThisFrame = false;
    public bool rightMouseWasClickedDownThisFrame = false;
    public bool rightMouseWasReleasedThisFrame = false;

    public Vector2 currentMousePosition = Vector2.Zero;

    private int currentFrameCount = 0;
    private int currentTitleCard = 0;
    public int currentTutorialStep = 0;
    private float currentTutorialTimer = 0.0f;
    public bool shouldUpdateNavmeshNextFrame = true;
    private bool resetPressedLastFrame = false;

    private List<Clone> rescuedClones = new List<Clone>();
    private List<Clone> currentClonesInPlayerParty = new List<Clone>();
    private int tutorialRecalledClonesCount = 0;
    public int totalClonesInGame = 0;
    public int partsRecovered = 0;
    private int currentCommandCombo = 0;
    private float remainingSecondsForCommandCombo = 0.0f;
    private Queue<Vector3> currentQueuedCommands = new Queue<Vector3>();
    private Dictionary<Spatial, HealthBar> spawnedHealthBars = new Dictionary<Spatial, HealthBar>();
    private float currentGameTime = 0.0f;

    public static int currentClonePersonality = 0;

    public override void _Ready()
    {
        instance = this;
        gameCamera = GetNode<Camera>("Camera");
        SetCurrentGameState(EGameState.TitleCards);
        RefreshCloneHud();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            currentMousePosition = mouseMotionEvent.Position;
        }
    }

    public override void _Process(float delta)
    {
        currentFrameCount++;
        if (currentFrameCount > 30)
        {
            wasLeftMouseHeldLastFrame = isLeftMouseHeldThisFrame;
            isLeftMouseHeldThisFrame = (Input.GetMouseButtonMask() & (int)ButtonList.MaskLeft) != 0;
            leftMouseWasClickedDownThisFrame = !wasLeftMouseHeldLastFrame && isLeftMouseHeldThisFrame;
            leftMouseWasReleasedThisFrame = wasLeftMouseHeldLastFrame && !isLeftMouseHeldThisFrame;

            wasRightMouseHeldLastFrame = isRightMouseHeldThisFrame;
            isRightMouseHeldThisFrame = (Input.GetMouseButtonMask() & (int)ButtonList.MaskRight) != 0;
            rightMouseWasClickedDownThisFrame = !wasRightMouseHeldLastFrame && isRightMouseHeldThisFrame;
            rightMouseWasReleasedThisFrame = wasRightMouseHeldLastFrame && !isRightMouseHeldThisFrame;
        }
        if (currentTutorialStep >= 2)
        {
            currentGameTime += delta;
        }

        switch (currentGameState)
        {
            case EGameState.TitleCards:
                if (leftMouseWasClickedDownThisFrame)
                {
                    currentTitleCard++;
                    GetNode<Node>("HUD/IntroCard" + currentTitleCard).QueueFree();
                    if (!HasNode("HUD/IntroCard" + (currentTitleCard + 1)))
                    {
                        SetCurrentGameState(EGameState.Playing);
                    }
                    AudioStreamPlayer musicPlayer = GetNode<AudioStreamPlayer>("GameMusic");
                    if (!musicPlayer.Playing)
                    {
                        musicPlayer.Play();
                    }
                }
                break;
            case EGameState.Playing:
                Update_Playing(delta);
                break;
        }

        if (currentGameState != EGameState.TitleCards)
        {
            bool resetPressedThisFrame = Input.IsKeyPressed((int)KeyList.F12);
            if (!resetPressedThisFrame && resetPressedLastFrame)
            {
                GetTree().ReloadCurrentScene();
                currentClonePersonality = 0;
                ShipPart.allShipParts.Clear();
            }
            else
            {
                resetPressedLastFrame = resetPressedThisFrame;
            }
        }

    }

    private void Update_Playing(float delta)
    {
        // Debug test to see how navmesh updating works
        if (shouldUpdateNavmeshNextFrame)
        {
            // https://ask.godotengine.org/26104/how-can-you-get-navigation2d-recognize-updates-the-nav-mesh
            NavigationPolygonInstance navigationPolygonInstance = new NavigationPolygonInstance();
            navigationPolygonInstance.Enabled = false;
            navigationPolygonInstance.Enabled = true;
            navigationPolygonInstance.QueueFree();
        }

        {
            switch (currentTutorialStep)
            {
                case 0:
                    // Press the Zoom button
                    if (Input.IsActionPressed("zoom"))
                    {
                        currentTutorialTimer += delta;
                        if (currentTutorialTimer > 1.0f)
                        {
                            currentTutorialTimer = 0.0f;
                            currentTutorialStep++;
                            RefreshTutorialLabels();
                        }
                    }
                    break;
                case 1:
                    // Release the Zoom button
                    if (!Input.IsActionPressed("zoom"))
                    {
                        currentTutorialTimer += delta;
                        if (currentTutorialTimer > 0.66f)
                        {
                            currentTutorialTimer = 0.0f;
                            currentTutorialStep++;
                            RefreshTutorialLabels();
                        }
                    }
                    break;
                case 2:
                    // Move
                    if (Input.IsActionPressed("ui_right") || Input.IsActionPressed("ui_down"))
                    {
                        currentTutorialTimer += delta;
                        if (currentTutorialTimer > 2.0f)
                        {
                            currentTutorialTimer = 0.0f;
                            currentTutorialStep++;
                            RefreshTutorialLabels();
                        }
                    }
                    break;
                case 3:
                    // Find a clone
                    if (rescuedClones.Count >= 1)
                    {
                        currentTutorialStep++;
                        RefreshTutorialLabels();
                    }
                    break;
                case 4:
                    // Add a clone to your party
                    if (rescuedClones.Count >= 2)
                    {
                        currentTutorialStep++;
                        RefreshTutorialLabels();
                        currentTutorialTimer = 0.0f;
                    }
                    break;
                case 5:
                    // Task clones with working
                    if (currentQueuedCommands.Count > 0)
                    {
                        currentTutorialTimer += 0.5f;
                    }
                    if (currentTutorialTimer >= 1.0f)
                    {
                        currentTutorialStep++;
                        RefreshTutorialLabels();
                        currentTutorialTimer = 0.0f;
                    }
                    break;
                case 6:
                    // You can also cancel clones' jobs
                    if (tutorialRecalledClonesCount >= 1)
                    {
                        currentTutorialStep++;
                        RefreshTutorialLabels();
                        currentTutorialTimer = 0.0f;
                    }
                    break;
                case 7:
                    currentTutorialTimer += delta;
                    if (currentTutorialTimer > 15.0f)
                    {
                        currentTutorialStep++;
                        RefreshTutorialLabels();
                    }
                    break;
            }
        }

        if (currentQueuedCommands.Count > 0)
        {
            if (currentClonesInPlayerParty.Count <= 0)
            {
                currentQueuedCommands.Clear();
            }
            else
            {
                Vector3 targetPosition = currentQueuedCommands.Dequeue();

                Clone commandedClone = currentClonesInPlayerParty[0];
                currentClonesInPlayerParty.RemoveAt(0);
                commandedClone.OnCommandedToFindWork(targetPosition);
                RefreshCloneHud();
            }
        }
        else if (remainingSecondsForCommandCombo >= 0.0f)
        {
            remainingSecondsForCommandCombo -= delta;
        }
        else
        {
            currentCommandCombo = 0;
        }

        if (PlayerController.instance.GlobalTranslation.DistanceTo(EscapePod.instance.GlobalTranslation) <= 6.0f
            && Input.IsKeyPressed((int)KeyList.E))
        {
            Victory();
        }
    }

    public void Victory()
    {
        SetCurrentGameState(EGameState.EndOfGameCard);
        GetNode<CanvasItem>("HUD/ScoreScreenCard").Visible = true;
        bool allParts = partsRecovered >= ShipPart.allShipParts.Count;
        bool allClones = rescuedClones.Count >= totalClonesInGame;
        GetNode<Label>("HUD/ScoreScreenCard/Score").Text =
            "Parts: " + partsRecovered + " / " + ShipPart.allShipParts.Count + (allParts ? " - SUCCESS!!" : "")
            + "\nSelves: " + rescuedClones.Count + " / " + totalClonesInGame + (allClones ? " - SUCCESS!!" : "")
            + "\nTime: " + currentGameTime.ToString("0.00") + " seconds";
        bool wasOneHundredPercent = allParts && allClones;
        if (wasOneHundredPercent)
        {
            GetNode<Label>("HUD/ScoreScreenCard/Prompt").Text = "CONGRATULATIONS!!";
        }
        if (allParts && allClones)
        {
            GetNode<Label>("HUD/ScoreScreenCard/Message").Text = "Great teamwork, me!";
        }
        else if (rescuedClones.Count > partsRecovered * 3 || partsRecovered == 0)
        {
            GetNode<Label>("HUD/ScoreScreenCard/Message").Text = "Not enough parts. Not enough seats. Not enough space.\nSometimes you have to sacrifice\npart of yourself to become a better\nversion of yourself.";
        }
        else
        {
            GetNode<Label>("HUD/ScoreScreenCard/Message").Text = "I managed to recover 69% of myself.\nNice!";
        }
    }

    public void GameOver()
    {
        SetCurrentGameState(EGameState.EndOfGameCard);
        GetNode<CanvasItem>("HUD/GameOverCard").Visible = true;
    }

    private void SetCurrentGameState(EGameState newState)
    {
        switch (newState)
        {
            case EGameState.TitleCards:
                Engine.TimeScale = 0;
                break;
            case EGameState.Playing:
                Engine.TimeScale = 1;
                RefreshTutorialLabels();
                break;
            case EGameState.Paused:
                Engine.TimeScale = 0;
                break;
            case EGameState.EndOfGameCard:
                Engine.TimeScale = 0;
                break;
        }
        currentGameState = newState;
    }

    private void RefreshTutorialLabels()
    {
        if (currentTutorialStep >= tutorialCards.Count)
        {
            GetNode<RichTextLabel>("HUD/TutorialLabel").BbcodeText = "";
            GetNode<Control>("HUD/TutorialBackground").Visible = false;
        }
        else
        {
            GetNode<RichTextLabel>("HUD/TutorialLabel").BbcodeText = tutorialCards[currentTutorialStep].Replace("\\n", "\n");
            GetNode<Control>("HUD/TutorialBackground").Visible = true;
        }
    }

    public void RefreshCloneHud()
    {
        GetNode<Label>("HUD/MinionLabel").Text =
            (currentClonesInPlayerParty.Count > 0 ? currentClonesInPlayerParty[0].GetPersonality() : "---\n")
            + "\nSquad: " + currentClonesInPlayerParty.Count
            + " / " + rescuedClones.Count
            + " / " + totalClonesInGame
            + "\nParts: " + partsRecovered
            + " / " + ShipPart.allShipParts.Count;
    }

    public void OnCloneRecruited(Clone clone)
    {
        if (!currentClonesInPlayerParty.Contains(clone))
        {
            currentClonesInPlayerParty.Insert(0, clone);
        }
        if (!rescuedClones.Contains(clone))
        {
            rescuedClones.Add(clone);
        }
        RefreshCloneHud();
    }

    public void OnPlayerCommandIssued()
    {
        remainingSecondsForCommandCombo = 0.5f;
        currentCommandCombo++;
        Vector3? clickedPoint = GetWorldspaceMousePositionParallelToPlayer();
        if (clickedPoint.HasValue)
        {
            Vector3 offset = clickedPoint.Value - PlayerController.instance.Translation;
            const float kMaximumCommandDistance = 5.0f;
            if (offset.Length() > kMaximumCommandDistance)
            {
                offset = offset.Normalized() * kMaximumCommandDistance;
                clickedPoint = PlayerController.instance.Translation + offset;
            }

            int clonesToCommand = currentCommandCombo * 2 - 1;
            for (int i = 0; i < clonesToCommand; i++)
            {
                currentQueuedCommands.Enqueue(clickedPoint.Value);
            }
        }
    }

    public void OnPlayerRecallOrdered()
    {
        Vector3? clickedPoint = GetWorldspaceMousePositionParallelToPlayer();
        if (clickedPoint.HasValue)
        {
            foreach (Clone clone in rescuedClones)
            {
                if (clone.CanBeRecalled())
                {
                    if (clickedPoint.Value.DistanceTo(clone.GlobalTranslation) <= 5.0f)
                    {
                        clone.OnRecallOrdered();
                        tutorialRecalledClonesCount++;
                    }
                }
            }
        }
    }

    private Vector3? GetWorldspaceMousePositionParallelToPlayer()
    {
        Plane worldPlane = new Plane(PlayerController.instance.Translation,
            PlayerController.instance.Translation + Vector3.Right,
            PlayerController.instance.Translation + Vector3.Forward);
        Vector3? clickedPoint = worldPlane.IntersectRay(gameCamera.ProjectRayOrigin(currentMousePosition), gameCamera.ProjectRayNormal(currentMousePosition));
        return clickedPoint;
    }

    public void RefreshObjectHealthBar(Spatial owner, float distanceAboveTarget, float currentHealth, float maxHealth)
    {
        if (currentHealth <= 0.0f)
        {
            if (spawnedHealthBars.TryGetValue(owner, out HealthBar targetHealthBar))
            {
                spawnedHealthBars.Remove(owner);
                targetHealthBar.Destroy();
            }
        }
        else
        {
            if (!spawnedHealthBars.TryGetValue(owner, out HealthBar targetHealthBar))
            {
                targetHealthBar = healthBarPrefab.Instance<HealthBar>();
                owner.AddChild(targetHealthBar);
                targetHealthBar.Translation = Vector3.Up * distanceAboveTarget;
                spawnedHealthBars[owner] = targetHealthBar;
                targetHealthBar.LookAt(targetHealthBar.GlobalTranslation - gameCamera.Transform.basis.z, Vector3.Up);
            }
            targetHealthBar.SetValue(currentHealth, maxHealth);
        }
    }

}