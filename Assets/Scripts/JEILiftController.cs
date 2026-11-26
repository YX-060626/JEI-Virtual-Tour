using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Calls, interlocks and drives the tour elevator without moving the rest of the building.</summary>
[DefaultExecutionOrder(35)]
public sealed class JEILiftController : MonoBehaviour
{
    public const float ShaftX = 10.9f;
    public const float LandingZ = 3.3f;
    public const float CabinFrontZ = 3.17f;
    public const float DoorWidth = 1.1f;
    public const float DoorHeight = 2.4f;

    [Serializable]
    public sealed class Landing
    {
        public Transform floor;
        public Transform leftDoor;
        public Transform rightDoor;
        public MeshRenderer button;
        public MeshRenderer indicator;
    }

    enum Phase { Closed, Opening, Open, Closing, Moving }

    [SerializeField] JEIStructureView overview;
    [SerializeField] CharacterController player;
    [SerializeField] Transform cabin;
    [SerializeField] Transform cabinLeftDoor;
    [SerializeField] Transform cabinRightDoor;
    [SerializeField] Landing[] landings;
    [SerializeField] Material readyLamp;
    [SerializeField] Material standbyLamp;

    const float DoorSpeed = 1.6f;
    const float DoorSlide = .565f;
    const float DoorOffset = .2775f;
    const float TravelSpeed = 1.8f;
    const float CabinSafeFrontZ = CabinFrontZ - .36f;
    Phase phase;
    int currentFloor = 1;
    int destinationFloor = 1;
    float openness;
    float travelElapsed;
    float travelDuration;
    float travelStart;
    float riderOffset;
    bool carryingPlayer;
    bool originalControllerState;
    MeshRenderer[] cabinRenderers;
    GUIStyle promptStyle;
    Font promptFont;

    public int CurrentFloor => currentFloor;
    public int FloorCount => landings?.Length ?? 0;
    public bool IsOpen => phase == Phase.Open;
    public bool IsMoving => phase == Phase.Moving;
    public Vector3 CabinPosition => cabin.position;

    public void Configure(JEIStructureView view, Transform car, Transform innerLeft, Transform innerRight,
        Landing[] stops, Material lit, Material dim)
    {
        overview = view;
        player = view.walkPlayer.GetComponent<CharacterController>();
        cabin = car;
        cabinLeftDoor = innerLeft;
        cabinRightDoor = innerRight;
        landings = stops;
        readyLamp = lit;
        standbyLamp = dim;
        PositionAtStop(1, false);
    }

    void Awake()
    {
        cabinRenderers = cabin.GetComponentsInChildren<MeshRenderer>(true);
        PositionAtStop(1, false);
    }

