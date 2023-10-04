using Godot;
using System;

public class EscapePod : Spatial
{
    public static EscapePod instance;

    public override void _Ready()
    {
        instance = this;
    }
}
