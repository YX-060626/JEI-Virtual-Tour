using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

// Applies only during this tour. The project's original quality settings are restored on exit.
public class JEITourPerformance : MonoBehaviour
{
    public RenderPipelineAsset tourPipeline;
    RenderPipelineAsset previousPipeline;
    int previousVSync,previousTarget;
    bool applied,showStats;
    float elapsed,worst;
    int frames;
    string stats="Warming up...";
    void OnEnable(){
        if(!Application.isPlaying||tourPipeline==null)return;
        previousPipeline=QualitySettings.renderPipeline;previousVSync=QualitySettings.vSyncCount;previousTarget=Application.targetFrameRate;
        QualitySettings.renderPipeline=tourPipeline;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;applied=true;
    }
    void Update(){
        if(Keyboard.current!=null&&Keyboard.current.f3Key.wasPressedThisFrame)showStats=!showStats;
        float dt=Time.unscaledDeltaTime;elapsed+=dt;frames++;worst=Mathf.Max(worst,dt);
        if(elapsed>=1){stats=$"{frames/elapsed:F0} FPS  |  average {elapsed/frames*1000:F1} ms  |  slowest {worst*1000:F1} ms\nF3: hide performance display";elapsed=0;frames=0;worst=0;}
    }
    void OnGUI(){if(showStats)GUI.Box(new Rect(12,12,470,52),stats);}
    void OnDisable(){if(!applied)return;QualitySettings.renderPipeline=previousPipeline;QualitySettings.vSyncCount=previousVSync;Application.targetFrameRate=previousTarget;applied=false;}
}
