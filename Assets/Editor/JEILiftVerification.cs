using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const float LiftCorridorProbeZ = 4.6f;
    const float LiftCabinProbeZ = 1.9f;

    static void VerifyExpandedLift(JEIStructureView view)
    {
        var lift = FindTourLift(view);
        if (lift.FloorCount != view.floors.Length)
            throw new InvalidOperationException("The elevator does not serve every tour floor.");

        Physics.SyncTransforms();
        if (view.floors.Length > 3)
        {
            // Check the cabin's profile through the upper shaft, including the full rise to the roof.
            float bottom = view.floors[3].position.y - .32f;
            float top = view.floors[view.floors.Length - 1].position.y + 2.51f;
            var shaftCenter = new Vector3(JEILiftController.ShaftX, (bottom + top) * .5f, 1.72f);
            var halfSize = new Vector3(1.26f, (top - bottom) * .5f, 1.466f);
            var blockers = Physics.OverlapBox(shaftCenter, halfSize, Quaternion.identity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (blockers.Length > 0)
                throw new InvalidOperationException($"Slab or finish blocks upper lift shaft: {blockers[0].name}");
        }

        VerifyLiftGeometry(view);
        bool playerWasActive = view.walkPlayer.activeSelf;
        view.walkPlayer.SetActive(true);
        try
        {
            VerifyLiftRuntime(view);
        }
        finally
        {
            lift.SetEditorLanding(1, false);
            view.walkPlayer.SetActive(playerWasActive);
            Physics.SyncTransforms();
        }

        Debug.Log($"JEI_LIFT_SUCCESS: verified boarding, rider travel and door interlocks across {lift.FloorCount} floors.");
    }

    static void VerifyLiftGeometry(JEIStructureView view)
    {
        var lift = FindTourLift(view);
        var probe = new GameObject("Temporary elevator boarding probe");
        var capsule = probe.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.radius = .3f;
        capsule.center = Vector3.up * .9f;
        capsule.stepOffset = .25f;
        capsule.skinWidth = .035f;
        try
        {
            VerifySealedLiftDoors(view, 0);
            for (int level = 1; level <= view.floors.Length; level++)
            {
                lift.SetEditorLanding(level, true);
                Physics.SyncTransforms();
                VerifySealedLiftDoors(view, level);
                float height = view.floors[level - 1].position.y;
                PlaceProbe(capsule, LiftProbePosition(height + .08f, LiftCorridorProbeZ));
                WalkTo(capsule, LiftProbeTarget(LiftCabinProbeZ));
                VerifyLiftElevation(capsule, height, "boarded floor " + level);
                WalkTo(capsule, LiftProbeTarget(LiftCorridorProbeZ));
                VerifyLiftElevation(capsule, height, "exited floor " + level);
            }
        }
        finally
        {
            Object.DestroyImmediate(probe);
            lift.SetEditorLanding(1, false);
            Physics.SyncTransforms();
        }
    }

    static void VerifyLiftRuntime(JEIStructureView view)
    {
        var lift = FindTourLift(view);
        var player = view.walkPlayer.GetComponent<CharacterController>();
        Vector3 originalPosition = player.transform.position;
        Quaternion originalRotation = player.transform.rotation;
        bool wasEnabled = player.enabled;
        try
        {
            PlaceProbe(player, LiftProbePosition(view.floors[0].position.y + .08f, LiftCorridorProbeZ));
            if (!lift.TryCall()) throw new InvalidOperationException("Cannot open the elevator at floor 1 with the landing call.");
            AdvanceLiftUntil(lift, () => lift.IsOpen);
            WalkTo(player, LiftProbeTarget(LiftCabinProbeZ));

            // Enter the door area during a close. The interlock must reopen without moving the cabin.
            if (!lift.TrySelectFloor(2)) throw new InvalidOperationException("Cannot select the second floor inside the elevator.");
            lift.AdvanceLift(.06f);
            Physics.SyncTransforms();
            WalkTo(player, LiftProbeTarget(JEILiftController.LandingZ));
            lift.AdvanceLift(.04f);
            AdvanceLiftUntil(lift, () => lift.IsOpen);
            if (lift.CurrentFloor != 1) throw new InvalidOperationException("Door interlock failed to hold the first floor clear.");
            WalkTo(player, LiftProbeTarget(LiftCabinProbeZ));

            for (int leg = 1; leg <= lift.FloorCount; leg++)
            {
                int destination = leg % lift.FloorCount + 1;
                if (!lift.TrySelectFloor(destination)) throw new InvalidOperationException("Cannot select lift floor " + destination);
                AdvanceLiftUntil(lift, () => lift.IsMoving);
                VerifySealedLiftDoors(view, 0);
                if (player.enabled) throw new InvalidOperationException("Character controller remained active during the ride.");
                AdvanceLiftUntil(lift, () => lift.IsOpen);
                if (lift.CurrentFloor != destination || !player.enabled)
                    throw new InvalidOperationException("The elevator did not restore normal walking at the selected floor.");
                float y = view.floors[destination - 1].position.y;
                VerifyLiftElevation(player, y, "riding to floor " + destination);
                VerifySealedLiftDoors(view, destination);
                WalkTo(player, LiftProbeTarget(LiftCorridorProbeZ));
                VerifyLiftElevation(player, y, "walking out at floor " + destination);
                if (destination != 1) WalkTo(player, LiftProbeTarget(LiftCabinProbeZ));
            }

            // A visitor who has taken the stairs can call an empty car from another floor.
            int topFloor = lift.FloorCount;
            float topHeight = view.floors[topFloor - 1].position.y;
            PlaceProbe(player, LiftProbePosition(topHeight + .08f, LiftCorridorProbeZ));
            if (!lift.TryCall()) throw new InvalidOperationException("The top landing could not call the empty car.");
            AdvanceLiftUntil(lift, () => lift.IsMoving);
            if (!player.enabled) throw new InvalidOperationException("Calling the elevator carried a visitor standing in the corridor.");
            AdvanceLiftUntil(lift, () => lift.IsOpen && lift.CurrentFloor == topFloor);
            VerifyLiftElevation(player, topHeight, "calling from the top-floor corridor");
            VerifySealedLiftDoors(view, topFloor);
        }
        finally
        {
            player.enabled = false;
            player.transform.SetPositionAndRotation(originalPosition, originalRotation);
            player.enabled = wasEnabled;
            Physics.SyncTransforms();
        }
    }

    static void AdvanceLiftUntil(JEILiftController lift, Func<bool> reached)
    {
        const int stepsForTwentySeconds = 20 * 60;
        for (int i = 0; i < stepsForTwentySeconds && !reached(); i++)
            lift.AdvanceLift(1f / 60f);
        Physics.SyncTransforms();
        if (!reached()) throw new InvalidOperationException("The elevator did not finish the expected transition.");
    }

    static void VerifySealedLiftDoors(JEIStructureView view, int openFloor)
    {
        for (int i = 0; i < view.floors.Length; i++)
        {
            var floor = view.floors[i];
            var landing = floor.Find(TourLiftLanding);
            foreach (float offset in new[] { -.17f, .17f })
            {
                var point = new Vector3(JEILiftController.ShaftX + offset, floor.position.y + 1.15f, LiftCorridorProbeZ);
                bool blocked = Physics.Raycast(point, Vector3.back, out var hit, LiftCorridorProbeZ - 3.08f);
                if (i + 1 == openFloor && blocked)
                    throw new InvalidOperationException("The open elevator threshold is blocked by " + hit.collider.name);
                if (i + 1 != openFloor && (!blocked || !hit.collider.transform.IsChildOf(landing)))
                    throw new InvalidOperationException("An undocked elevator landing is not physically secured on floor " + (i + 1));
            }
        }
    }

    static void VerifyLiftElevation(CharacterController controller, float expected, string action)
    {
        if (Mathf.Abs(controller.transform.position.y - expected) > .085f)
            throw new InvalidOperationException($"Elevator height mismatch when {action}: expected {expected:F2}, got {controller.transform.position.y:F2}.");
    }

    static Vector3 LiftProbePosition(float height, float z) => new Vector3(JEILiftController.ShaftX, height, z);
    static Vector2 LiftProbeTarget(float z) => new Vector2(JEILiftController.ShaftX, z);
}
