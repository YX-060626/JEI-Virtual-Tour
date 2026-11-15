using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A compact status card with an expandable keyboard guide.</summary>
[RequireComponent(typeof(JEIStructureView))]
public class JEITourHUD : MonoBehaviour
{
    const float CardWidth = 312f;
    const float CompactHeight = 91f;
    static readonly string[,] WalkingControls =
    {
        { "WASD", "Move · Mouse to look" },
        { "SPACE", "Jump" },
        { "SHIFT", "Toggle walk / run" },
        { "V", "Switch camera view" },
        { "TAB", "Building overview" },
        { "F", "Open / close a room door" },
        { "E", "Call lift · 1–3 inside" },
        { "ESC", "Release mouse" }
    };
    static readonly string[,] OverviewControls =
    {
        { "MOUSE", "Drag to orbit · Scroll to zoom" },
        { "1  2  3", "View one floor" },
        { "0", "Show all floors" },
        { "R", "Toggle roof" },
        { "TAB", "Start walking" }
    };

    public bool Expanded { get; private set; }
    JEIStructureView overview;
    JEITourController tour;
    Texture2D compactWood, walkingWood, overviewWood, badge;
    GUIStyle title, small, body, keyStyle;

    void Awake()
    {
        overview = GetComponent<JEIStructureView>();
        if (overview.walkPlayer) tour = overview.walkPlayer.GetComponent<JEITourController>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) ToggleHelp();
    }

    public void ToggleHelp() => Expanded = !Expanded;

    void Init()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title = Style(font, 20, FontStyle.Bold);
        small = Style(font, 12);
        body = Style(font, 14);
        keyStyle = Style(font, 12, FontStyle.Bold);
        keyStyle.alignment = TextAnchor.MiddleCenter;
        compactWood = CardTexture((int)CardWidth, (int)CompactHeight, 8, true);
        badge = CardTexture(76, 24, 4, false);
    }

    static GUIStyle Style(Font font, int size, FontStyle weight = FontStyle.Normal)
    {
        var style = new GUIStyle { font = font, fontSize = size, fontStyle = weight, alignment = TextAnchor.MiddleLeft };
        style.normal.textColor = Color.white;
        return style;
    }

    static Texture2D CardTexture(int width, int height, float radius, bool useGrain)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = useGrain ? "Natural oak HUD background" : "HUD key background",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float xInset = Mathf.Min(x + .5f, width - x - .5f);
                float yInset = Mathf.Min(y + .5f, height - y - .5f);
                float dx = Mathf.Max(0, radius - xInset);
                float dy = Mathf.Max(0, radius - yInset);
                float coverage = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                if (!useGrain)
                {
                    pixels[y * width + x] = new Color32(103, 70, 44, (byte)(220 * coverage));
                    continue;
                }

                // Stretch the noise along the board so the grain follows the long edge.
                float grain = Mathf.PerlinNoise(x * .011f, y * .27f) - .5f;
                float rings = Mathf.Sin(y * .62f + Mathf.PerlinNoise(x * .009f, y * .07f) * 4f);
                float tone = (grain * 20f) + (rings * 2.4f) + (y / (float)height - .5f) * 7f;
                if (Mathf.Min(xInset, yInset) < 1.5f) tone += y >= height / 2f ? 12f : -10f;
                pixels[y * width + x] = new Color32((byte)(148 + tone), (byte)(107 + tone), (byte)(70 + tone), (byte)(253 * coverage));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    void Badge(Rect rect, string label)
    {
        GUI.DrawTexture(rect, badge);
        GUI.Label(rect, label, keyStyle);
    }

    void OnGUI()
    {
        if (!overview || Event.current.type != EventType.Repaint) return;
        if (!compactWood) Init();

        bool walking = overview.IsWalking;
        string[,] controls = walking ? WalkingControls : OverviewControls;
        int rows = controls.GetLength(0);
        float height = Expanded ? 106f + rows * 31f + (walking ? 24f : 0f) : CompactHeight;
        Texture2D panel = compactWood;
        if (Expanded)
        {
            if (walking && !walkingWood) walkingWood = CardTexture((int)CardWidth, (int)height, 8, true);
            if (!walking && !overviewWood) overviewWood = CardTexture((int)CardWidth, (int)height, 8, true);
            panel = walking ? walkingWood : overviewWood;
        }

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        float scale = Mathf.Clamp(Screen.height / 900f, .65f, 2f);
        scale = Mathf.Min(scale, Screen.width / (CardWidth + 36f), Screen.height / (height + 36f));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(18, 18, CardWidth, height), panel);
        GUI.Label(new Rect(34, 27, 245, 17), "JEI  /  VIRTUAL TOUR", small);
        string mode = walking ? (tour && tour.IsThirdPerson ? "Third-person tour" : "First-person tour") : "Building overview";
        string status = walking ? (tour && tour.IsRunning ? "Running" : "Walking") :
            (overview.SelectedFloor == 0 ? "All floors" : $"Floor {overview.SelectedFloor}");
        GUI.Label(new Rect(34, 45, 208, 28), mode, title);
        Badge(new Rect(246, 47, 68, 24), status);
        GUI.Label(new Rect(34, 78, 260, 19), Expanded ? "F1  Hide controls" : "F1  Show controls", small);

        if (Expanded)
        {
            GUI.color = new Color(1, 1, 1, .25f);
            GUI.DrawTexture(new Rect(34, 105, 280, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            for (int row = 0; row < rows; row++)
            {
                float y = 116 + row * 31f;
                Badge(new Rect(34, y, 64, 24), controls[row, 0]);
                GUI.Label(new Rect(110, y, 205, 24), controls[row, 1], body);
            }
            if (walking) GUI.Label(new Rect(34, 118 + rows * 31f, 270, 22), "Click the view to resume the tour", small);
        }

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    void OnDestroy()
    {
        if (compactWood) Destroy(compactWood);
        if (walkingWood) Destroy(walkingWood);
        if (overviewWood) Destroy(overviewWood);
        if (badge) Destroy(badge);
    }
}