    void Update()
    {
        if (overview.IsWalking && Cursor.lockState == CursorLockMode.Locked)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) TryCall();
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) TrySelectFloor(1);
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) TrySelectFloor(2);
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) TrySelectFloor(3);
                if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) TrySelectFloor(4);
                if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) TrySelectFloor(5);
                if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) TrySelectFloor(6);
            }
        }
        AdvanceLift(Time.deltaTime);
    }

    void LateUpdate()
    {
        int isolatedFloor = overview.SelectedFloor;
        bool visible = isolatedFloor == 0 || (!IsMoving && currentFloor == isolatedFloor);
        foreach (var renderer in cabinRenderers) renderer.enabled = visible;
    }

    void OnDisable()
    {
        ReleaseRider();
    }

    public bool TryCall()
    {
        int landing = NearbyLanding();
        return landing > 0 && RequestFloor(landing);
    }

    public bool TrySelectFloor(int floor)
    {
        return IsOpen && IsInsideCabin() && RequestFloor(floor);
    }

    bool RequestFloor(int floor)
    {
        if (floor < 1 || floor > landings.Length || phase == Phase.Moving || phase == Phase.Closing) return false;
        if (floor == currentFloor)
        {
            if (phase == Phase.Closed)
            {
                phase = Phase.Opening;
                SetDoorVisuals();
            }
            return true;
        }
        if (phase == Phase.Opening || (phase == Phase.Open && DoorwayOccupied())) return false;

        destinationFloor = floor;
        if (phase == Phase.Open) phase = Phase.Closing;
        else StartTravel();
        return true;
    }

    // Uses the same stepping function in Play mode and deterministic scene verification.
    public void AdvanceLift(float elapsed)
    {
        switch (phase)
        {
            case Phase.Opening:
                openness = Mathf.MoveTowards(openness, 1f, elapsed * DoorSpeed);
                if (openness >= 1f) phase = Phase.Open;
                SetDoorVisuals();
                break;
            case Phase.Closing:
                if (DoorwayOccupied())
                {
                    destinationFloor = currentFloor;
                    phase = Phase.Opening;
                    return;
                }
                openness = Mathf.MoveTowards(openness, 0f, elapsed * DoorSpeed);
                if (openness <= 0f) StartTravel();
                SetDoorVisuals();
                break;
            case Phase.Moving:
                travelElapsed += elapsed;
                float progress = Mathf.Clamp01(travelElapsed / travelDuration);
                float y = Mathf.Lerp(travelStart, StopHeight(destinationFloor), Mathf.SmoothStep(0f, 1f, progress));
                SetCabinHeight(y);
                if (carryingPlayer)
                {
                    var position = transform.InverseTransformPoint(player.transform.position);
                    position.y = y + riderOffset;
                    player.transform.position = transform.TransformPoint(position);
                }
                if (progress < 1f) break;
                currentFloor = destinationFloor;
                ReleaseRider();
                phase = Phase.Opening;
                SetDoorVisuals();
                break;
        }
    }

    void StartTravel()
    {
        carryingPlayer = IsInsideCabin();
        if (carryingPlayer)
        {
            riderOffset = Mathf.Max(0f, transform.InverseTransformPoint(player.transform.position).y - cabin.localPosition.y);
            originalControllerState = player.enabled;
            player.enabled = false;
        }
        travelElapsed = 0f;
        travelStart = cabin.localPosition.y;
        travelDuration = 1f + Mathf.Abs(StopHeight(destinationFloor) - travelStart) / TravelSpeed;
        phase = Phase.Moving;
        SetDoorVisuals();
    }

    void ReleaseRider()
    {
        if (carryingPlayer && player) player.enabled = originalControllerState;
        carryingPlayer = false;
    }

    public void SetEditorLanding(int floor, bool open)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Preview positions can only be applied in Edit mode.");
        PositionAtStop(floor, open);
    }

    void PositionAtStop(int floor, bool open)
    {
        currentFloor = destinationFloor = floor;
        phase = open ? Phase.Open : Phase.Closed;
        openness = open ? 1f : 0f;
        SetCabinHeight(StopHeight(floor));
        SetDoorVisuals();
    }

    float StopHeight(int floor) => transform.InverseTransformPoint(landings[floor - 1].floor.position).y;

    void SetCabinHeight(float height)
    {
        var position = cabin.localPosition;
        position.y = height;
        cabin.localPosition = position;
    }

    void SetDoorVisuals()
    {
        float fraction = IsMoving ? 0f : Mathf.SmoothStep(0f, 1f, openness);
        SlideDoor(cabinLeftDoor, -1, fraction);
        SlideDoor(cabinRightDoor, 1, fraction);
        for (int i = 0; i < landings.Length; i++)
        {
            var landing = landings[i];
            bool docked = !IsMoving && currentFloor == i + 1;
            SlideDoor(landing.leftDoor, -1, docked ? fraction : 0f);
            SlideDoor(landing.rightDoor, 1, docked ? fraction : 0f);
            landing.indicator.sharedMaterial = docked ? readyLamp : standbyLamp;
            landing.button.sharedMaterial = destinationFloor == i + 1 ? readyLamp : standbyLamp;
        }
    }

    static void SlideDoor(Transform door, int direction, float fraction)
    {
        var position = door.localPosition;
        position.x = ShaftX + direction * (DoorOffset + DoorSlide * fraction);
        door.localPosition = position;
    }

    int NearbyLanding()
    {
        if (!player || !player.gameObject.activeInHierarchy) return 0;
        var p = transform.InverseTransformPoint(player.transform.position);
        if (Mathf.Abs(p.x - ShaftX) > 1.3f || p.z < 3.52f || p.z > 5.28f) return 0;
        for (int floor = 1; floor <= landings.Length; floor++)
            if (Mathf.Abs(p.y - StopHeight(floor)) < .55f) return floor;
        return 0;
    }

    bool IsInsideCabin()
    {
        if (!player || !player.gameObject.activeInHierarchy) return false;
        var p = transform.InverseTransformPoint(player.transform.position);
        return Mathf.Abs(p.y - cabin.localPosition.y) < 1f && Mathf.Abs(p.x - ShaftX) <= 1.04f &&
            p.z >= .53f && p.z <= CabinSafeFrontZ;
    }

    bool DoorwayOccupied()
    {
        if (!player || !player.gameObject.activeInHierarchy) return false;
        var p = transform.InverseTransformPoint(player.transform.position);
        return Mathf.Abs(p.y - StopHeight(currentFloor)) < 1.1f && Mathf.Abs(p.x - ShaftX) < 1.23f &&
            p.z > CabinSafeFrontZ && p.z < LandingZ + player.radius + .065f;
    }

    void OnGUI()
    {
        if (!overview.IsWalking) return;
        int landing = NearbyLanding();
        string message;
        if (carryingPlayer) message = $"Lift moving to floor {destinationFloor}";
        else if (IsInsideCabin()) message = $"Floor {currentFloor} · Press 1–{FloorCount} to select a floor";
        else if ((phase == Phase.Open || phase == Phase.Opening || phase == Phase.Closing) && DoorwayOccupied())
            message = "Step fully inside or return to the corridor";
        else if (landing == 0) return;
        else if (IsMoving || phase == Phase.Closing) message = $"Lift moving to floor {destinationFloor}";
        else if (currentFloor == landing && (phase == Phase.Open || phase == Phase.Opening)) message = $"Step inside · Press 1–{FloorCount}";
        else message = "E   Call lift / Open doors";

        if (promptStyle == null)
        {
            promptFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 18);
            promptStyle = new GUIStyle { font = promptFont, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            promptStyle.normal.textColor = new Color(.88f, .97f, .94f);
        }
        float scale = Mathf.Clamp(Screen.height / 900f, .7f, 1.8f);
        promptStyle.fontSize = Mathf.RoundToInt(scale * 16f);
        float width = Mathf.Min(Screen.width - 16f, 390f * scale);
        var rect = new Rect((Screen.width - width) * .5f, Screen.height - 83f * scale, width, 44f * scale);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0,
            new Color(.045f, .078f, .1f, .92f), 0, 8f * scale);
        GUI.Label(rect, message, promptStyle);
    }

    void OnDestroy()
    {
        if (promptFont) Destroy(promptFont);
    }
}
