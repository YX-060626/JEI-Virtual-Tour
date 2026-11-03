using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JEIPerformanceOptimizer
{
    const string Request="Library/JEI_Optimize.request";
    static double nextPoll;
    static JEIPerformanceOptimizer(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.timeSinceStartup<nextPoll)return; nextPoll=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(SceneManager.GetActiveScene().path!="Assets/Scenes/JEI_Floor01_Prototype.unity")return;
        File.Delete(Request);
        try {Optimize();}catch(Exception e){File.WriteAllText("Library/JEI_Optimize.result",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/JEI/Optimize Prototype Performance")]
    public static void Optimize(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/JEI_Floor01_Prototype.unity")throw new InvalidOperationException("Open the prototype scene first.");
        var root=GameObject.Find("JEI_Building")?.transform.Find("Floors/Floor_01/Prototype_Generated");
        if(!root||!root.GetComponent<JEIGeneratedObject>())throw new InvalidOperationException("Generated root not found.");
        string report=Combine(root);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Library/JEI_Optimize.result",report);Debug.Log(report);
    }
    public static string Combine(Transform root){
        int before=root.GetComponentsInChildren<MeshRenderer>(true).Length;
        int collisions=root.GetComponentsInChildren<Collider>(true).Length;
        // Merge only marked, static, collider-free leaf decorations. Keep text and the switchable roof separate.
        var candidates=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<JEIGeneratedObject>()&&f.transform.childCount==0&&f.GetComponent<Collider>()==null&&f.GetComponent<JEICeiling>()==null&&f.GetComponent<MeshRenderer>()!=null&&f.GetComponent<MeshRenderer>().enabled&&f.sharedMesh!=null&&!f.name.StartsWith("Combined_")).ToArray();
        var groups=candidates.GroupBy(f=>{
            var r=f.GetComponent<MeshRenderer>();Vector3 pos=root.InverseTransformPoint(f.transform.position);
            return r.sharedMaterial.GetInstanceID()+"_"+Mathf.FloorToInt(pos.x/5)+"_"+Mathf.FloorToInt(pos.z/5);
        });
        const string folder="Assets/JEI_GeneratedMeshes";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets","JEI_GeneratedMeshes");
        long sourceVertices=0,mergedVertices=0;int removed=0,created=0,index=0;
        foreach(var group in groups){
            var parts=group.ToArray();if(parts.Length<2)continue;
            var material=parts[0].GetComponent<MeshRenderer>().sharedMaterial;
            var mesh=new Mesh {name="Combined_"+index,indexFormat=IndexFormat.UInt32};
            var combine=parts.Select(f=>new CombineInstance {mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
            foreach(var f in parts)sourceVertices+=f.sharedMesh.vertexCount;
            mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();mergedVertices+=mesh.vertexCount;
            string path=folder+"/Decor_"+index+".asset";index++;
            var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(asset){EditorUtility.CopySerialized(mesh,asset);UnityEngine.Object.DestroyImmediate(mesh);}else{asset=mesh;AssetDatabase.CreateAsset(asset,path);}
            var g=new GameObject("Combined_"+material.name+"_"+index);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=asset;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            g.AddComponent<JEIGeneratedObject>();created++;
            foreach(var f in parts){UnityEngine.Object.DestroyImmediate(f.gameObject);removed++;}
        }
        if(sourceVertices!=mergedVertices)throw new InvalidOperationException("Merged geometry vertex count mismatch.");
        if(root.GetComponentsInChildren<Collider>(true).Length!=collisions)throw new InvalidOperationException("Collider count changed.");
        int after=root.GetComponentsInChildren<MeshRenderer>(true).Length;
        return $"JEI optimization: renderers {before} -> {after}; merged {removed} objects into {created} regional meshes; vertices preserved {sourceVertices}; colliders unchanged {collisions}.";
    }
}
