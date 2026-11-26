using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Pixel-aligned tour status and keyboard guide, drawn over the final camera image.</summary>
[RequireComponent(typeof(JEIStructureView))]
public class JEITourHUD : MonoBehaviour
{
    const float CardWidth = 344f;
    const float CompactHeight = 96f;
    const float FirstRow = 104f;
    const float RowSpacing = 29f;
    const float RowHeight = 24f;
    static readonly string[,] WalkingControls =
    {
        { "WASD", "Move · Mouse to look" },
        { "SPACE", "Jump" },
        { "SHIFT", "Toggle walk / run" },
        { "V", "Switch camera view" },
        { "TAB", "Building overview" },
        { "F", "Open / close a room door" },
        { "E", "Call lift" },
        { "ESC", "Release mouse" },
        { "F2 / F3", "Clear / Smooth · Frame stats" }
    };
    static readonly string[,] OverviewControls =
    {
        { "MOUSE", "Drag to orbit · Scroll to zoom" },
        { "", "View one floor" },
        { "0", "Show all floors" },
        { "R", "Toggle roof" },
        { "TAB", "Start walking" },
        { "F2 / F3", "Clear / Smooth · Frame stats" }
    };

    public bool Expanded { get; private set; }

    JEIStructureView overview;
    JEITourController tour;
    JEILiftController lift;
    JEITourPerformance performance;
    TMP_FontAsset font;
    Canvas canvas;
    RawImage panel, statusBadge, separator;
    TextMeshProUGUI heading, title, status, hint, footer;
    readonly RawImage[] rowBadges = new RawImage[WalkingControls.GetLength(0)];
    readonly TextMeshProUGUI[] rowKeys = new TextMeshProUGUI[WalkingControls.GetLength(0)];
    readonly TextMeshProUGUI[] rowLabels = new TextMeshProUGUI[WalkingControls.GetLength(0)];
    Texture2D compactWood, walkingWood, overviewWood, badge;
    int layoutWidth, layoutHeight;
    bool layoutWalking, layoutExpanded;
    float scale;

    void Awake()
    {
        overview = GetComponent<JEIStructureView>();
        if (overview.walkPlayer) tour = overview.walkPlayer.GetComponent<JEITourController>();
        lift = FindFirstObjectByType<JEILiftController>();
        performance = GetComponent<JEITourPerformance>();
    }

    void OnEnable()
    {
        if (!canvas) BuildCanvas();
        canvas.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        if (canvas) canvas.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) ToggleHelp();

        bool walking = overview.IsWalking;
        if (layoutWidth != Screen.width || layoutHeight != Screen.height ||
            layoutWalking != walking || layoutExpanded != Expanded)
        {
            Layout(walking);
        }

