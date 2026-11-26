using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Perspective orbit overview with an optional existing first-person tour.</summary>
[RequireComponent(typeof(Camera), typeof(AudioListener))]
public class JEIStructureView : MonoBehaviour
{
    public Transform target;
    public GameObject walkPlayer;
    public Transform roof;
    public bool startInFirstPerson;
    public bool IsWalking { get { return walking; } }
    public bool CanToggleRoof => roof != null && floors.Length < 6;
    [Tooltip("Optional floors for number key isolation; 0 restores all floors.")]
    public Transform[] floors = new Transform[0];
    public float distance = 48f;
    public float yaw = -35f;
    public float pitch = 45f;
    [Tooltip("Preserve the scene camera position by deriving the initial orbit from it.")]
    public bool useInitialCameraPose = true;
    public float orbitSensitivity = .2f;
    public float zoomSensitivity = .025f;

    Camera overviewCamera;
    AudioListener overviewListener;
    Camera[] walkCameras;
    AudioListener[] walkListeners;
    bool walking;
    int selectedFloor;
    bool showRoof = true;
    readonly struct FloorRenderer
    {
        public readonly Renderer Renderer;
        public readonly bool OriginallyEnabled;

        public FloorRenderer(Renderer renderer)
        {
            Renderer = renderer;
            OriginallyEnabled = renderer.enabled;
        }
    }

    FloorRenderer[][] floorRenderers;

    Vector3 FocusPoint { get { return target != null ? target.position : Vector3.zero; } }

    void Start()
    {
        overviewCamera = GetComponent<Camera>();
        overviewListener = GetComponent<AudioListener>();
        overviewCamera.orthographic = false;
        floorRenderers = new FloorRenderer[floors.Length][];
        for (int i = 0; i < floors.Length; i++)
        {
            if (!floors[i]) continue;
            var renderers = floors[i].GetComponentsInChildren<Renderer>(true);
            floorRenderers[i] = System.Array.ConvertAll(renderers, renderer => new FloorRenderer(renderer));
        }

        // Keep this component active in both modes so Tab can always return to overview.
        if (walkPlayer != null && (walkPlayer == gameObject || transform.IsChildOf(walkPlayer.transform)))
        {
            Debug.LogWarning("JEIStructureView: walkPlayer must not contain the overview camera.", this);
            walkPlayer = null;
        }
        walkCameras = walkPlayer != null ? walkPlayer.GetComponentsInChildren<Camera>(true) : new Camera[0];
        walkListeners = walkPlayer != null ? walkPlayer.GetComponentsInChildren<AudioListener>(true) : new AudioListener[0];
        if (walkPlayer != null) walkPlayer.SetActive(false);

        // The generated scene can contain inactive cameras from the original prototype.
        foreach (Camera other in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (other != overviewCamera) other.enabled = false;
        foreach (AudioListener other in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (other != overviewListener) other.enabled = false;

        if (useInitialCameraPose)
        {
            Vector3 offset = transform.position - FocusPoint;
            if (offset.sqrMagnitude > .01f)
            {
                distance = offset.magnitude;
                pitch = Mathf.Asin(Mathf.Clamp(offset.y / distance, -1f, 1f)) * Mathf.Rad2Deg;
                yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            }
        }
        pitch = Mathf.Clamp(pitch, 20f, 80f);
        distance = Mathf.Clamp(distance, 20f, 80f);
        overviewCamera.enabled = true;
        overviewListener.enabled = true;
        ReleaseCursor();
        ApplyOrbit();
        if (startInFirstPerson) SetWalking(true);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            SetWalking(!walking);

        if (walking) return;
        var keyboard = Keyboard.current;
        if (CanToggleRoof && keyboard != null && keyboard.rKey.wasPressedThisFrame) SetRoofVisible(!showRoof);
        if (keyboard != null && floors.Length > 0)
        {
            if (keyboard.digit0Key.wasPressedThisFrame) SetFloor(0);
            if (keyboard.digit1Key.wasPressedThisFrame) SetFloor(1);
            if (keyboard.digit2Key.wasPressedThisFrame) SetFloor(2);
            if (keyboard.digit3Key.wasPressedThisFrame) SetFloor(3);
            if (keyboard.digit4Key.wasPressedThisFrame) SetFloor(4);
            if (keyboard.digit5Key.wasPressedThisFrame) SetFloor(5);
            if (keyboard.digit6Key.wasPressedThisFrame) SetFloor(6);
        }
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        if (mouse.leftButton.isPressed || mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * orbitSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, 20f, 80f);
        }
        distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * zoomSensitivity, 20f, 80f);
    }

    void LateUpdate()
    {
        if (!walking) ApplyOrbit();
    }

    void ApplyOrbit()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(FocusPoint + rotation * Vector3.back * distance, rotation);
    }

    public void SetWalking(bool value)
    {
        // Do not leave the user without a view if the optional player has not been assigned.
        if (value && (walkPlayer == null || walkCameras.Length == 0)) return;
        walking = value;
        if (walking)
        {
            // Isolation is visual only. Walking always restores the complete building.
            SetFloor(0);
            SetRoofVisible(true);
            overviewCamera.enabled = false;
            overviewListener.enabled = false;
            for (int i = 0; i < walkCameras.Length; i++) walkCameras[i].enabled = i == 0;
            for (int i = 0; i < walkListeners.Length; i++) walkListeners[i].enabled = i == 0;
            walkPlayer.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            walkPlayer.SetActive(false);
            for (int i = 0; i < walkCameras.Length; i++) walkCameras[i].enabled = false;
            for (int i = 0; i < walkListeners.Length; i++) walkListeners[i].enabled = false;
            overviewCamera.enabled = true;
            overviewListener.enabled = true;
            ReleaseCursor();
            ApplyOrbit();
        }
    }

    static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetFloor(int number)
    {
        if (number < 0 || number > floors.Length || floors.Length == 0) return;
        int previous = selectedFloor;
        selectedFloor = number;
        UpdateRoofVisibility();

        if (floorRenderers != null && number != previous)
        {
            if (number == 0 || previous == 0)
            {
                for (int i = 0; i < floorRenderers.Length; i++)
                    SetFloorRenderers(i, number == 0 || i == number - 1);
            }
            else
            {
                SetFloorRenderers(previous - 1, false);
                SetFloorRenderers(number - 1, true);
            }
        }
        if (target != null)
            target.position = number == 0
                ? (floors[0].position + floors[floors.Length - 1].position) / 2 + Vector3.up * .7f
                : floors[number - 1].position + Vector3.up;
        ApplyOrbit();
    }

    void SetFloorRenderers(int index, bool visible)
    {
        var renderers = floorRenderers[index];
        if (renderers == null) return;
        foreach (var item in renderers)
        {
            bool enabled = visible && item.OriginallyEnabled;
            if (item.Renderer && item.Renderer.enabled != enabled) item.Renderer.enabled = enabled;
        }
    }

    void OnDisable()
    {
        if (floorRenderers != null)
            for (int i = 0; i < floorRenderers.Length; i++) SetFloorRenderers(i, true);
        selectedFloor = 0;
        if (roof != null) foreach (var renderer in roof.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
    }

    public void SetRoofVisible(bool visible)
    {
        showRoof = visible;
        UpdateRoofVisibility();
    }

    void UpdateRoofVisibility()
    {
        if (roof != null)
            foreach (var renderer in roof.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = walking || (selectedFloor == 0 && showRoof);
    }

    public int SelectedFloor { get { return selectedFloor; } }
    public bool RoofVisible { get { return showRoof; } }
}
