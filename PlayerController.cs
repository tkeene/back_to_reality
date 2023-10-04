using Godot;
using System;

public class PlayerController : Spatial
{
	public static PlayerController instance;

	[Export]
	public float walkingSpeed = 6.0f;

	private AnimatedSprite3D myRenderer;
	private Vector2 currentFacing = Vector2.Right;
	private Vector2 currentVelocity = Vector2.Zero;
	private float remainingIssueCommandAnimationTime = 0.0f;

	public override void _Ready()
	{
		base._Ready();
		instance = this;
		myRenderer = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
	}

	public override void _Process(float delta)
	{
		base._Process(delta);
		GameManager.instance.gameCamera.LookAtFromPosition(GlobalTranslation + new Vector3(1.0f, 1.0f, 1.0f) * GameManager.instance.zoomedOutCameraZoom, GlobalTranslation, Vector3.Up);
		GameManager.instance.gameCamera.Size = Mathf.Lerp(GameManager.instance.gameCamera.Size,
			Input.IsActionPressed("zoom") ? GameManager.instance.zoomedOutCameraZoom : GameManager.instance.defaultCameraZoom,
			delta / GameManager.instance.cameraZoomDurationSeconds);

		if (GameManager.instance.isLeftMouseHeldThisFrame)
		{
			remainingIssueCommandAnimationTime = 1.5f;
			if (GameManager.instance.leftMouseWasClickedDownThisFrame && GameManager.instance.currentTutorialStep >= 3)
			{
				GameManager.instance.OnPlayerCommandIssued();
			}
		}
		if (GameManager.instance.isRightMouseHeldThisFrame || Input.IsKeyPressed((int)KeyList.Q))
		{
			GameManager.instance.OnPlayerRecallOrdered();
		}

		UpdateRenderer(delta);
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);
		bool isOnGround = false;
		if (delta > 0.0f && GameManager.instance.currentTutorialStep >= 2)
		{
			Vector2 playerInput = new Vector2(
				Input.GetAxis("ui_left", "ui_right"),
				Input.GetAxis("ui_down", "ui_up"));
			currentVelocity = playerInput * walkingSpeed;
			if (playerInput.LengthSquared() > 0.0f)
			{
				currentFacing = playerInput.Normalized();
			}

			Vector3 right = GameManager.instance.gameCamera.GlobalTransform.basis.x.Normalized();
			Vector3 forward = right.Rotated(Vector3.Up, Mathf.Pi * 0.5f);

			Vector3 inputDirectionInWorld = right * playerInput.x + forward * playerInput.y;
			PhysicsDirectSpaceState currentPhysics = GetWorld().DirectSpaceState;
			if (inputDirectionInWorld.LengthSquared() > 0)
			{
				float currentOffsetRadians = 0.0f;
				const float kSlidingCheckIncrementDegrees = 5.0f;
				const float kMaximumSlideDegrees = 85.0f;
				Vector3 forwardOffset = inputDirectionInWorld * walkingSpeed * delta;
				if (Input.IsActionPressed("zoom"))
				{
					forwardOffset *= 0.25f;
				}
				bool hasPlaceToStand = false;
				while (!hasPlaceToStand && currentOffsetRadians <= Mathf.Deg2Rad(kMaximumSlideDegrees))
				{
					Vector3 newPosition = Vector3.Zero;
					for (int i = 0; i < 2; i++)
					{
						int currentSign = (i == 0 ? 1 : -1);
						Vector3 possibleOffset = forwardOffset.Rotated(Vector3.Up, currentOffsetRadians * currentSign);
						possibleOffset *= (1.0f + Mathf.Cos(currentOffsetRadians)) * 0.5f; // Slightly slower movement if most of the component slides along the wall
						Vector3 positionToCheck = Translation + possibleOffset;
						if (KoboldsKeep.Utils.Raycast(currentPhysics, positionToCheck, Vector3.Down,
								GameManager.kTerrainMask, queryAreas: false, out Vector3 hitPoint, out _))
						{
							if (KoboldsKeep.Utils.GetOverlappingColliders(currentPhysics, positionToCheck,
								0.5f, GameManager.kInteractionMask, checkAreas: false, out Godot.Collections.Array _) == 0)
							{
								hasPlaceToStand = true;
								newPosition = hitPoint + Vector3.Up * 0.5f;
								break;
							}
						}
					}
					if (hasPlaceToStand)
					{
						Translation = newPosition;
						isOnGround = true;
						break;
					}
					else
					{
						currentOffsetRadians += Mathf.Deg2Rad(kSlidingCheckIncrementDegrees);
					}
				}
			}
			else
			{
				currentVelocity = Vector2.Zero;
			}

			if (!isOnGround && KoboldsKeep.Utils.Raycast(currentPhysics, Translation, Vector3.Down,
				GameManager.kTerrainMask, queryAreas: false, out _, out _))
			{
				isOnGround = true;
			}
			if (!isOnGround)
			{
				Translation = Translation.MoveToward(BlackHole.instance.Translation, delta);
			}
			if (Translation.DistanceTo(BlackHole.instance.Translation) < BlackHole.instance.GetCurrentRadius() - 3.0f)
			{
				GameManager.instance.GameOver();
			}
		}
	}

	private void UpdateRenderer(float delta)
	{
		string facingPrefix = KoboldsKeep.Utils.GetCardinalDirection(currentFacing);

		string animationToUse;
		if (currentVelocity.LengthSquared() > 0.0f)
		{
			animationToUse = "move_" + facingPrefix;
			remainingIssueCommandAnimationTime = 0.0f;
		}
		else if ((GameManager.instance.isLeftMouseHeldThisFrame || remainingIssueCommandAnimationTime > 0.0f)
			&& GameManager.instance.currentTutorialStep >= 2)
		{
			animationToUse = "point_" + facingPrefix;
			remainingIssueCommandAnimationTime -= delta;
		}
		else
		{
			animationToUse = "idle_" + facingPrefix;
			remainingIssueCommandAnimationTime = 0.0f;
		}
		if (myRenderer.Animation != animationToUse)
		{
			myRenderer.Animation = animationToUse;
		}
	}

}
