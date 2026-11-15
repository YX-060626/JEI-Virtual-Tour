using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    // Imported mesh/material data is namespaced to avoid changing the existing upper floors.
    [MenuItem("Tools/JEI/Interior/Verify Imported First Floor")]
    public static void VerifyImportedInterior()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        EditorSceneManager.OpenScene(ScenePath);
        ValidateImportedRooms();
        VerifyFitout();
        CaptureFitout();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("JEI_IMPORTED_INTERIOR_SUCCESS");
    }

    static void ValidateImportedRooms()
    {
        using var openRoomDoors = new OpenRoomDoors();
        var v=Object.FindFirstObjectByType<JEIStructureView>();
        var floor=v.floors[0];
        var report=new List<string>();
        if(floor.Find("06_Interior_Design"))throw new InvalidOperationException("Old furnishings still present.");
        foreach(string name in new[]{"06_Approved_Interiors","07_Courtyard_Commons","08_Service_Areas","09_Public_Restrooms"})
        {
            var group=floor.Find(name);if(!group)throw new InvalidOperationException("Missing imported group: "+name);
            foreach(var t in group.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script: "+t.name);
            foreach(var m in group.GetComponentsInChildren<MeshFilter>(true))if(!m.sharedMesh)throw new InvalidOperationException("Missing mesh: "+m.name);
            foreach(var r in group.GetComponentsInChildren<Renderer>(true))
                if(r.sharedMaterials.Any(m=>!m || !m.shader || m.shader.name.Contains("InternalError")))throw new InvalidOperationException("Missing material/shader: "+r.name);
            report.Add("PASS: "+name+" imported with all meshes, materials and scripts resolved.");
        }
        if(floor.Find("06_Approved_Interiors").childCount!=8)throw new InvalidOperationException("Expected eight perimeter rooms.");
        var signs=floor.Find("07_Room_Number_Plaques");
        var admin=signs.Cast<Transform>().Single(t=>t.name.StartsWith("1-007__"));
        var adminMount = ReadPlaqueMounts().Single(m => m.Number == "1-007");
        if(Vector3.Distance(admin.localPosition, adminMount.Position)>.002f)throw new InvalidOperationException("Admin plaque must be left of the revised doorway.");
        var probe=new GameObject("Temporary imported interior walk probe");var cc=probe.AddComponent<CharacterController>();
        cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.radius=.3f;cc.skinWidth=.035f;cc.stepOffset=.25f;
        var doors=floor.GetComponentsInChildren<JEIGlassOfficeDoor>(true);
        try
        {
            foreach(var door in doors)door.SetOpenImmediate(true);Physics.SyncTransforms();
            ImportedPerimeterRoutes(cc);
            ImportedServiceRoutes(cc);
            ImportedRestroomRoutes(cc);
            ImportedRoute(cc,new[]{new Vector2(0,-4.6f),new Vector2(0,-2.4f),new Vector2(1.1f,-2.0f),new Vector2(1.3f,.55f),new Vector2(1.3f,2.4f),new Vector2(1.3f,.55f),new Vector2(1.1f,-2.0f),new Vector2(0,-2.4f),new Vector2(0,-4.6f)});
            // Full circuit outside the curved benches from the user's original courtyard design.
            var ring=new List<Vector2>();for(float a=0;a<=360;a+=2.5f)ring.Add(new Vector2(-1.15f+Mathf.Cos(a*Mathf.Deg2Rad)*2.12f,.55f+Mathf.Sin(a*Mathf.Deg2Rad)*2.12f));
            ImportedRoute(cc,ring.ToArray());
        }
        finally{Object.DestroyImmediate(probe);foreach(var door in doors)door.SetOpenImmediate(false);Physics.SyncTransforms();}
        report.Add("PASS: actual CharacterController traversed all eight teaching/office rooms, service rooms, both public restrooms, courtyard entrance and bench circuit.");
        report.Add("PASS: retained 1-007 plaque mounted to the left of the imported S03 doorway at X=7.30m.");
        File.WriteAllText(FitoutDocs+"/Import_Verification.txt",string.Join("\n",report));
    }
}
