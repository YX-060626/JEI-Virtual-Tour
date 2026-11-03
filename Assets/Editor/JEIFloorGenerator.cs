using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class JEIFloorGenerator : EditorWindow
{
    const string Output = "Assets/Scenes/JEI_Floor01_Prototype.unity";
    public static bool Silent;
    static Transform root;
    static Material wall, floor, wood, blue, dark, green, white, teal, sand, gold;
    static Material Mat(string name, Color color) {
        string dir = "Assets/JEI_Prototype_Materials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "JEI_Prototype_Materials");
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { var shader = Shader.Find("Universal Render Pipeline/Lit"); if (!shader) throw new System.Exception("URP Lit shader unavailable"); m = new Material(shader); m.color = color; AssetDatabase.CreateAsset(m, path); }
        return m;
    }
    static Transform Group(Transform p, string n) { var g = new GameObject(n); g.transform.SetParent(p, false); return g.transform; }
    static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Material m) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(p,false); g.transform.localPosition=pos; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=m; return g;
    }
    static void Wall(Transform p, float x, float z, float sx, float sz) { Box(p,"Wall",new Vector3(x,1.5f,z),new Vector3(sx,3,sz),wall); }
    static void DoorWall(Transform p, float a, float b, float z, float door) {
        float l=door-.65f, r=door+.65f;
        if(l>a) Wall(p,(a+l)/2,z,l-a,.2f); if(b>r) Wall(p,(r+b)/2,z,b-r,.2f);
        Box(p,"Door lintel",new Vector3(door,2.65f,z),new Vector3(1.3f,.7f,.2f),wall);
    }
    static void Label(Transform p,string text,float x,float z) {
        var g=new GameObject("Sign " + text); g.transform.SetParent(p,false); g.transform.localPosition=new Vector3(x,2.45f,z); var t=g.AddComponent<TextMesh>(); t.text=text; t.fontSize=48; t.characterSize=.015f; t.anchor=TextAnchor.MiddleCenter; t.color=Color.white;
    }
    static void Table(Transform p,float x,float z) {
        Box(p,"Table top",new Vector3(x,.75f,z),new Vector3(1.1f,.1f,.6f),wood);
        foreach(float dx in new[]{-.45f,.45f}) foreach(float dz in new[]{-.22f,.22f}) Box(p,"Leg",new Vector3(x+dx,.36f,z+dz),new Vector3(.07f,.72f,.07f),dark);
    }
    static void Chair(Transform p,float x,float z) {
        Box(p,"Chair seat",new Vector3(x,.45f,z),new Vector3(.48f,.12f,.48f),blue);
        Box(p,"Chair back",new Vector3(x,.75f,z-.2f),new Vector3(.48f,.6f,.08f),blue);
        foreach(float dx in new[]{-.18f,.18f}) foreach(float dz in new[]{-.18f,.18f}) Box(p,"Chair leg",new Vector3(x+dx,.22f,z+dz),new Vector3(.05f,.44f,.05f),dark);
    }
    static void Sofa(Transform p,float x,float z) { Box(p,"Sofa seat",new Vector3(x,.225f,z),new Vector3(1.5f,.45f,.65f),blue); Box(p,"Sofa back",new Vector3(x,.75f,z+.28f),new Vector3(1.5f,.65f,.12f),blue); }
    static void Plant(Transform p,float x,float z) { Box(p,"Planter",new Vector3(x,.22f,z),new Vector3(.4f,.44f,.4f),dark); var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name="Plant"; g.transform.SetParent(p,false); g.transform.localPosition=new Vector3(x,.8f,z); g.transform.localScale=new Vector3(.55f,.8f,.55f); g.GetComponent<Renderer>().sharedMaterial=green; }
    static void Decor(Transform p,string name,Vector3 pos,Vector3 size,Material m) { var g=Box(p,name,pos,size,m); Object.DestroyImmediate(g.GetComponent<Collider>()); }
    static void MarkGenerated(Transform t) { t.gameObject.AddComponent<JEIGeneratedObject>(); foreach (Transform c in t) MarkGenerated(c); }
    static void PreserveManual(Transform t, Transform destination) {
        for (int i=t.childCount-1;i>=0;i--) { var c=t.GetChild(i); if (!c.GetComponent<JEIGeneratedObject>()) c.SetParent(destination, true); else PreserveManual(c,destination); }
    }
    public static void GenerateBatch() { EditorSceneManager.OpenScene(Output); Generate(); }
    [MenuItem("Tools/JEI/Generate First Floor Prototype")]
    public static void Generate() {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play mode before generating.");
        var scene=SceneManager.GetActiveScene();
        var building=GameObject.Find("JEI_Building"); var floors=building ? building.transform.Find("Floors") : null; var level=floors ? floors.Find("Floor_01") : null; var slab=level ? level.Find("GroundSlab") : null; var walls=level ? level.Find("Walls") : null; if(!walls && floors) walls=floors.Find("Walls");
        if(!scene.IsValid() || !scene.isLoaded || !building || !floors || !level || !slab || !walls || !walls.Find("BackWall") || !walls.Find("LeftWall") || !walls.Find("RightWall")) { EditorUtility.DisplayDialog("JEI", "Required JEI_Building/Floors/Floor_01, GroundSlab or Walls are missing. Open the source scene first.", "OK"); return; }
        if(scene.path != Output) { if(System.IO.File.Exists(Output)) { EditorUtility.DisplayDialog("JEI", "The prototype scene already exists. Open it to regenerate; it will not be overwritten from another scene.", "OK"); return; } if(!EditorSceneManager.SaveScene(scene,Output,true)) return; scene=EditorSceneManager.OpenScene(Output); building=GameObject.Find("JEI_Building"); floors=building.transform.Find("Floors"); level=floors.Find("Floor_01"); slab=level.Find("GroundSlab"); walls=level.Find("Walls"); if(!walls) walls=floors.Find("Walls"); }
        var existing=level.Find("Prototype_Generated"); if(existing) {
            if (!existing.GetComponent<JEIGeneratedObject>()) { EditorUtility.DisplayDialog("JEI", "Prototype_Generated is not marked as generated; it has been preserved.", "OK"); return; }
            PreserveManual(existing, level); Object.DestroyImmediate(existing.gameObject);
        }
        wall=Mat("Wall",new Color(.8f,.81f,.79f)); floor=Mat("Floor",new Color(.25f,.28f,.31f)); wood=Mat("Wood",new Color(.55f,.36f,.2f)); blue=Mat("Blue",new Color(.16f,.35f,.48f)); dark=Mat("Dark",new Color(.12f,.15f,.18f)); green=Mat("Green",new Color(.2f,.42f,.23f)); white=Mat("White",new Color(.9f,.9f,.86f));
        teal=Mat("Accent_Teal",new Color(.12f,.38f,.41f)); sand=Mat("Warm_Sand",new Color(.68f,.56f,.39f)); gold=Mat("Accent_Gold",new Color(.85f,.57f,.19f));
        walls.SetParent(level,false); slab.localScale=new Vector3(26,.3f,18); slab.GetComponent<Renderer>().sharedMaterial=floor;
        var back=walls.Find("BackWall"); back.localPosition=new Vector3(0,1.5f,8.9f); back.localScale=new Vector3(26,3,.2f);
        var left=walls.Find("LeftWall"); left.localPosition=new Vector3(-12.9f,1.5f,0); left.localScale=new Vector3(.2f,3,18);
        var right=walls.Find("RightWall"); right.localPosition=new Vector3(12.9f,1.5f,0); right.localScale=new Vector3(.2f,3,18);
        foreach(Transform w in walls) { var renderer=w.GetComponent<Renderer>(); if(renderer) renderer.sharedMaterial=wall; }
        root=Group(level,"Prototype_Generated"); var entrance=Group(root,"Entrance"); var lobby=Group(root,"Lobby and Reception"); var corridor=Group(root,"Main Corridor"); var rooms=Group(root,"Rooms"); var light=Group(root,"Lighting");
        DoorWall(entrance,-12.8f,12.8f,-8.9f,0);
        foreach(float side in new[]{-1f,1f}) {
            Box(entrance,"Entrance frame",new Vector3(side*.68f,1.1f,-8.9f),new Vector3(.08f,2.2f,.12f),dark);
            var panel=Box(entrance,"Open blue glass door",new Vector3(side*1.03f,1.08f,-8.52f),new Vector3(.08f,2.1f,.72f),blue);
            panel.GetComponent<Collider>().enabled=false;
        }
        Decor(entrance,"Entrance sign backing",new Vector3(0,2.53f,-8.77f),new Vector3(3.3f,.42f,.05f),teal); Label(entrance,"JEI BUILDING",0,-8.73f);
        Decor(entrance,"Entrance canopy",new Vector3(0,2.88f,-8.7f),new Vector3(4,.12f,.55f),wood);
        Box(lobby,"Reception desk",new Vector3(-3,.55f,-6.7f),new Vector3(2.2f,1.1f,.65f),wood); Chair(lobby,-3,-7.45f); Sofa(lobby,4,-7.9f); Plant(lobby,6,-8);
        Decor(lobby,"Reception accent",new Vector3(-3,.58f,-6.35f),new Vector3(1.8f,.32f,.03f),teal);
        Decor(lobby,"Lobby rug",new Vector3(4,.012f,-6.9f),new Vector3(2.8f,.02f,1.7f),sand); Table(lobby,4,-6.7f);
        Decor(corridor,"Corridor floor runner",new Vector3(0,.012f,0),new Vector3(24,.02f,1.5f),teal);
        // Rooms line a wide central east-west corridor. Door openings face the corridor.
        float[] edges={-12.8f,-7.7f,-2.6f,2.6f,7.7f,12.8f}; string[] north={"Classroom A","Classroom B","Office","Meeting Room","Stairwell"}; string[] south={"Lounge","Restroom","Entrance Lobby","Reception","Facilities"};
        for(int i=0;i<5;i++) {
            float a=edges[i], b=edges[i+1], x=(a+b)/2; var n=Group(rooms,north[i]); DoorWall(n,a,b,2,x); Label(n,north[i],x,2.12f);
            if(i>0) Wall(rooms,a,5.45f,.2f,6.7f);
            Decor(n,"Colour band",new Vector3(x,1.15f,2.105f),new Vector3(1.05f,.18f,.015f),i<2?teal:sand);
            Decor(n,"Room floor inset",new Vector3(x,.012f,5.5f),new Vector3(4.7f,.02f,6.3f),i<2?sand:floor);
            if(i<2) { for(int row=0;row<3;row++) for(int col=0;col<2;col++) { float tx=x-1.1f+col*2.2f, tz=3.3f+row*1.55f; Table(n,tx,tz); Chair(n,tx,tz-.65f); } Box(n,"Blackboard",new Vector3(x,1.7f,8.72f),new Vector3(3,1,.08f),dark); Box(n,"Lectern",new Vector3(x,.55f,7.95f),new Vector3(.65f,1.1f,.55f),wood); }
            if(i==2) { Table(n,x,5); Chair(n,x,4.3f); Box(n,"Monitor",new Vector3(x,1.15f,5),new Vector3(.6f,.45f,.08f),dark); Box(n,"Monitor stand",new Vector3(x,.92f,5),new Vector3(.08f,.25f,.08f),dark); Box(n,"Cabinet",new Vector3(x+1.7f,.6f,8),new Vector3(.6f,1.2f,.6f),white); for(int d=0;d<3;d++) Decor(n,"Drawer handle",new Vector3(x+1.7f,.25f+d*.36f,7.69f),new Vector3(.23f,.035f,.025f),dark); }
            if(i==3) { Box(n,"Meeting table",new Vector3(x,.75f,5.3f),new Vector3(2.5f,.12f,1.1f),wood); foreach(float dx in new[]{-1f,1f}) foreach(float dz in new[]{-.4f,.4f}) Box(n,"Table leg",new Vector3(x+dx,.35f,5.3f+dz),new Vector3(.08f,.7f,.08f),dark); for(int j=-1;j<=1;j++){ Chair(n,x+j,4.3f); Chair(n,x+j,6.4f); } Box(n,"Screen",new Vector3(x,1.7f,8.72f),new Vector3(2,1,.08f),dark); }
            if(i==4) { for(int j=0;j<9;j++) Box(n,"Step " + j,new Vector3(x, (.2f+j*.18f)/2,3.8f+j*.5f),new Vector3(1.4f,.2f+j*.18f,.5f),wood); var rail=Box(n,"Handrail",new Vector3(x+.8f,1.57f,5.8f),new Vector3(.08f,.08f,4.4f),dark); rail.transform.localRotation=Quaternion.Euler(-19.8f,0,0); for(int post=0;post<3;post++) Box(n,"Rail post",new Vector3(x+.8f,.6f+post*.34f,4+post),new Vector3(.07f,1.2f,.07f),dark); Label(n,"FLOOR 01",x,8.72f); }
            if(i<2) { var s=Group(rooms,south[i]); DoorWall(s,a,b,-2,x); if(i==1) Wall(rooms,a,-5.45f,.2f,6.7f); if(i==1) Wall(rooms,b,-5.45f,.2f,6.7f); Label(s,south[i],x,-1.88f); if(i==0){ Sofa(s,x,-7.8f); Table(s,x,-6.3f); Plant(s,x+1.8f,-7.8f); Box(s,"Vending machine",new Vector3(x-1.8f,1,-7.8f),new Vector3(.7f,2,.6f),blue); Decor(s,"Vending display",new Vector3(x-1.8f,1.35f,-7.49f),new Vector3(.5f,.65f,.02f),white); Decor(s,"Vending controls",new Vector3(x-1.8f,.75f,-7.49f),new Vector3(.35f,.12f,.02f),gold); } else { Box(s,"Sink",new Vector3(x-1.5f,.8f,-7.8f),new Vector3(1,.25f,.5f),white); Box(s,"Sink pedestal",new Vector3(x-1.5f,.35f,-7.8f),new Vector3(.2f,.7f,.2f),white); Decor(s,"Mirror",new Vector3(x-1.5f,1.55f,-8.77f),new Vector3(.9f,.7f,.03f),blue); Box(s,"Cubicle divider",new Vector3(x+1,.9f,-7),new Vector3(.1f,1.8f,2),wall); Box(s,"Cubicle door",new Vector3(x+1.8f,1,-6),new Vector3(.8f,2,.08f),blue); } }
        }
        for(int i=-2;i<=2;i++) for(int row=0;row<2;row++) { float lx=i*5, lz=row==0?5.5f:-5.5f; var fixture=Box(light,"Room light fixture",new Vector3(lx,2.88f,lz),new Vector3(.8f,.08f,.45f),white); Object.DestroyImmediate(fixture.GetComponent<Collider>()); var lamp=new GameObject("Room light"); lamp.transform.SetParent(light,false); lamp.transform.localPosition=new Vector3(lx,2.75f,lz); var ll=lamp.AddComponent<Light>(); ll.type=LightType.Point; ll.range=8; ll.intensity=30; }
        for(int i=-2;i<=2;i++){ var g=new GameObject("Ceiling light"); g.transform.SetParent(light,false); g.transform.localPosition=new Vector3(i*5,2.75f,0); var l=g.AddComponent<Light>(); l.type=LightType.Point; l.range=9; l.intensity=45; l.shadows=LightShadows.None; }
        var player=Group(root,"Tour Player"); player.localPosition=new Vector3(0,0,-6); var cc=player.gameObject.AddComponent<CharacterController>(); cc.height=1.8f; cc.center=new Vector3(0,.9f,0); cc.radius=.3f;
        var camera=Group(player,"Tour Camera"); camera.localPosition=new Vector3(0,1.65f,0); camera.gameObject.AddComponent<Camera>(); camera.gameObject.AddComponent<AudioListener>(); player.gameObject.AddComponent<JEITourController>().view=camera;
        foreach(var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if(other != camera.GetComponent<Camera>()) { other.gameObject.SetActive(false); }
        JEIInteriorDesign.Build(root);
        MarkGenerated(root);
        JEIPerformanceOptimizer.Combine(root);
        JEITourPerformanceSetup.Apply();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Selection.activeGameObject=root.gameObject; if (!Application.isBatchMode && !Silent) EditorUtility.DisplayDialog("JEI", "Prototype generated and saved to " + Output, "OK");
    }
}