        title.text = walking
            ? (tour && tour.IsThirdPerson ? "Third-person tour" : "First-person tour")
            : "Building overview";
        status.text = walking
            ? (tour && tour.IsRunning ? "Running" : "Walking")
            : (overview.SelectedFloor == 0 ? "All floors" : $"Floor {overview.SelectedFloor}");
    }

    public void ToggleHelp() => Expanded = !Expanded;

    void BuildCanvas()
    {
        // Overlay Canvas is composited after the lower-resolution 3D tour view.
        var root = new GameObject("Tour HUD", typeof(RectTransform), typeof(Canvas));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = 100;

        font = TMP_Settings.defaultFontAsset;
        compactWood = CardTexture((int)CardWidth, (int)CompactHeight, 8f, true);
        badge = CardTexture(76, (int)RowHeight, 4f, false);
        panel = CreateImage("Status card", compactWood, canvas.transform);

        Transform card = panel.transform;
        heading = CreateLabel("Heading", FontStyles.Bold, TextAlignmentOptions.MidlineLeft, card);
        title = CreateLabel("Tour mode", FontStyles.Bold, TextAlignmentOptions.MidlineLeft, card);
        statusBadge = CreateImage("Status badge", badge, card);
        status = CreateLabel("Status", FontStyles.Bold, TextAlignmentOptions.Midline, statusBadge.transform);
        hint = CreateLabel("Help shortcut", FontStyles.Normal, TextAlignmentOptions.MidlineLeft, card);
        separator = CreateImage("Separator", Texture2D.whiteTexture, card);
        separator.color = new Color(1f, 1f, 1f, .25f);
        footer = CreateLabel("Tour navigation note", FontStyles.Normal, TextAlignmentOptions.MidlineLeft, card);
        heading.text = "JEI  /  VIRTUAL TOUR";

        for (int row = 0; row < rowBadges.Length; row++)
        {
            rowBadges[row] = CreateImage($"Shortcut {row + 1}", badge, card);
            rowKeys[row] = CreateLabel("Key", FontStyles.Bold, TextAlignmentOptions.Midline, rowBadges[row].transform);
            rowLabels[row] = CreateLabel($"Action {row + 1}", FontStyles.Normal, TextAlignmentOptions.MidlineLeft, card);
        }

        Layout(overview.IsWalking);
    }

    void Layout(bool walking)
    {
        layoutWidth = Screen.width;
        layoutHeight = Screen.height;
        layoutWalking = walking;
        layoutExpanded = Expanded;

        string[,] controls = walking ? WalkingControls : OverviewControls;
        bool hideRoofShortcut = !walking && !overview.CanToggleRoof;
        bool showPerformance = performance && performance.enabled && performance.tourPipeline;
        int rows = controls.GetLength(0) - (hideRoofShortcut ? 1 : 0) - (showPerformance ? 0 : 1);
        float footerY = FirstRow + rows * RowSpacing + 3f;
        float height = Expanded
            ? (walking ? footerY + 34f : FirstRow + rows * RowSpacing + 9f)
            : CompactHeight;
        scale = Mathf.Min(Mathf.Clamp(layoutHeight / 900f, .9f, 1.6f),
            layoutWidth / (CardWidth + 36f), layoutHeight / (height + 36f));

        panel.texture = PanelTexture(walking, height);
        Place(panel, 18, 18, CardWidth, height);
        PlaceLabel(heading, 16, 8, 264, 21, 13);
        PlaceLabel(title, 16, 28, 235, 32, 23);
        Place(statusBadge, 252, 32, 76, RowHeight);
        PlaceLabel(status, 0, 0, 76, RowHeight, 13);
        PlaceLabel(hint, 16, 65, 310, 22, 14);
        hint.text = Expanded ? "F1  Hide controls" : "F1  Show controls";
        separator.gameObject.SetActive(Expanded);
        footer.gameObject.SetActive(Expanded && walking);

        Place(separator, 16, 93, 312, 1);
        PlaceLabel(footer, 16, footerY, 312, 23, 13);
        int lastFloor = overview.floors.Length;
        footer.text = "Click the view to resume the tour";

        for (int row = 0; row < rowBadges.Length; row++)
        {
            bool show = Expanded && row < rows;
            rowBadges[row].gameObject.SetActive(show);
            rowLabels[row].gameObject.SetActive(show);
            if (!show) continue;

            float y = FirstRow + row * RowSpacing;
            Place(rowBadges[row], 16, y, 70, RowHeight);
            PlaceLabel(rowKeys[row], 0, 0, 70, RowHeight, 13);
            PlaceLabel(rowLabels[row], 98, y, 230, RowHeight, 15);
            int controlRow = hideRoofShortcut && row >= 3 ? row + 1 : row;
            string key = controls[controlRow, 0];
            rowKeys[row].text = !walking && controlRow == 1 ? $"1–{lastFloor}" : key;
            rowLabels[row].text = walking && key == "E" && lift
                ? $"Call lift · Select floors 1–{lift.FloorCount}"
                : controls[controlRow, 1];
        }
    }

    Texture2D PanelTexture(bool walking, float height)
    {
        if (!Expanded) return compactWood;
        if (walking)
        {
            if (!walkingWood) walkingWood = CardTexture((int)CardWidth, (int)height, 8f, true);
            return walkingWood;
        }

        if (!overviewWood) overviewWood = CardTexture((int)CardWidth, (int)height, 8f, true);
        return overviewWood;
    }

    static RawImage CreateImage(string name, Texture texture, Transform parent)
    {
        var element = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        element.transform.SetParent(parent, false);
        element.texture = texture;
        element.raycastTarget = false;
        return element;
    }

    TextMeshProUGUI CreateLabel(string name, FontStyles weight, TextAlignmentOptions alignment, Transform parent)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(parent, false);
        var label = item.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontStyle = weight;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    void Place(Graphic item, float x, float y, float width, float height)
    {
        RectTransform rect = item.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = new Vector2(Mathf.Round(x * scale), -Mathf.Round(y * scale));
        rect.sizeDelta = new Vector2(Mathf.Max(1f, Mathf.Round(width * scale)), Mathf.Max(1f, Mathf.Round(height * scale)));
    }

    void PlaceLabel(TMP_Text item, float x, float y, float width, float height, int size)
    {
        Place(item, x, y, width, height);
        item.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * scale));
    }

    static Texture2D CardTexture(int width, int height, float radius, bool useGrain)
    {
        const int density = 2;
        var texture = new Texture2D(width * density, height * density, TextureFormat.RGBA32, false)
        {
            name = useGrain ? "Oak HUD background" : "HUD key background",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[texture.width * texture.height];
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                float sampleX = (x + .5f) / density;
                float sampleY = (y + .5f) / density;
                float xInset = Mathf.Min(sampleX, width - sampleX);
                float yInset = Mathf.Min(sampleY, height - sampleY);
                float dx = Mathf.Max(0, radius - xInset);
                float dy = Mathf.Max(0, radius - yInset);
                float coverage = Mathf.Clamp01((radius - Mathf.Sqrt(dx * dx + dy * dy)) * density + .5f);
                if (!useGrain)
                {
                    pixels[y * texture.width + x] = new Color32(66, 41, 24, (byte)(255 * coverage));
                    continue;
                }

                float grain = Mathf.PerlinNoise(sampleX * .011f, sampleY * .27f) - .5f;
                float rings = Mathf.Sin(sampleY * .62f + Mathf.PerlinNoise(sampleX * .009f, sampleY * .07f) * 4f);
                float tone = grain * 14f + rings * 1.5f + (sampleY / height - .5f) * 7f;
                if (Mathf.Min(xInset, yInset) < 1f) tone += sampleY >= height / 2f ? 8f : -7f;
                pixels[y * texture.width + x] = new Color32((byte)(112 + tone), (byte)(75 + tone), (byte)(46 + tone), (byte)(255 * coverage));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    void OnDestroy()
    {
        if (canvas) Destroy(canvas.gameObject);
        if (compactWood) Destroy(compactWood);
        if (walkingWood) Destroy(walkingWood);
        if (overviewWood) Destroy(overviewWood);
        if (badge) Destroy(badge);
    }
}
