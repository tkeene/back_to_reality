using Godot;
using System;

public class HealthBar : Spatial
{
    public void SetValue(float current, float max)
    {
        if (max <= 0)
        {
            max = 0.01f;
        }
        float ratio = current / max;
        GetNode<Spatial>("BarScaler").Scale
            = new Vector3(ratio, 1.0f, 1.0f);
    }

    public void Destroy()
    {
        QueueFree();
    }
}
