using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class JEITourController : MonoBehaviour
{
    public Transform view;
    public float speed = 3.2f;
    float pitch, verticalSpeed;
    CharacterController controller;
    void Start() { controller = GetComponent<CharacterController>(); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void Update() {
        if (Keyboard.current == null || view == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked) {
            Vector2 delta = Mouse.current.delta.ReadValue() * .12f;
            transform.Rotate(0, delta.x, 0); pitch = Mathf.Clamp(pitch - delta.y, -80, 80); view.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (Cursor.lockState != CursorLockMode.Locked) return;
        var k = Keyboard.current;
        Vector2 input = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
        input = Vector2.ClampMagnitude(input, 1);
        verticalSpeed = controller.isGrounded ? -2 : verticalSpeed - 9.81f * Time.deltaTime;
        controller.Move((transform.right * input.x * speed + transform.forward * input.y * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
    }
}
