using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A hinged or sliding room door with collision-checked movement.</summary>
public sealed class JEIRoomDoor : MonoBehaviour
{
    const float Reach = 2.5f;
    const float AimThreshold = .9537f; // Approximately 17.5 degrees from the center of view.
    const float DegreesPerSecond = 115f;
    const float MaxAngularStep = 2f;
    const float SlideMetresPerSecond = 1.25f;
    const float MaxSlideStep = .015f;
    const float PoseTolerance = .0001f;
    static readonly Vector3 EyeOffset = new Vector3(0, 1.65f, 0);
    static readonly HashSet<JEIRoomDoor> ActiveDoors = new HashSet<JEIRoomDoor>();
    static readonly Collider[] SweepHits = new Collider[32];
    static readonly RaycastHit[] RayHits = new RaycastHit[32];

    [SerializeField] Transform hinge;
    [SerializeField] BoxCollider[] leafColliders;
    [SerializeField] Quaternion closedRotation = Quaternion.identity;
    [SerializeField] Vector3 closedPosition;
    [SerializeField] Vector3 slideInParent;
    [SerializeField] Vector3 localHandlePoint;
    [SerializeField] float openAngle = 90f;

    float progress;

    public bool TargetOpen { get; private set; }
    public bool IsOpen => progress >= 1f - PoseTolerance;
    public bool IsMoving => Mathf.Abs(progress - (TargetOpen ? 1f : 0f)) >= PoseTolerance;
    public bool IsBlocked { get; private set; }
    public Vector3 InteractionPoint => transform.TransformPoint(localHandlePoint);
    public bool IsSliding => slideInParent.sqrMagnitude > 0f;

    void OnEnable() => ActiveDoors.Add(this);
    void OnDisable() => ActiveDoors.Remove(this);

    public void Configure(Transform movingHinge, Collider[] movingColliders, Vector3 handleInRoot, float degreesOpen)
    {
        if (Mathf.Abs(degreesOpen) < 1f || Mathf.Abs(degreesOpen) > 120f)
            throw new ArgumentOutOfRangeException(nameof(degreesOpen));
        ConfigureLeaf(movingHinge, movingColliders, handleInRoot);
        openAngle = degreesOpen;
        slideInParent = Vector3.zero;
        SetOpenImmediate(false);
    }

    public void ConfigureSliding(Transform panel, Collider[] movingColliders, Vector3 handleInRoot, Vector3 travelInMount)
    {
        if (travelInMount.sqrMagnitude < .0001f) throw new ArgumentOutOfRangeException(nameof(travelInMount));
        ConfigureLeaf(panel, movingColliders, handleInRoot);
        openAngle = 0f;
        slideInParent = panel.parent.InverseTransformVector(transform.TransformVector(travelInMount));
        SetOpenImmediate(false);
    }

    void ConfigureLeaf(Transform movingLeaf, Collider[] movingColliders, Vector3 handleInRoot)
    {
        if (!movingLeaf || movingLeaf == transform || !movingLeaf.IsChildOf(transform))
            throw new ArgumentException("The moving leaf must be below the stationary door root.", nameof(movingLeaf));
        if (movingColliders == null || movingColliders.Length == 0)
            throw new ArgumentException("The door needs a solid leaf collider.", nameof(movingColliders));

        var boxes = new BoxCollider[movingColliders.Length];
        for (int i = 0; i < movingColliders.Length; i++)
        {
            if (!(movingColliders[i] is BoxCollider box) || !box.transform.IsChildOf(movingLeaf) || box.isTrigger)
                throw new ArgumentException("Door leaf colliders must be solid boxes on the moving leaf or its children.", nameof(movingColliders));
            boxes[i] = box;
        }

        hinge = movingLeaf;
        leafColliders = boxes;
        closedRotation = hinge.localRotation;
        closedPosition = hinge.localPosition;
        localHandlePoint = handleInRoot;
    }

    /// <summary>Set a pose for editor geometry checks. Gameplay uses TryToggle.</summary>
    public void SetOpenImmediate(bool open)
    {
        TargetOpen = open;
        IsBlocked = false;
        SetPose(open ? 1f : 0f);
    }

    void SetPose(float value)
    {
        progress = value;
        if (!hinge) return;
        if (IsSliding) hinge.localPosition = closedPosition + slideInParent * progress;
        else hinge.localRotation = closedRotation * Quaternion.AngleAxis(openAngle * progress, Vector3.up);
    }

    public bool TryToggle()
    {
        if (!hinge || leafColliders == null || leafColliders.Length == 0) return false;
        bool nextOpen = !TargetOpen;
        float firstStep = Mathf.MoveTowards(progress, nextOpen ? 1f : 0f, MaxProgressStep());
        if (firstStep != progress && !MotionIsClear(firstStep)) return false;
        TargetOpen = nextOpen;
        IsBlocked = false;
        return true;
    }

