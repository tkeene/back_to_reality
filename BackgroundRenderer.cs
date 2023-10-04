using Godot;
using System;

public class BackgroundRenderer : Spatial
{
    [Export]
    public Vector2 scrollSpeed = new Vector2(-1.0f, -0.5f);

    public override void _Process(float delta)
    {
        base._Process(delta);
        GlobalTranslation = GameManager.instance.gameCamera.Translation
            - GameManager.instance.gameCamera.Transform.basis.z * GameManager.instance.gameCamera.Far * 0.99f;
        Scale = Vector3.One * GameManager.instance.gameCamera.Size;
        LookAt(GlobalTranslation - GameManager.instance.gameCamera.Transform.basis.z, Vector3.Up);

        SpatialMaterial foregroundMaterial = GetNode<MeshInstance>("Foreground").GetActiveMaterial(0) as SpatialMaterial;
        foregroundMaterial.Uv1Offset = new Vector3(
            (foregroundMaterial.Uv1Offset.x + delta * scrollSpeed.x) % 1.0f,
            (foregroundMaterial.Uv1Offset.y + delta * scrollSpeed.y) % 1.0f,
            0.0f);
    }
}
