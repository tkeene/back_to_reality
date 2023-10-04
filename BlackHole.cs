using Godot;
using System;

public class BlackHole : Spatial
{
    [Export]
    public float growRateMetersPerSecond = 0.25f;

    public static BlackHole instance;

    public override void _Ready()
    {
        base._Ready();
        instance = this;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (delta > 0.0f && GameManager.instance.currentTutorialStep >= 2)
        {
            Scale = Vector3.One * (Scale.x + growRateMetersPerSecond * delta);
        }
    }

    public float GetCurrentRadius()
    {
        return Scale.x;
    }
}
