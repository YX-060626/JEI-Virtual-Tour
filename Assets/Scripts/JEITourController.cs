using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>One physical player and one camera shared by both walking perspectives.</summary>
[RequireComponent(typeof(CharacterController))]
public class JEITourController : MonoBehaviour
{
    public Transform view;
    public Transform avatar;
    public float speed = 3.2f;
    public float runSpeed = 5.8f;
    public float jumpHeight = .9f;
    public float gravity = 18f;
    public bool IsRunning { get; private set; }
    public bool IsGrounded { get { return controller != null && controller.isGrounded; } }
    public float VerticalSpeed { get { return verticalSpeed; } }
    public Vector3 PlanarVelocity { get; private set; }
    public float mouseSensitivity = .12f;
    public float thirdPersonDistance = 2.6f;
    public float cameraRadius = .18f;
    public bool IsThirdPerson { get; private set; }
    public float CameraDistance { get; private set; }
    float pitch, verticalSpeed, followDistance;
    bool ignoreLookFrame;
    CharacterController controller;
    Renderer[] avatarRenderers;
    JEIAvatarMotion avatarMotion;
    float baseStepOffset;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        baseStepOffset = controller.stepOffset;
        avatarRenderers = avatar ? avatar.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        avatarMotion = avatar ? avatar.GetComponent<JEIAvatarMotion>() : null;
    }

    void OnEnable()
    {
        verticalSpeed = 0;
        ignoreLookFrame = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UpdateView(true);
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnApplicationFocus(bool focused) { ignoreLookFrame = true; }

    void Update()
    {
        if (!view) return;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            ignoreLookFrame = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (keyboard != null && keyboard.vKey.wasPressedThisFrame) SetThirdPerson(!IsThirdPerson);
        Vector2 input = Vector2.zero;
        bool jump = false;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (mouse != null && !ignoreLookFrame)
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0, delta.x, 0);
                pitch = Mathf.Clamp(pitch - delta.y, IsThirdPerson ? -55 : -80, IsThirdPerson ? 70 : 80);
            }
            if (keyboard != null)
            {
                if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                    IsRunning = !IsRunning;
                jump = keyboard.spaceKey.wasPressedThisFrame;
                input = Vector2.ClampMagnitude(new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0)), 1);
            }
        }
        MoveCharacter(input, jump, Time.deltaTime);
        ignoreLookFrame = false;
    }

    // Shared by normal input and deterministic collision verification in the editor.
    public void MoveCharacter(Vector2 input, bool jumpPressed, float deltaTime)
    {
        if (controller == null || !controller.enabled || deltaTime <= 0) return;
        input = Vector2.ClampMagnitude(input, 1);
        bool grounded = controller.isGrounded;
        if (grounded && verticalSpeed <= 0) verticalSpeed = -2;
        if (jumpPressed && grounded && verticalSpeed <= 0)
            verticalSpeed = Mathf.Sqrt(2 * gravity * jumpHeight);
        else verticalSpeed = Mathf.Max(verticalSpeed - gravity * deltaTime, -30);
        controller.stepOffset = grounded && verticalSpeed <= 0 ? baseStepOffset : 0;
        Vector3 before = transform.position;
        Vector3 movement = (transform.right * input.x + transform.forward * input.y) * (IsRunning ? runSpeed : speed);
        CollisionFlags hits = controller.Move((movement + Vector3.up * verticalSpeed) * deltaTime);
        if ((hits & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        if ((hits & CollisionFlags.Below) != 0 && verticalSpeed < 0) verticalSpeed = -2;
        PlanarVelocity = Vector3.ProjectOnPlane((transform.position - before) / deltaTime, Vector3.up);
    }

    void LateUpdate()
    {
        if (avatarMotion != null) avatarMotion.Animate(PlanarVelocity, IsRunning, IsGrounded, verticalSpeed, Time.deltaTime);
        UpdateView(false);
    }

    public void SetThirdPerson(bool value)
    {
        IsThirdPerson = value;
        pitch = Mathf.Clamp(pitch, value ? -55 : -80, value ? 70 : 80);
        UpdateView(true);
    }

    void UpdateView(bool snap)
    {
        if (!view) return;
        Quaternion rotation = transform.rotation * Quaternion.Euler(pitch, 0, 0);
        Vector3 eye = transform.TransformPoint(new Vector3(0, 1.65f, 0));
        CameraDistance = 0;
        if (IsThirdPerson)
        {
            Vector3 direction = rotation * Vector3.back;
            float clearDistance = thirdPersonDistance;
            foreach (var hit in Physics.SphereCastAll(eye, cameraRadius, direction, thirdPersonDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                clearDistance = Mathf.Min(clearDistance, Mathf.Max(0, hit.distance - .06f));
            }
            // Retract immediately at walls; ease out when the space opens up again.
            followDistance = snap || clearDistance < followDistance ? clearDistance
                : Mathf.MoveTowards(followDistance, clearDistance, 5 * Time.deltaTime);
            CameraDistance = followDistance;
            view.SetPositionAndRotation(eye + direction * followDistance, rotation);
        }
        else view.SetPositionAndRotation(eye, rotation);
        if (avatarRenderers != null)
            foreach (var renderer in avatarRenderers) renderer.enabled = IsThirdPerson && CameraDistance > .65f;
    }
}
