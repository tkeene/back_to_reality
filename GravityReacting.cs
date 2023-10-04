using Godot;
using System;

public class GravityReacting : Spatial
{
    [Export]
    public float health = 10.0f;
    public float currentHealth;
    [Export]
    public float gravityVulnerability = 1.0f;
    [Export]
    public float radius = 5.0f;

    const int kNumberOfFramesBetweenPhysicsChecks = 60;
    private static int currentGravityReactionObjectCounter = 0;
    protected static RandomNumberGenerator rng = new RandomNumberGenerator();
    private int framesUntilNextPhysicsCheck = 0;
    const float kFallIntoHoleSpeed = 1.0f;
    private Vector3 fallingRotation = Vector3.One.Normalized();

    protected bool isBeingDrainedByBlackHole = false;
    protected bool isFallingIntoBlackHole = false;
    protected bool canFallIntoBlackHole = true;

    public override void _Ready()
    {
        base._Ready();

        currentHealth = health;
        // Round robin objects so they're not all checking for black holes on the same frame.
        currentGravityReactionObjectCounter++;
        framesUntilNextPhysicsCheck = kNumberOfFramesBetweenPhysicsChecks
            + (currentGravityReactionObjectCounter % kNumberOfFramesBetweenPhysicsChecks);

        if (HasNode("Blocker"))
        {
            Destroyable blocker = GetNode<Destroyable>("Blocker");
            NavigationMeshInstance navMesh = GetNode<NavigationMeshInstance>("NavigationMeshInstance");
            navMesh.TravelCost = 999999;
            blocker.navMeshToUpdateWhenDestroyed = navMesh;
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (isFallingIntoBlackHole)
        {
            if (Translation != BlackHole.instance.Translation)
            {
                Translation = Translation.MoveToward(BlackHole.instance.Translation, kFallIntoHoleSpeed * delta);
                // TODO Do scale and rotation updates not work on floors because I'm using static rigidbodies for the child objects?
                //GD.Print(fallingRotation);
                Rotate(fallingRotation, Mathf.Pi * delta * 0.25f);
                GlobalScale(Vector3.One * (1.0f - (delta * 0.3f)));
            }
            else
            {
                GameManager.instance.RefreshObjectHealthBar(this, radius, 0.0f, health);
                GameManager.instance.shouldUpdateNavmeshNextFrame = true;
                QueueFree();
            }
        }
        else if (isBeingDrainedByBlackHole)
        {
            float thisFrameDamage = delta * gravityVulnerability;
            currentHealth -= thisFrameDamage;
            if (currentHealth <= 0.0f)
            {
                StartFalling();
            }
        }
        else
        {
            framesUntilNextPhysicsCheck--;
            if (framesUntilNextPhysicsCheck <= 0)
            {
                PhysicsDirectSpaceState currentPhysics = GetWorld().DirectSpaceState;
                int numberOfColliders = KoboldsKeep.Utils.GetOverlappingColliders(currentPhysics,
                    GlobalTranslation, radius, GameManager.kGravityWellMask, checkAreas: true, out Godot.Collections.Array results);
                if (numberOfColliders > 0)
                {
                    //GD.Print(Name + " is colliding with " + numberOfColliders + " gravity colliders.");
                    isBeingDrainedByBlackHole = true;
                    if (numberOfColliders >= 2)
                    {
                        gravityVulnerability += delta * 20.0f;
                    }
                }
                else
                {
                    framesUntilNextPhysicsCheck = kNumberOfFramesBetweenPhysicsChecks;
                }
            }
        }

        if (currentHealth < health)
        {
            GameManager.instance.RefreshObjectHealthBar(this, radius, currentHealth, health);
        }
    }

    private void StartFalling()
    {
        if (canFallIntoBlackHole)
        {
            isFallingIntoBlackHole = true;
            GameManager.instance.shouldUpdateNavmeshNextFrame = true;
            const float kMagnitude = 4.0f;
            fallingRotation = new Vector3(
                rng.RandfRange(-Mathf.Pi * kMagnitude, Mathf.Pi * kMagnitude),
                rng.RandfRange(-Mathf.Pi * kMagnitude, Mathf.Pi * kMagnitude),
                rng.RandfRange(-Mathf.Pi * kMagnitude, Mathf.Pi * kMagnitude)
                );
            if (fallingRotation.LengthSquared() == 0.0f)
            {
                fallingRotation = Vector3.One;
            }
            fallingRotation = fallingRotation.Normalized();

            RecursivelyDeletePhysics(this);
        }
    }

    private void RecursivelyDeletePhysics(Node node)
    {
        foreach (object child in node.GetChildren())
        {
            if (child is Node)
            {
                RecursivelyDeletePhysics(child as Node);
            }
            if (child is CollisionObject)
            {
                //GD.Print("Freeing CollisionObject " + (child as CollisionObject).Name);
                (child as CollisionObject).QueueFree();
            }
            else if (child is StaticBody)
            {
                //GD.Print("Freeing StaticBody " + (child as StaticBody).Name);
                (child as Node).QueueFree();
            }
        }
    }
}
