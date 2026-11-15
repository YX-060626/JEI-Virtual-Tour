using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    static void Desk(float x,float z,float yaw=0,bool computer=false,float width=1.25f,Material top=null)
    {
        var g=Item(computer?"Computer workstation":"Desk",x,z,yaw);
        D(g,"Worktop",new Vector3(0,.76f,0),new Vector3(width,.075f,.66f),top?top:oak);
        foreach(float dx in new[]{-width/2+.09f,width/2-.09f})foreach(float dz in new[]{-.24f,.24f})D(g,"Steel desk leg",new Vector3(dx,.36f,dz),new Vector3(.045f,.72f,.045f),metal);
        Solid(g,new Vector3(0,.4f,0),new Vector3(width,.8f,.66f));
        if(computer)
        {
            D(g,"Monitor back",new Vector3(0,1.14f,.19f),new Vector3(.55f,.34f,.045f),metal);
            D(g,"Monitor display",new Vector3(0,1.14f,.162f),new Vector3(.50f,.29f,.009f),screen);
            D(g,"Monitor stand",new Vector3(0,.90f,.19f),new Vector3(.045f,.20f,.045f),metal);
            D(g,"Monitor base",new Vector3(0,.81f,.15f),new Vector3(.24f,.025f,.17f),metal);
            D(g,"Keyboard",new Vector3(0,.815f,-.15f),new Vector3(.40f,.028f,.13f),metal);
            for(int k=0;k<3;k++)D(g,"Screen text",new Vector3(-.05f,1.20f-k*.045f,.154f),new Vector3(.30f-k*.05f,.009f,.003f),white);
        }
        else
        {
            D(g,"Notebook cover",new Vector3(-.24f,.814f,0),new Vector3(.24f,.024f,.30f),teal);
            D(g,"Notebook pages",new Vector3(-.24f,.829f,0),new Vector3(.22f,.009f,.28f),paper);
            D(g,"Pen",new Vector3(.13f,.815f,-.02f),new Vector3(.014f,.014f,.20f),metal);
        }
    }
    static void Chair(float x,float z,float yaw=0,Material fabric=null)
    {
        var g=Item("Chair",x,z,yaw);fabric=fabric?fabric:blue;
        D(g,"Seat",new Vector3(0,.46f,0),new Vector3(.46f,.07f,.46f),fabric);
        D(g,"Back",new Vector3(0,.72f,-.21f),new Vector3(.44f,.46f,.055f),fabric);
        foreach(float dx in new[]{-.18f,.18f})foreach(float dz in new[]{-.17f,.17f})D(g,"Chair leg",new Vector3(dx,.22f,dz),new Vector3(.035f,.44f,.035f),metal);
        Solid(g,new Vector3(0,.47f,0),new Vector3(.48f,.94f,.48f));
    }
    static void Sofa(float x,float z,float yaw=0,Material fabric=null,float width=1.8f)
    {
        var g=Item("Lounge sofa",x,z,yaw);fabric=fabric?fabric:purple;
        D(g,"Sofa plinth",new Vector3(0,.21f,0),new Vector3(width,.26f,.73f),metal);
        for(int i=0;i<2;i++)D(g,"Seat cushion",new Vector3((i-.5f)*(width-.2f)/2,.43f,-.02f),new Vector3((width-.25f)/2,.18f,.63f),fabric);
        D(g,"Back cushion",new Vector3(0,.73f,.29f),new Vector3(width,.65f,.18f),fabric);
        foreach(float side in new[]{-1f,1f})D(g,"Sofa arm",new Vector3(side*(width/2-.06f),.56f,0),new Vector3(.13f,.48f,.78f),fabric);
        D(g,"Accent pillow",new Vector3(-width*.28f,.64f,.15f),new Vector3(.31f,.31f,.12f),yellow);
        Solid(g,new Vector3(0,.5f,0),new Vector3(width,1,.8f));
    }
    static void CoffeeTable(float x,float z)
    {
        var g=Item("Coffee table",x,z);D(g,"Top",new Vector3(0,.43f,0),new Vector3(1.1f,.075f,.6f),oak);D(g,"Pedestal",new Vector3(0,.20f,0),new Vector3(.65f,.40f,.38f),white);
        D(g,"Magazine",new Vector3(.1f,.48f,0),new Vector3(.28f,.02f,.35f),paper);Solid(g,new Vector3(0,.25f,0),new Vector3(1.1f,.5f,.6f));
    }
    static void Shelf(float x,float z,float yaw=0,float width=1.5f,bool books=true)
    {
        var g=Item(books?"Bookcase":"Storage cabinet",x,z,yaw);
        D(g,"Cabinet back",new Vector3(0,1,.18f),new Vector3(width,2,.06f),white);
        foreach(float dx in new[]{-width/2,width/2})D(g,"Cabinet side",new Vector3(dx,1,0),new Vector3(.06f,2,.44f),oak);
        for(int row=0;row<5;row++)
        {
            float y=.08f+row*.46f;D(g,"Shelf",new Vector3(0,y,0),new Vector3(width,.04f,.44f),oak);
            if(row==4)continue;
            int count=books?Mathf.Max(2,Mathf.FloorToInt((width-.2f)/.16f)):Mathf.Max(1,Mathf.FloorToInt((width-.1f)/.4f));
            for(int j=0;j<count;j++)
            {
                float px=books?-width/2+.14f+j*.16f:(j-(count-1)/2f)*.4f;
                D(g,books?"Book":"Archive box",new Vector3(px,y+.16f,0),new Vector3(books?.09f:.31f,.28f,books?.25f:.32f),j%3==0?teal:j%3==1?yellow:paper);
            }
        }
        Solid(g,new Vector3(0,1,0),new Vector3(width+.06f,2,.46f));
    }
    static void Plant(float x,float z,float scale=1)
    {
        var g=Item("Potted plant",x,z);g.localScale=Vector3.one*scale;
        Shape(g,"Ceramic pot",PrimitiveType.Cylinder,new Vector3(0,.20f,0),new Vector3(.38f,.20f,.38f),white);
        Shape(g,"Soil",PrimitiveType.Cylinder,new Vector3(0,.402f,0),new Vector3(.33f,.012f,.33f),oak);
        for(int i=0;i<7;i++)
        {
            float a=i*2.4f;var l=Shape(g,"Leaf",PrimitiveType.Sphere,new Vector3(Mathf.Sin(a)*.12f,.80f+(i%3)*.09f,Mathf.Cos(a)*.12f),new Vector3(.12f,.85f,.07f),leaf);
            l.transform.localRotation=Quaternion.Euler(Mathf.Cos(a)*25,0,Mathf.Sin(a)*25);
        }
        Solid(g,new Vector3(0,.30f,0),new Vector3(.42f,.6f,.42f));
    }
    static void Board(float x,float z,float yaw,string text,Material accent,float width=2.2f)
    {
        var g=Item("Wall teaching board",x,z,yaw);
        D(g,"Board frame",new Vector3(0,1.85f,0),new Vector3(width+.10f,1.2f,.07f),metal);
        D(g,"Writing surface",new Vector3(0,1.85f,-.044f),new Vector3(width,1.1f,.018f),white);
        D(g,"Board header",new Vector3(0,2.25f,-.057f),new Vector3(width,.24f,.01f),accent);
        Label(g,text,new Vector3(0,2.25f,-.066f),.038f,Color.white);
        for(int i=0;i<3;i++)D(g,"Board note line",new Vector3(-.12f,1.98f-i*.18f,-.06f),new Vector3(width*.64f-i*.12f,.015f,.012f),accent);
        D(g,"Marker rail",new Vector3(0,1.28f,-.07f),new Vector3(width,.045f,.12f),metal);
    }
    static void Toilet(float x,float z,float yaw)
    {
        var g=Item("Toilet",x,z,yaw);Shape(g,"Pedestal",PrimitiveType.Capsule,new Vector3(0,.22f,0),new Vector3(.32f,.23f,.40f),white);
        Shape(g,"Bowl",PrimitiveType.Sphere,new Vector3(0,.42f,-.06f),new Vector3(.45f,.18f,.60f),white);
        Shape(g,"Seat opening",PrimitiveType.Sphere,new Vector3(0,.50f,-.10f),new Vector3(.27f,.015f,.36f),metal);
        D(g,"Cistern",new Vector3(0,.65f,.23f),new Vector3(.43f,.57f,.18f),white);Solid(g,new Vector3(0,.46f,0),new Vector3(.48f,.92f,.67f));
    }
    static void Sink(float x,float z,float yaw)
    {
        var g=Item("Wash basin",x,z,yaw);D(g,"Vanity",new Vector3(0,.40f,0),new Vector3(.65f,.8f,.45f),oak);
        Shape(g,"Basin",PrimitiveType.Sphere,new Vector3(0,.85f,0),new Vector3(.70f,.15f,.48f),white);
        D(g,"Tap",new Vector3(0,1.0f,.13f),new Vector3(.035f,.24f,.035f),metal);D(g,"Tap spout",new Vector3(0,1.1f,.07f),new Vector3(.035f,.035f,.14f),metal);
        D(g,"Mirror frame",new Vector3(0,1.6f,.23f),new Vector3(.66f,.65f,.04f),metal);D(g,"Mirror",new Vector3(0,1.6f,.205f),new Vector3(.61f,.60f,.012f),tile);
        Solid(g,new Vector3(0,.5f,0),new Vector3(.72f,1,.52f));
    }
    static void CombineRoomDecoration(Transform root)
    {
        var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.GetComponent<TextMesh>()).ToArray();
        foreach(var set in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var combines=new List<CombineInstance>();
            foreach(var r in set){var f=r.GetComponent<MeshFilter>();if(f && f.sharedMesh)combines.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix});}
            if(combines.Count==0)continue;
            string roomId = root.name.Split(new[]{"__"},StringSplitOptions.None)[0];
            string path=FitoutAssets+"/"+roomId+"_"+set.Key.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var mesh=existing ? existing : new Mesh();
            mesh.Clear();
            mesh.name=root.name+"_"+set.Key.name;
            mesh.indexFormat=IndexFormat.UInt32;
            mesh.CombineMeshes(combines.ToArray(),true,true);
            mesh.RecalculateBounds();
            if(existing) EditorUtility.SetDirty(mesh);
            else AssetDatabase.CreateAsset(mesh,path);
            var g=Group(root,"Combined visuals__"+set.Key.name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=set.Key;
            foreach(var r in set){var f=r.GetComponent<MeshFilter>();Object.DestroyImmediate(r);if(f)Object.DestroyImmediate(f);}
        }
    }
}
