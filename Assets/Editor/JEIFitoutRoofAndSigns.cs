using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class JEIStructureBlockout
{
    static void BuildRoomNumbers(JEIStructureView view) => RebuildRoomPlaques(view);
    static void BuildPitchedCourtyardRoof(Transform roof)
    {
        if(!roof)throw new InvalidOperationException("Flat roof missing.");
        var root=Group(roof,"04_Pitched_Roof__open_courtyard");
        var outer=new[]{new Vector3(-18.35f,.82f,-13.35f),new Vector3(18.35f,.82f,-13.35f),new Vector3(18.35f,.82f,13.35f),new Vector3(-18.35f,.82f,13.35f)};
        var ridge=new[]{new Vector3(-11.125f,3.2f,-8.275f),new Vector3(11.125f,3.2f,-8.275f),new Vector3(11.125f,3.2f,8.275f),new Vector3(-11.125f,3.2f,8.275f)};
        var inner=new[]{new Vector3(-3.9f,.82f,-3.2f),new Vector3(3.9f,.82f,-3.2f),new Vector3(3.9f,.82f,3.2f),new Vector3(-3.9f,.82f,3.2f)};
        for(int side=0;side<4;side++)
        {
            int next=(side+1)%4;
            RoofSurface(root,"Outer slope "+side,new[]{outer[side],outer[next],ridge[next],ridge[side]});
            RoofSurface(root,"Courtyard slope "+side,new[]{ridge[side],ridge[next],inner[next],inner[side]});
            Beam(root,"Ridge cap",ridge[side],ridge[next],.16f,.12f,metal);
            Beam(root,"Outer fascia",outer[side],outer[next],.12f,.26f,metal);
            Beam(root,"Courtyard fascia",inner[side],inner[next],.08f,.26f,metal);
            int seams=Mathf.CeilToInt(Vector3.Distance(outer[side],outer[next])/1.25f);
            for(int i=1;i<seams;i++)
            {
                float t=(float)i/seams;Vector3 a=Vector3.Lerp(outer[side],outer[next],t),b=Vector3.Lerp(ridge[side],ridge[next],t),c=Vector3.Lerp(inner[side],inner[next],t);
                Beam(root,"Standing seam",a+Vector3.up*.03f,b+Vector3.up*.03f,.022f,.025f,metal);
                Beam(root,"Standing seam",b+Vector3.up*.03f,c+Vector3.up*.03f,.022f,.025f,metal);
            }
        }
    }
    static void Beam(Transform root,string name,Vector3 a,Vector3 b,float width,float height,Material m)
    { var g=D(root,name,(a+b)/2,new Vector3(width,height,Vector3.Distance(a,b)),m);g.transform.localRotation=Quaternion.LookRotation(b-a); }
    static void RoofSurface(Transform root,string name,Vector3[] corners)
    {
        var mesh=new Mesh{name=name};var v=new Vector3[8];for(int i=0;i<4;i++){v[i]=corners[i];v[i+4]=corners[i]-Vector3.up*.12f;}
        var indices=new List<int>();int[] top={0,1,2,0,2,3};
        if(Vector3.Cross(v[1]-v[0],v[2]-v[0]).y<0)top=new[]{0,2,1,0,3,2};indices.AddRange(top);
        for(int i=0;i<top.Length;i+=3)indices.AddRange(new[]{top[i]+4,top[i+2]+4,top[i+1]+4});
        for(int i=0;i<4;i++){int j=(i+1)%4;indices.AddRange(new[]{i,j,j+4,i,j+4,i+4});}
        mesh.vertices=v;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,FitoutAssets+"/Roof_"+name.Replace(' ','_')+".asset");
        var g=Group(root,name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=roofSlate;g.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
}
