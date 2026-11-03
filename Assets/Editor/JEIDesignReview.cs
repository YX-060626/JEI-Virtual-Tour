using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class JEIDesignReview
{
    const string Request="Library/JEI_DesignReview.request";
    static JEIDesignReview() { EditorApplication.update+=Poll; }
    static double nextPoll;
    static void Poll() {
        if(EditorApplication.timeSinceStartup<nextPoll)return; nextPoll=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(SceneManager.GetActiveScene().path!="Assets/Scenes/JEI_Floor01_Prototype.unity")return;
        File.Delete(Request);
        try { JEIFloorGenerator.Silent=true; JEIFloorGenerator.Generate(); Capture(); File.WriteAllText("Library/JEI_DesignReview.result","SUCCESS: generated, saved and rendered."); }
        catch(System.Exception e){File.WriteAllText("Library/JEI_DesignReview.result",e.ToString());Debug.LogException(e);}
        finally {JEIFloorGenerator.Silent=false;}
    }
    [MenuItem("Tools/JEI/Capture Design Preview")]
    public static void Capture() {
        var building=GameObject.Find("JEI_Building");
        var g=new GameObject("Temporary review camera");var c=g.AddComponent<Camera>();
        c.backgroundColor=new Color(.23f,.29f,.34f);c.clearFlags=CameraClearFlags.SolidColor;c.nearClipPlane=.1f;c.farClipPlane=150;
        var rt=new RenderTexture(1600,1100,24);var tex=new Texture2D(1600,1100,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try {
            c.transform.position=building.transform.TransformPoint(new Vector3(22,28,-25));c.transform.LookAt(building.transform.position);c.orthographic=true;c.orthographicSize=17;c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1100),0,0);tex.Apply();File.WriteAllBytes("Library/JEI_Overview.png",tex.EncodeToPNG());
            foreach(var roof in Object.FindObjectsByType<JEICeiling>(FindObjectsSortMode.None))roof.GetComponent<Renderer>().enabled=true;
            c.orthographic=false;c.fieldOfView=70;c.transform.position=building.transform.TransformPoint(new Vector3(-10.25f,1.75f,2.6f));c.transform.LookAt(building.transform.TransformPoint(new Vector3(-10.25f,1.3f,8)));c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1100),0,0);tex.Apply();File.WriteAllBytes("Library/JEI_Classroom.png",tex.EncodeToPNG());
            if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(building.transform.position,Quaternion.Euler(55,-35,0),23,false,true);
        } finally {foreach(var roof in Object.FindObjectsByType<JEICeiling>(FindObjectsSortMode.None))roof.GetComponent<Renderer>().enabled=false;RenderTexture.active=old;c.targetTexture=null;Object.DestroyImmediate(g);Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
    }
}
