using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Perspective orbit overview with an optional existing first-person tour.</summary>
[RequireComponent(typeof(Camera), typeof(AudioListener))]
public class JEIStructureView : MonoBehaviour
{
    public Transform target;
    public GameObject walkPlayer;
    [Tooltip("Optional floors for 1/2/3 isolated structural inspection; 0 restores all floors.")]
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
    GUIStyle hintStyle;
    int selectedFloor;
    readonly System.Collections.Generic.Dictionary<Renderer, bool> floorRenderers = new System.Collections.Generic.Dictionary<Renderer, bool>();

    Vector3 FocusPoint { get { return target != null ? target.position : Vector3.zero; } }

    void Start()
    {
        overviewCamera = GetComponent<Camera>();
        overviewListener = GetComponent<AudioListener>();
        overviewCamera.orthographic = false;
        foreach (var floor in floors)
            if (floor != null)
                foreach (var renderer in floor.GetComponentsInChildren<Renderer>(true))
                    floorRenderers[renderer] = renderer.enabled;

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
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            SetWalking(!walking);

        if (walking) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && floors.Length > 0)
        {
            if (keyboard.digit0Key.wasPressedThisFrame) SetFloor(0);
            if (keyboard.digit1Key.wasPressedThisFrame) SetFloor(1);
            if (keyboard.digit2Key.wasPressedThisFrame) SetFloor(2);
            if (keyboard.digit3Key.wasPressedThisFrame) SetFloor(3);
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

    void SetWalking(bool value)
    {
        // Do not leave the user without a view if the optional player has not been assigned.
        if (value && (walkPlayer == null || walkCameras.Length == 0)) return;
        walking = value;
        if (walking)
        {
            // Isolation is visual only. Walking always restores the complete building.
            SetFloor(0);
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
        selectedFloor = number;
        for (int i = 0; i < floors.Length; i++)
        {
            if (floors[i] == null) continue;
            foreach (var renderer in floors[i].GetComponentsInChildren<Renderer>(true))
                if (floorRenderers.TryGetValue(renderer, out bool enabled))
                    renderer.enabled = enabled && (number == 0 || number == i + 1);
        }
        if (target != null)
            target.position = number == 0
                ? (floors[0].position + floors[floors.Length - 1].position) / 2 + Vector3.up * .7f
                : floors[number - 1].position + Vector3.up;
        ApplyOrbit();
    }

    void OnDisable()
    {
        foreach (var pair in floorRenderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
    }

    void OnGUI()
    {
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 13;
            hintStyle.normal.textColor = Color.white;
            hintStyle.padding = new RectOffset(11, 9, 8, 6);
        }
        GUI.Box(new Rect(16, 16, 460, floors.Length > 0 && !walking ? 86 : 65), GUIContent.none);
        string hint = walking
            ? "WALK MODE  |  WASD: move  |  Mouse: look\nEsc: free cursor  |  Tab: return to 3D overview"
            : "3D OVERVIEW  |  Drag left / right mouse: orbit\nScroll: zoom  |  Tab: enter first-person tour";
        if (!walking && floors.Length > 0) hint += "\n1 / 2 / 3: isolate floor  |  0: all floors  |  " + (selectedFloor == 0 ? "ALL" : "F" + selectedFloor);
        GUI.Label(new Rect(16, 16, 460, 86), hint, hintStyle);
    }
}
