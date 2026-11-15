using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Routes the walking player's F key to one nearby room door.</summary>
[RequireComponent(typeof(JEITourController))]
[DefaultExecutionOrder(50)]
public sealed class JEIRoomInteraction : MonoBehaviour
{
    JEITourController tour;
    GUIStyle promptStyle;
    float blockedUntil;

    public JEIRoomDoor Target { get; private set; }

    void Awake() => tour = GetComponent<JEITourController>();
    void OnDisable() => Target = null;

    void LateUpdate() => ReadDoorInput();

    void ReadDoorInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Target = null;
            return;
        }

        var next = JEIRoomDoor.FindLookTarget(tour);
        if (next != Target) blockedUntil = 0f;
        Target = next;
        if (Target && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            blockedUntil = Target.TryToggle() ? 0f : Time.unscaledTime + 1.1f;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !Target || Cursor.lockState != CursorLockMode.Locked) return;
        const float width = 174f;
        const float height = 36f;
        float scale = Mathf.Clamp(Screen.height / 900f, .65f, 2f);
        var rect = new Rect((Screen.width - width * scale) * .5f, Screen.height * .58f, width * scale, height * scale);
        var previousColor = GUI.color;
        GUI.color = new Color(.17f, .12f, .09f, .91f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            promptStyle.normal.textColor = Color.white;
        }
        promptStyle.fontSize = Mathf.RoundToInt(15f * scale);
        string text = Time.unscaledTime < blockedUntil ? "Door blocked" :
            Target.IsBlocked ? "Door blocked · F reverse" : Target.TargetOpen ? "F   Close door" : "F   Open door";
        GUI.Label(rect, text, promptStyle);
        GUI.color = previousColor;
    }
}
