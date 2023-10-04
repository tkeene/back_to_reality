using Godot;
using System.Collections.Generic;

public class Destroyable : GravityReacting
{
    public static Dictionary<object, Destroyable> collidersToDestroyable = new Dictionary<object, Destroyable>();

    private object myCollider;

    public NavigationMeshInstance navMeshToUpdateWhenDestroyed = null;

    public override void _Ready()
    {
        base._Ready();

        myCollider = GetNode("StaticBody");
        if (myCollider == null)
        {
            GD.PrintErr(Name + " couldn't find its child collider.");
        }
        else
        {
            collidersToDestroyable[myCollider] = this;
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (currentHealth <= 0.0f)
        {
            GameManager.instance.shouldUpdateNavmeshNextFrame = true;
            GameManager.instance.RefreshObjectHealthBar(this, radius, currentHealth, health);
            QueueFree();
        }
        else if (currentHealth < health)
        {
            GameManager.instance.RefreshObjectHealthBar(this, radius, currentHealth, health);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (myCollider != null)
        {
            collidersToDestroyable.Remove(myCollider);
        }
        if (IsInstanceValid(navMeshToUpdateWhenDestroyed) && navMeshToUpdateWhenDestroyed != null)
        {
            navMeshToUpdateWhenDestroyed.TravelCost = 1;
            navMeshToUpdateWhenDestroyed = null;
        }
    }
}
