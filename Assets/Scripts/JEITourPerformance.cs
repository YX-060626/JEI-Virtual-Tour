using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>Applies the selected rendering profile across walking and building overview.</summary>
[DisallowMultipleComponent]
public class JEITourPerformance : MonoBehaviour
{
    public RenderPipelineAsset tourPipeline;
    [SerializeField] RenderPipelineAsset clearPipeline;
    [SerializeField] bool startInSmoothMode = true;

    RenderPipelineAsset previousPipeline;
    int previousVSync, previousTarget;
    bool settingsSaved, profileApplied, showStats;
    ProfilerRecorder drawCalls;
    float elapsed, worst, noticeUntil;
    int frames;
    string stats = "";
    GUIStyle statsStyle;

    public bool IsSmoothMode { get; private set; }

    public void ConfigureProfiles(RenderPipelineAsset clear, RenderPipelineAsset smooth)
    {
        clearPipeline = clear;
        tourPipeline = smooth;
        startInSmoothMode = false;
    }

    void OnEnable()
    {
        if (!Application.isPlaying || (!tourPipeline && !clearPipeline)) return;

        previousPipeline = QualitySettings.renderPipeline;
        previousVSync = QualitySettings.vSyncCount;
        previousTarget = Application.targetFrameRate;
        settingsSaved = true;
        SetSmoothMode(startInSmoothMode);
    }

    public void SetSmoothMode(bool smooth)
    {
        if (!settingsSaved || (profileApplied && IsSmoothMode == smooth)) return;

        var profile = smooth ? tourPipeline : clearPipeline;
        QualitySettings.renderPipeline = profile ? profile : previousPipeline;
        QualitySettings.vSyncCount = profile ? 0 : previousVSync;
        Application.targetFrameRate = profile ? 60 : previousTarget;
        profileApplied = true;
        IsSmoothMode = smooth;
        ResetStatsSample();
    }

    void Update()
    {
        if (!settingsSaved) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
        {
            SetSmoothMode(!IsSmoothMode);
            noticeUntil = Time.unscaledTime + 2.5f;
        }
        if (keyboard != null && keyboard.f3Key.wasPressedThisFrame) SetStatsVisible(!showStats);
        if (!showStats) return;

        float duration = Time.unscaledDeltaTime;
        elapsed += duration;
        worst = Mathf.Max(worst, duration);
        frames++;
        if (elapsed < 1f) return;

        string drawCount = drawCalls.Valid && drawCalls.Count > 0
            ? $"  ·  {drawCalls.LastValue} draws"
            : "";
        stats = $"\n{frames / elapsed:F0} FPS  ·  {elapsed / frames * 1000f:F1} ms average{drawCount}" +
            $"\n{worst * 1000f:F1} ms slowest  ·  F3 to hide";
        elapsed = worst = 0f;
        frames = 0;
    }

    void SetStatsVisible(bool visible)
    {
        if (showStats == visible) return;

        showStats = visible;
        if (visible) drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
        else drawCalls.Dispose();
        ResetStatsSample();
    }

    void ResetStatsSample()
    {
        stats = "\nMeasuring frame time…";
        elapsed = worst = 0f;
        frames = 0;
    }

    void OnGUI()
    {
        if (!settingsSaved || (!showStats && Time.unscaledTime >= noticeUntil) || Event.current.type != EventType.Repaint)
            return;

        statsStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(12, 8, 5, 5),
            normal = { textColor = new Color(.94f, .98f, 1f) }
        };

        float width = Mathf.Min(320f, Screen.width - 36f);
        float height = showStats ? 71f : 35f;
        var card = new Rect(Screen.width - width - 18f, 18f, width, height);
        GUI.DrawTexture(card, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0,
            new Color(.04f, .08f, .12f, .9f), 0, 7f);
        string mode = IsSmoothMode ? "Smooth" : "Clear";
        GUI.Label(card, $"{mode}  ·  F2 to switch" + (showStats ? stats : ""), statsStyle);
    }

    void OnDisable()
    {
        SetStatsVisible(false);
        if (settingsSaved)
        {
            QualitySettings.renderPipeline = previousPipeline;
            QualitySettings.vSyncCount = previousVSync;
            Application.targetFrameRate = previousTarget;
        }
        profileApplied = false;
        settingsSaved = false;
        noticeUntil = 0f;
    }
}