    void Update() => AdvanceMotion(Time.deltaTime);

    /// <summary>Advances the same collision-checked motion used by gameplay.</summary>
    public void AdvanceMotion(float deltaTime)
    {
        if (!hinge || !IsMoving || deltaTime <= 0f) return;
        float remaining = (IsSliding ? SlideMetresPerSecond / SlideDistance() : DegreesPerSecond / Mathf.Abs(openAngle)) *
            Mathf.Min(deltaTime, .10f);
        float destination = TargetOpen ? 1f : 0f;
        float maxStep = MaxProgressStep();
        while (remaining > .00001f && IsMoving)
        {
            float next = Mathf.MoveTowards(progress, destination, Mathf.Min(remaining, maxStep));
            if (Mathf.Abs(next - destination) < PoseTolerance) next = destination;
            IsBlocked = !MotionIsClear(next);
            if (IsBlocked) return;
            remaining -= Mathf.Abs(next - progress);
            SetPose(next);
        }
    }

    float SlideDistance() => hinge.parent.TransformVector(slideInParent).magnitude;

    float MaxProgressStep() => IsSliding ? MaxSlideStep / SlideDistance() : MaxAngularStep / Mathf.Abs(openAngle);

    bool MotionIsClear(float nextProgress)
    {
        float delta = nextProgress - progress;
        Quaternion rotation = IsSliding ? Quaternion.identity : Quaternion.AngleAxis(openAngle * delta, hinge.up);
        Vector3 translation = IsSliding ? hinge.parent.TransformVector(slideInParent * delta) : Vector3.zero;
        foreach (var box in leafColliders)
        {
            if (!box || !box.enabled || !box.gameObject.activeInHierarchy) continue;
            Vector3 pivot = hinge.position;
            Vector3 center = pivot + rotation * (box.transform.TransformPoint(box.center) - pivot) + translation;
            Vector3 scale = box.transform.lossyScale;
            Vector3 halfSize = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * .5f;
            // Keep a small inset so the closed leaf can meet its jamb without reporting mere contact.
            halfSize -= Vector3.one * .002f;
            halfSize = Vector3.Max(halfSize, Vector3.one * .001f);
            int count = Physics.OverlapBoxNonAlloc(center, halfSize, SweepHits, rotation * box.transform.rotation,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == SweepHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var obstacle = SweepHits[i];
                if (!obstacle.transform.IsChildOf(transform) &&
                    !Physics.GetIgnoreLayerCollision(box.gameObject.layer, obstacle.gameObject.layer) &&
                    !Physics.GetIgnoreCollision(box, obstacle))
                    return false;
            }
        }
        return true;
    }

    /// <summary>Finds the room door under the center of the view, or nearest visible handle just beside it.</summary>
    public static JEIRoomDoor FindLookTarget(JEITourController tour)
    {
        if (!tour || !tour.isActiveAndEnabled || !tour.view) return null;
        // The third-person camera sits behind the player, so measure reach from the player's eyes.
        Vector3 origin = tour.transform.TransformPoint(EyeOffset);
        Vector3 direction = tour.view.forward;
        var direct = ClosestHit(origin, direction, Reach, tour.transform);
        if (direct)
        {
            var door = direct.GetComponentInParent<JEIRoomDoor>();
            if (door && door.hinge && door.isActiveAndEnabled) return door;
        }

        JEIRoomDoor nearest = null;
        float nearestDistance = Reach;
        foreach (var door in ActiveDoors)
        {
            if (!door.hinge) continue;
            Vector3 offset = door.InteractionPoint - origin;
            float distance = offset.magnitude;
            if (distance < .001f || distance >= nearestDistance) continue;
            Vector3 toHandle = offset / distance;
            if (Vector3.Dot(direction, toHandle) < AimThreshold) continue;
            var obstruction = ClosestHit(origin, toHandle, distance, tour.transform);
            if (obstruction && !obstruction.transform.IsChildOf(door.transform)) continue;
            nearest = door;
            nearestDistance = distance;
        }
        return nearest;
    }

    static Collider ClosestHit(Vector3 origin, Vector3 direction, float range, Transform player)
    {
        Collider closest = null;
        float nearest = range;
        int count = Physics.RaycastNonAlloc(origin, direction, RayHits, range,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = RayHits[i];
            if (hit.distance >= nearest || hit.collider.transform.IsChildOf(player)) continue;
            closest = hit.collider;
            nearest = hit.distance;
        }
        return closest;
    }
}
