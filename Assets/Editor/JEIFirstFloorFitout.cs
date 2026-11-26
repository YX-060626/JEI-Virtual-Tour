using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string FitoutAssets = "Assets/JEI_Fitout";
    const string FitoutDocs = "Documentation/Floor01_Interior";
    static Material plaster, oak, white, metal, teal, blue, yellow, purple, terracotta, leaf, paper, screen, tile, roofSlate;
    static Transform decor;
    static readonly Dictionary<string, float> firstDoors = new Dictionary<string,float> {
        {"N01",-10.7f},{"N02",-5f},{"N03",4.5f},{"N04",13.3f},{"S01",-8f},{"S02",0},{"S03",7.3f},{"S04",13.3f},
        {"C01",-13.3f},{"C02",-9.5f},{"C03",-9.5f},{"C04",0},{"C05",8},{"C06",10.9f},{"C07",8},{"C08",10.9f},{"C09",13.3f}};
    const string FitoutRuntimeKey="JEI_Fitout_Test";

    [InitializeOnLoadMethod]
    static void RegisterFitout() { EditorApplication.update += PollFitout; }

    [MenuItem("Tools/JEI/Interior/Build First Floor and Courtyard Roof")]
    public static void BuildFirstFloorInterior()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var s=SceneManager.GetSceneAt(i); if(!s.isLoaded || !s.isDirty) continue;
            if(!EditorSceneManager.SaveScene(s,"Assets/Scenes/JEI_Recovery_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity",true)) throw new IOException("Cannot preserve unsaved edits.");
            if(!string.IsNullOrEmpty(s.path)) EditorSceneManager.SaveScene(s);
        }
        if(SceneManager.GetActiveScene().path!=ScenePath) EditorSceneManager.OpenScene(ScenePath);
        building=GameObject.Find("JEI_Structure_Study").transform;
        var view=building.GetComponentInChildren<JEIStructureView>(true);
        var first=view.floors[0];
        if(first.Find("06_Interior_Design") || first.Find("06_Approved_Interiors")) throw new InvalidOperationException("Interior exists; manual edits preserved.");
        if(!AssetDatabase.IsValidFolder(FitoutAssets)) AssetDatabase.CreateFolder("Assets","JEI_Fitout");
        Directory.CreateDirectory(FitoutDocs);
        InitFitoutMaterials();
        if(!view.GetComponent<JEITourHUD>()) view.gameObject.AddComponent<JEITourHUD>();
        var interior=Group(first,"06_Interior_Design"); interior.gameObject.AddComponent<JEIGeneratedObject>();
        plan=JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
        foreach(var renderer in first.Find("02_Walls__0.30m_external__0.20m_internal").GetComponentsInChildren<Renderer>(true))
            if(renderer.name=="Wall") renderer.sharedMaterial=plaster;
        foreach(var room in plan.rooms)
        {
            decor=Group(interior,room.id+"__"+room.name);
            FinishRoom(first,room);
            if(room.id=="N01") Classroom(room,false);
            else if(room.id=="N02") Classroom(room,true);
            else if(room.id=="N03") ComputerCollaboration(room);
            else if(room.id=="N04") Seminar(room);
            else if(room.id=="S01") TeachersOffice(room);
            else if(room.id=="S02") Reception(room);
            else if(room.id=="S03") StudentServices(room);
            else if(room.id=="S04") MeetingRoom(room);
            else ServiceRoom(room);
            if(room.kind!="courtyard" && room.kind!="stair") CeilingLight(room);
            CombineRoomDecoration(decor);
        }
        BuildRoomNumbers(view);
        BuildPitchedCourtyardRoof(view.roof);
        Physics.SyncTransforms();
        VerifyFitout();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Cannot save furnished scene.");
        AssetDatabase.SaveAssets();
        CaptureFitout();
        Debug.Log("JEI_FITOUT_BUILD_SUCCESS");
    }
    static Material FitMat(string name,Color color)
    {
        string path=FitoutAssets+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m) return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,path);return m;
    }
    static void InitFitoutMaterials()
    {
        plaster=FitMat("Warm_Plaster",new Color(.87f,.89f,.87f));oak=FitMat("Oak_Furniture",new Color(.64f,.43f,.25f));white=FitMat("Porcelain",new Color(.91f,.92f,.90f));
        metal=FitMat("Graphite_Metal",new Color(.09f,.13f,.17f));teal=FitMat("Deep_Teal",new Color(.10f,.42f,.46f));blue=FitMat("Sky_Blue",new Color(.25f,.55f,.73f));
        yellow=FitMat("Warm_Ochre",new Color(.91f,.68f,.23f));purple=FitMat("Muted_Plum",new Color(.42f,.35f,.51f));terracotta=FitMat("Terracotta",new Color(.78f,.39f,.27f));
        leaf=FitMat("Foliage",new Color(.18f,.42f,.22f));paper=FitMat("Paper",new Color(.95f,.94f,.86f));screen=FitMat("Display",new Color(.22f,.46f,.65f));
        screen.EnableKeyword("_EMISSION");screen.SetColor("_EmissionColor",new Color(.08f,.18f,.25f));
        tile=FitMat("Stone_Tile",new Color(.73f,.78f,.79f));roofSlate=FitMat("Slate_Roof",new Color(.16f,.24f,.29f));
    }
    static Transform Item(string name,float x,float z,float yaw=0)
    { var g=Group(decor,name);g.localPosition=new Vector3(x,0,z);g.localRotation=Quaternion.Euler(0,yaw,0);return g; }
    static GameObject D(Transform p,string name,Vector3 pos,Vector3 size,Material m,bool solid=false)
    { return Box(p,name,pos,size,m,solid); }
    static GameObject Shape(Transform p,string name,PrimitiveType type,Vector3 pos,Vector3 size,Material m)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;
        Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static void Solid(Transform p,Vector3 center,Vector3 size)
    { var c=p.gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size; }
    static void Label(Transform parent,string value,Vector3 position,float size,Color color,float yaw=0)
    {
        var g=Group(parent,"Text__"+value.Replace('\n','_'));g.localPosition=position;g.localRotation=Quaternion.Euler(0,yaw,0);
        var t=g.gameObject.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=64;t.characterSize=size;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
        AssignWorldTextDepth(t);
    }
    static Material FloorFinish(Room r)
    {
        bool wood=r.kind=="office" || r.kind=="collaboration" || r.kind=="teaching";
        var m=FitMat("Floor_"+r.id,Color.white);string texPath=FitoutAssets+(wood?"/Oak_Planks.png":"/Porcelain_Tiles.png");
        if(!File.Exists(texPath))
        {
            var t=new Texture2D(128,128,TextureFormat.RGB24,false);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                bool seam=wood?(y%32<2 || (x+(y/32%2)*64)%128<2):(x%64<2 || y%64<2);
                float n=Mathf.PerlinNoise(x*.16f,y*(wood?.8f:.13f))*.10f;
                Color c=wood?new Color(.64f+n,.46f+n,.29f+n):new Color(.80f+n,.82f+n,.81f+n);
                if(seam)c*=wood?.78f:.86f;t.SetPixel(x,y,c);
            }
            t.Apply();File.WriteAllBytes(texPath,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(texPath);
        }
        m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);m.mainTextureScale=new Vector2((r.x1-r.x0)/2,(r.z1-r.z0)/2);EditorUtility.SetDirty(m);return m;
    }
    static void FinishRoom(Transform first,Room r)
    {
        var room=first.Find("01_Rooms__numbered/"+r.id+"__"+r.name);
        if(room && r.kind!="courtyard" && r.kind!="stair")
        { var f=room.GetComponentInChildren<MeshRenderer>();if(f)f.sharedMaterial=FloorFinish(r); }
        if(!r.id.StartsWith("N") && !r.id.StartsWith("S")) return;
        float outside=r.z1>0?r.z1-.17f:r.z0+.17f;
        Material accent=r.id=="S03"?terracotta:r.kind=="teaching"?blue:r.kind=="office"?teal:yellow;
        D(decor,"Window wall colour band",new Vector3((r.x0+r.x1)/2,.43f,outside),new Vector3(r.x1-r.x0-.35f,.76f,.035f),accent);
        D(decor,"Window wall skirting",new Vector3((r.x0+r.x1)/2,.10f,outside+(outside>0?-.025f:.025f)),new Vector3(r.x1-r.x0-.35f,.14f,.04f),metal);
        foreach(float x in new[]{r.x0+.12f,r.x1-.12f})D(decor,"Side skirting",new Vector3(x,.10f,(r.z0+r.z1)/2),new Vector3(.035f,.14f,r.z1-r.z0-.35f),metal);
        foreach(float x in new[]{r.x0+.38f,r.x1-.38f})
            for(int i=0;i<3;i++)D(decor,"Curtain fold",new Vector3(x+(i-1)*.075f,1.91f,outside+(outside>0?-.11f:.11f)),new Vector3(.065f,1.75f,.09f),white);
    }
    static void CeilingLight(Room r)
    {
        float cx=(r.x0+r.x1)/2,cz=(r.z0+r.z1)/2;int n=r.x1-r.x0>8?2:1;
        for(int i=0;i<n;i++)
        {
            float x=cx+(i-(n-1)/2f)*3.7f;
            D(decor,"Ceiling panel frame",new Vector3(x,3.20f,cz),new Vector3(1.25f,.06f,.55f),metal);
            var luminous=FitMat("Light_Diffuser",new Color(.98f,.97f,.89f));luminous.EnableKeyword("_EMISSION");luminous.SetColor("_EmissionColor",new Color(1,.96f,.85f)*1.2f);
            D(decor,"Ceiling diffuser",new Vector3(x,3.16f,cz),new Vector3(1.16f,.025f,.46f),luminous);
            var g=Group(decor,"Room ambient light");g.localPosition=new Vector3(x,2.9f,cz);var l=g.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=9;l.intensity=1.6f;l.color=new Color(1,.96f,.89f);l.shadows=LightShadows.None;
        }
    }
}
