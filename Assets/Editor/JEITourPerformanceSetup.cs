using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.IO;

public static class JEITourPerformanceSetup
{
    const string Folder="Assets/JEI_TourSettings";
    const string TourScene = "Assets/Scenes/JEI_Floor_Structure.unity";
    const string TourPipeline = Folder + "/Tour_Pipeline.asset";
    const string ClearPipeline = Folder + "/Tour_Clear_Pipeline.asset";

    public static void TourBatch()
    {
        EditorSceneManager.OpenScene(TourScene);
        ApplyToStructure();
    }

    [MenuItem("Tools/JEI/Tour/Install Display Profiles")]
    public static void ApplyToStructure()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != TourScene || EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
            throw new System.InvalidOperationException("Open and save the structure tour in Edit mode.");
        InstallOnTour(Object.FindFirstObjectByType<JEIStructureView>());
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Could not save the tour performance settings.");
        AssetDatabase.SaveAssets();
        Debug.Log("JEI_TOUR_PERFORMANCE_READY: Clear and Smooth profiles installed for both tour cameras.");
    }

    public static void InstallOnTour(JEIStructureView view)
    {
        if (!view || !view.walkPlayer)
            throw new System.InvalidOperationException("The tour controller or walking camera is missing.");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(TourPipeline);
        if (!pipeline)
            throw new System.InvalidOperationException("The existing smooth tour pipeline asset is missing.");
        var clearPipeline = ConfigureClearPipeline();

        // The overview object stays active when switching between its own camera and the walking player.
        var temporary = view.walkPlayer.GetComponent<JEITourPerformance>();
        if (temporary) Undo.DestroyObjectImmediate(temporary);
        var performance = view.GetComponent<JEITourPerformance>();
        if (!performance) performance = Undo.AddComponent<JEITourPerformance>(view.gameObject);
        Undo.RecordObject(performance, "Install display profiles");
        performance.ConfigureProfiles(clearPipeline, pipeline);
        performance.enabled = true;
        EditorUtility.SetDirty(performance);
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
    }
    static UniversalRenderPipelineAsset ConfigureClearPipeline()
    {
        if (!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ClearPipeline)
            && !AssetDatabase.CopyAsset(TourPipeline, ClearPipeline))
            throw new IOException("Cannot create the clear tour pipeline.");
        var clear = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ClearPipeline);
        clear.renderScale = 1f;
        clear.msaaSampleCount = 2;
        clear.upscalingFilter = UpscalingFilterSelection.Auto;
        clear.fsrOverrideSharpness = false;

        EditorUtility.SetDirty(clear);
        return clear;
    }

    public static void Batch(){
        EditorSceneManager.OpenScene("Assets/Scenes/JEI_Floor01_Prototype.unity");
        Apply();
    }
    [MenuItem("Tools/JEI/Apply Smooth Tour Settings")]
    public static void Apply(){
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/JEI_Floor01_Prototype.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Open the prototype in Edit mode.");
        var player=Object.FindFirstObjectByType<JEITourController>();
        if(!player)throw new System.InvalidOperationException("Tour player missing.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets","JEI_TourSettings");
        string pipelinePath=Folder+"/Tour_Pipeline.asset",rendererPath=Folder+"/Tour_Renderer.asset";
        if(!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath)&&!AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset",pipelinePath))throw new System.IO.IOException("Cannot copy pipeline.");
        if(!AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath)&&!AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset",rendererPath))throw new System.IO.IOException("Cannot copy renderer.");
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        foreach(var feature in renderer.rendererFeatures){feature.SetActive(false);EditorUtility.SetDirty(feature);}
        var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        var data=new SerializedObject(pipe);data.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
        data.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;data.FindProperty("m_SoftShadowQuality").intValue=1;data.ApplyModifiedPropertiesWithoutUndo();
        pipe.renderScale=.85f;pipe.shadowDistance=25;pipe.shadowCascadeCount=2;pipe.mainLightShadowmapResolution=1024;
        pipe.supportsCameraOpaqueTexture=false;pipe.supportsCameraDepthTexture=false;pipe.msaaSampleCount=1;
        EditorUtility.SetDirty(pipe);EditorUtility.SetDirty(renderer);
        var perf=player.GetComponent<JEITourPerformance>();if(!perf)perf=player.gameObject.AddComponent<JEITourPerformance>();perf.tourPipeline=pipe;
        var cam=player.GetComponentInChildren<Camera>();cam.nearClipPlane=.1f;cam.farClipPlane=45;
        var cameraData=cam.GetUniversalAdditionalCameraData();cameraData.requiresColorOption=CameraOverrideOption.Off;cameraData.requiresDepthOption=CameraOverrideOption.Off;cameraData.renderPostProcessing=false;
        var cc=player.GetComponent<CharacterController>();cc.skinWidth=.05f;cc.stepOffset=.25f;cc.minMoveDistance=0;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Library/JEI_TourPerformance.result","PASS: prototype saved; dedicated 0.85-scale pipeline, 1024 shadow map / 2 cascades, renderer features disabled on copied renderer only; original PC settings preserved; F3 frame-time HUD installed.");
    }
}
