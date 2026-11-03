using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;
using System.Linq;

// All design objects are owned by Prototype_Generated and rebuilt by the generator.
public static class JEIInteriorDesign
{
    static Transform p;
    static Material ivory, ink, oak, teal, sky, brass, rust, leaf, ceramic, light, paper;
    static Material M(string name, Color color, float smooth=.2f) {
        string path="Assets/JEI_Prototype_Materials/Design_"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
        m.color=color; m.SetFloat("_Smoothness",smooth); EditorUtility.SetDirty(m); return m;
    }
    static GameObject Part(string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube,bool solid=false) {
        var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(p,false); g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid) Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void B(string n,float x,float y,float z,float w,float h,float d,Material m,bool solid=false) {Part(n,new Vector3(x,y,z),new Vector3(w,h,d),m,PrimitiveType.Cube,solid);}
    static void Text(string t,float x,float y,float z,float size,Color color,float yaw=0) {
        var g=new GameObject(t);g.transform.SetParent(p,false);g.transform.localPosition=new Vector3(x,y,z);g.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var tm=g.AddComponent<TextMesh>();tm.text=t;tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=color;
    }
    static void Shelf(float x,float z,float yaw=0) {
        var old=p;var g=new GameObject("Library shelving");g.transform.SetParent(p,false);g.transform.localPosition=new Vector3(x,0,z);g.transform.localRotation=Quaternion.Euler(0,yaw,0);p=g.transform;
        B("Backing",0,1,0,1.8f,2,.08f,teal,true);
        foreach(float a in new[]{-.9f,0,.9f}) B("Upright",a,1,-.17f,.06f,2,.4f,oak,true);
        for(int row=0;row<4;row++) {float y=.08f+row*.48f;B("Shelf",0,y,-.17f,1.86f,.06f,.4f,oak,true);
            for(int j=0;j<11;j++){float h=.22f+(j%3)*.05f;B("Book",-.78f+j*.145f,y+.03f+h/2,-.17f,.09f,h,.26f,j%3==0?rust:j%3==1?paper:sky);}}
        p=old;
    }
    static void Plant(float x,float z) {
        Part("Ceramic planter",new Vector3(x,.22f,z),new Vector3(.4f,.22f,.4f),ceramic,PrimitiveType.Cylinder,true);
        for(int i=0;i<9;i++) {float a=i*2.4f;var f=Part("Leaf",new Vector3(x+Mathf.Sin(a)*.17f,.75f+(i%3)*.1f,z+Mathf.Cos(a)*.17f),new Vector3(.12f,.9f,.08f),leaf,PrimitiveType.Sphere);f.transform.localRotation=Quaternion.Euler(Mathf.Cos(a)*28,0,Mathf.Sin(a)*28);}
    }
    static void Sofa(float x,float z,float yaw,Material fabric) {
        var old=p;var g=new GameObject("Upholstered sofa");g.transform.SetParent(p,false);g.transform.localPosition=new Vector3(x,0,z);g.transform.localRotation=Quaternion.Euler(0,yaw,0);p=g.transform;
        B("Base",0,.23f,0,1.85f,.22f,.78f,ink,true);
        for(int j=0;j<3;j++) {B("Seat cushion",-.58f+j*.58f,.43f,-.03f,.56f,.18f,.64f,fabric,true);B("Back cushion",-.58f+j*.58f,.76f,.32f,.56f,.65f,.18f,fabric,true);}
        foreach(float side in new[]{-1f,1f}) {B("Arm",side*.96f,.58f,0,.17f,.55f,.86f,fabric,true);foreach(float dz in new[]{-.28f,.28f}) B("Foot",side*.72f,.09f,dz,.08f,.18f,.08f,oak);}
        var pillow=Part("Accent pillow",new Vector3(-.6f,.68f,.15f),new Vector3(.34f,.34f,.12f),rust);pillow.transform.localRotation=Quaternion.Euler(10,0,12);p=old;
    }
    static void Window(float x,float z,float width) {
        B("Window recess",x,1.98f,z,width,1.45f,.05f,ink);
        B("Blue glazing",x,1.98f,z-.035f,width-.12f,1.33f,.025f,sky);
        foreach(float dx in new[]{-width/2,0,width/2}) B("Window mullion",x+dx,1.98f,z-.07f,.06f,1.5f,.09f,ivory);
        foreach(float y in new[]{1.23f,2.73f}) B("Window frame",x,y,z-.07f,width+.1f,.06f,.09f,ivory);
        B("Window sill",x,1.2f,z-.12f,width+.2f,.07f,.25f,oak);
        for(int j=0;j<4;j++) B("Blind slat",x,2.67f-j*.07f,z-.1f,width,.035f,.06f,ceramic);
    }
    static void Notebook(float x,float y,float z) {B("Notebook cover",x,y,z,.25f,.035f,.32f,rust);B("Pages",x,y+.02f,z,.23f,.014f,.29f,paper);B("Pen",x+.2f,y,z,.018f,.018f,.25f,ink);}
    public static void Build(Transform root) {
        ivory=M("Plaster",new Color(.91f,.88f,.81f));ink=M("Graphite",new Color(.065f,.09f,.11f));oak=M("Oak",new Color(.67f,.43f,.23f));teal=M("Petrol",new Color(.09f,.31f,.35f));sky=M("Glazing",new Color(.46f,.7f,.78f),.7f);brass=M("Ochre",new Color(.87f,.64f,.23f));rust=M("Terracotta",new Color(.7f,.26f,.14f));leaf=M("Leaves",new Color(.15f,.34f,.17f));ceramic=M("Ceramic",new Color(.78f,.77f,.7f));paper=M("Paper",new Color(.95f,.94f,.86f));light=M("Diffuser",Color.white);
        light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(1,.89f,.7f)*1.2f);
        // Remove only base-generator details; user's unmarked descendants were preserved first.
        string[] remove={"Sofa seat","Sofa back","Plant","Planter","Colour band","Room floor inset","Corridor floor runner","Lobby rug","Reception desk","Reception accent","Entrance canopy","Entrance sign backing"};
        foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>remove.Contains(t.name)||t.name.StartsWith("Sign ")).ToArray()) Object.DestroyImmediate(t.gameObject);
        var group=new GameObject("Interior_Design");group.transform.SetParent(root,false);p=group.transform;
        foreach(var r in root.GetComponentsInChildren<Renderer>()) {
            if(r.name=="Wall"||r.name=="Door lintel")r.sharedMaterial=ivory;
            if(r.name=="Table top"||r.name=="Meeting table")r.sharedMaterial=oak;
            if(r.name=="Chair seat"||r.name=="Chair back")r.sharedMaterial=teal;
        }
        foreach(var r in root.parent.Find("Walls").GetComponentsInChildren<Renderer>())r.sharedMaterial=ivory;
        // Fine tile grid in lobby and corridor; room parquet is individual narrow planks.
        for(int ix=0;ix<26;ix++) for(int iz=0;iz<18;iz++) B("Porcelain tile",-12.5f+ix,.013f,-8.5f+iz,.988f,.025f,.988f,(iz>=7&&iz<=10&&ix%5==0)?sky:ceramic);
        float[] centers={-10.25f,-5.15f,0,5.15f,10.25f};
        for(int room=0;room<4;room++) {
            float x=centers[room];var accent=room<2?teal:room==2?rust:teal;
            for(int row=0;row<22;row++) for(int col=0;col<5;col++) B("Oak parquet",x-2+col,.033f,2.28f+row*.295f,.985f,.018f,.28f,(row+col)%4==0?M("OakLight",new Color(.72f,.51f,.3f)):oak);
            B("Feature wall",x,1.3f,8.775f,4.95f,2.55f,.035f,accent);
            B("Skirting",x,.09f,8.73f,4.95f,.16f,.045f,ink);
            // Side-wall inset windows are decorative glazing, preserving the original outer shell.
            if(room>=2) Window(x,8.7f,3.8f); else Window(x-1.85f,8.7f,.65f);
            // Move classroom boards to corridor-side walls; students face the original back wall.
            if(room<2) {var board=root.Find("Rooms/"+(room==0?"Classroom A":"Classroom B")+"/Blackboard");board.localPosition=new Vector3(x,1.78f,8.55f);board.localScale=new Vector3(2.7f,1.05f,.09f);Window(x+1.85f,8.7f,.65f);B("Board frame",x,1.78f,8.6f,2.86f,1.18f,.06f,oak);Text("JEI  /  LEARN • DISCOVER",x,2.05f,8.49f,.019f,Color.white);B("Chalk tray",x,1.22f,8.48f,2.85f,.035f,.15f,ceramic);
                for(int row=0;row<3;row++)for(int col=0;col<2;col++)Notebook(x-1.1f+col*2.2f,.817f,3.3f+row*1.55f);
            }
            // Coloured jambs and plaque face the corridor, with a clear 1.3 m opening.
            foreach(float dx in new[]{-.7f,.7f})B("Door jamb",x+dx,1.13f,1.86f,.08f,2.26f,.12f,oak);
            B("Room plaque",x,2.62f,1.84f,2.3f,.34f,.04f,accent);Text(new[]{"101  CLASSROOM A","102  CLASSROOM B","103  OFFICE","104  MEETING"}[room],x,2.62f,1.81f,.017f,Color.white);
            for(int side=0;side<2;side++){float cx=x+(side==0?-1.66f:1.66f);B("Corridor wainscot",cx,.58f,1.88f,1.8f,1.14f,.04f,accent);B("Chair rail",cx,1.17f,1.84f,1.8f,.045f,.06f,oak);}
        }
        // Reception is relocated entirely inside the lobby, away from restroom wall.
        var reception=root.Find("Lobby and Reception");foreach(Transform t in reception) if(t.name.StartsWith("Chair")) t.localPosition+=new Vector3(5,0,0);
        B("Reception base",2,.5f,-6.9f,2.6f,1,.8f,teal,true);B("Stone countertop",2,1.03f,-6.9f,2.78f,.09f,.94f,paper,true);
        for(int i=0;i<18;i++)B("Reception oak fluting",.79f+i*.143f,.51f,-6.485f,.055f,.85f,.03f,oak);
        B("Reception nameplate",2,.65f,-6.45f,1.05f,.26f,.035f,ink);Text("RECEPTION",2,.65f,-6.42f,.016f,Color.white,180);
        B("Entry sign",1.6f,2.3f,-8.76f,4.5f,.9f,.05f,teal);Text("JEI BUILDING",1.6f,2.4f,-8.7f,.044f,Color.white,180);Text("WELCOME  /  FLOOR 01",1.6f,2.07f,-8.7f,.019f,Color.white,180);
        // Comfortable seating and a library corner occupy the previously empty east lobby.
        Sofa(7.2f,-7.8f,0,teal);Sofa(10.2f,-6.3f,90,brass);Shelf(11.6f,-8.5f,180);Plant(5.5f,-8);Plant(11.8f,-3);
        B("Lounge rug",8.2f,.045f,-6.1f,4.4f,.02f,3.2f,teal);
        B("Coffee table top",8,.47f,-6.2f,1.5f,.08f,.8f,paper,true);foreach(float dx in new[]{-.58f,.58f})foreach(float dz in new[]{-.28f,.28f})B("Coffee table leg",8+dx,.22f,-6.2f+dz,.06f,.44f,.06f,oak);Notebook(8,.53f,-6.2f);
        Sofa(-10.25f,-7.8f,0,brass);Shelf(-12.45f,-4.4f,90);Plant(-8.4f,-8);Plant(-12,-2.8f);
        Shelf(-1.2f,8.3f);Plant(2,3.2f);Plant(6.95f,8.1f);
        // Desk electronics, papers, screen and meeting stationery.
        B("Keyboard",0,.82f,4.82f,.4f,.025f,.15f,ink);Notebook(.35f,.825f,5.1f);
        B("Meeting display content",5.15f,1.7f,8.65f,1.85f,.85f,.015f,sky);Text("JEI  /  IDEAS TOGETHER",5.15f,1.7f,8.63f,.022f,Color.white);
        for(int j=-1;j<=1;j++)Notebook(5.15f+j,.84f,5.15f);
        // Restroom fixtures within the existing cubicle footprint.
        B("Toilet cistern",-3.48f,.66f,-8.2f,.48f,.65f,.25f,paper,true);
        Part("Toilet pedestal",new Vector3(-3.48f,.22f,-7.85f),new Vector3(.33f,.22f,.45f),paper,PrimitiveType.Cylinder,true);
        Part("Toilet bowl",new Vector3(-3.48f,.43f,-7.8f),new Vector3(.5f,.14f,.65f),paper,PrimitiveType.Sphere);
        B("Tap upright",-6.65f,1,-7.95f,.05f,.25f,.05f,ink);B("Tap spout",-6.65f,1.11f,-7.86f,.05f,.05f,.23f,ink);
        // Consistent lower wall colour and skirting to south rooms.
        B("Lounge feature",-10.25f,.65f,-8.76f,4.9f,1.25f,.04f,rust);B("Restroom splashback",-5.15f,.65f,-8.76f,4.9f,1.25f,.04f,sky);
        var ceiling=Part("Ceiling - visible in Play mode",new Vector3(0,3.12f,0),new Vector3(25.6f,.2f,17.6f),ivory);ceiling.AddComponent<JEICeiling>();ceiling.GetComponent<Renderer>().enabled=false;
        foreach(var text in root.GetComponentsInChildren<TextMesh>()) {
            string mp="Assets/JEI_Prototype_Materials/Design_Font.mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(!mat){mat=new Material(Shader.Find("JEI/Depth Tested Text"));mat.mainTexture=text.font.material.mainTexture;AssetDatabase.CreateAsset(mat,mp);}
            text.GetComponent<Renderer>().sharedMaterial=mat;
        }
        foreach(var l in root.GetComponentsInChildren<Light>()) {l.intensity=1.6f;l.range=8;l.shadows=LightShadows.None;}
        foreach(var r in root.GetComponentsInChildren<Renderer>()) if(r.name=="Room light fixture")r.sharedMaterial=light;
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional){l.intensity=1.1f;l.color=new Color(1,.94f,.84f);l.shadows=LightShadows.Soft;}
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.65f,.71f,.76f);RenderSettings.ambientEquatorColor=new Color(.47f,.49f,.5f);RenderSettings.ambientGroundColor=new Color(.3f,.27f,.23f);
    }
}
