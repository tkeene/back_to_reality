using Godot;
using System;

namespace KoboldsKeep
{
    public class Utils
    {
        #region Godot Utils

        public static bool Raycast(PhysicsDirectSpaceState currentPhysics, Vector3 position, Vector3 direction,
            uint collisionMask, bool queryAreas, out Vector3 hitPoint, out object hitObject)
        {
            bool hasHit = false;
            hitPoint = Vector3.Zero;
            hitObject = null;

            Godot.Collections.Dictionary intersectionResult = currentPhysics.IntersectRay(position, position + direction, null, collisionMask, !queryAreas, queryAreas);
            if (intersectionResult != null && intersectionResult.Contains("collider") && intersectionResult["collider"] != null)
            {
                hitPoint = (Vector3)intersectionResult["position"];
                hitObject = intersectionResult["collider"];
                hasHit = true;
            }
            return hasHit;
        }

        // TODO The collection appears to be a list of query result dictionaries
        // TODO Change to output tuples of results?
        public static int GetOverlappingColliders(PhysicsDirectSpaceState currentPhysics,
            Vector3 position, float radius, uint collisionMask, bool checkAreas, out Godot.Collections.Array results)
        {
            PhysicsShapeQueryParameters query = new PhysicsShapeQueryParameters();
            query.Transform = new Transform(Basis.Identity, position);
            query.CollisionMask = collisionMask;
            query.CollideWithAreas = checkAreas;
            SphereShape overlapShape = new SphereShape();
            overlapShape.Radius = radius;
            query.SetShape(overlapShape);
            results = currentPhysics.IntersectShape(query);
            return results.Count;
        }
        #endregion

        private static float[] facingAnglesDegrees = { -180.0f, -135.0f, -90.0f, -45.0f, 0.0f, 45.0f, 90.0f, 135.0f, 180.0f };
        private static string[] facingNamePrefixes = { "s", "sw", "w", "nw", "n", "ne", "e", "se", "s" };
        public static string GetCardinalDirection(Vector2 direction)
        {
            if (direction.LengthSquared() == 0.0f)
            {
                direction = Vector2.Right;
            }
            float facingRotationDegrees = Mathf.Rad2Deg(direction.AngleTo(Vector2.Down));
            int bestDirection = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < facingAnglesDegrees.Length; i++)
            {
                float newDistance = Mathf.Abs(facingAnglesDegrees[i] - facingRotationDegrees);
                if (newDistance < bestDistance)
                {
                    bestDirection = i;
                    bestDistance = newDistance;
                }
            }
            return facingNamePrefixes[bestDirection];
        }

        // Can we change this to some kind of RecursivelyGetComponents<> thing?
        //private void RecursivelyDeletePhysics(Node node)
        //{
        //    foreach (object child in node.GetChildren())
        //    {
        //        if (child is Node)
        //        {
        //            RecursivelyDeletePhysics(child as Node);
        //        }
        //        if (child is CollisionObject)
        //        {
        //            //GD.Print("Freeing CollisionObject " + (child as CollisionObject).Name);
        //            (child as CollisionObject).QueueFree();
        //        }
        //        else if (child is StaticBody)
        //        {
        //            //GD.Print("Freeing StaticBody " + (child as StaticBody).Name);
        //            (child as Node).QueueFree();
        //        }
        //    }
        //}

    }
}