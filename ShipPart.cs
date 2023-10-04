using Godot;
using System;
using System.Collections.Generic;

public class ShipPart : GravityReacting
{
    public static List<ShipPart> allShipParts = new List<ShipPart>();

    public List<Clone> currentlyCarriedBy = new List<Clone>();
    public bool isRescued = false;

    public override void _Ready()
    {
        base._Ready();
        allShipParts.Add(this);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (isRescued)
        {
            // Do nothing
        }
        else if (currentlyCarriedBy.Count > 0)
        {
            currentHealth = health;
            GameManager.instance.RefreshObjectHealthBar(this, radius, 0, 10); // This hides it
            if (GlobalTranslation.DistanceTo(EscapePod.instance.GlobalTranslation) > 5.0f)
            {
                GlobalTranslation = currentlyCarriedBy[0].GlobalTranslation + Vector3.Up;
                foreach (Clone clone in currentlyCarriedBy)
                {
                    clone.SetNumberOfClonesHelpingCarry(currentlyCarriedBy.Count);
                }
            }
            else
            {
                isRescued = true;
                GameManager.instance.partsRecovered++;
                GameManager.instance.RefreshCloneHud();
                foreach (Clone clone in currentlyCarriedBy)
                {
                    clone.OnPartDelivered();
                }
                currentlyCarriedBy.Clear();
                GlobalTranslation = EscapePod.instance.GlobalTranslation + Vector3.Right.Rotated(Vector3.Up, rng.RandfRange(0.0f, Mathf.Pi * 2.0f)) * 5.0f;
                GlobalScale(0.25f * Vector3.One);
                GetNode<OmniLight>("OmniLight").LightEnergy *= 0.25f;
            }
        }
    }

    public void OnCloneStoppedCarrying(Clone clone)
    {
        currentlyCarriedBy.Remove(clone);
        if (!isRescued && currentlyCarriedBy.Count == 0)
        {
            // Put it back on the ground.
            GlobalTranslation -= Vector3.Up;
        }
    }
}
