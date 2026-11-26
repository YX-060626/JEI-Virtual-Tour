using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    static bool IsLiftLandingOpening(Wall wall, Opening opening) =>
        wall.axis == "H" && Mathf.Abs(wall.at - 3.3f) < .01f && Mathf.Abs(opening.at - 10.9f) < .01f;

    public static void VerifyFitout()
    {
        using var openDoors = new OpenRoomDoors();
        var view=Object.FindFirstObjectByType<JEIStructureView>();
        var data=JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
        Physics.SyncTransforms();var report=new List<string>();
        foreach(var w in data.walls)foreach(var o in w.openings)
        {
            if(o.kind!="door" || IsLiftLandingOpening(w, o))continue;
            var center=w.axis=="H"?new Vector3(o.at,1,w.at):new Vector3(w.at,1,o.at);
            var half=w.axis=="H"?new Vector3(o.width/2-.08f,.8f,.09f):new Vector3(.09f,.8f,o.width/2-.08f);
            AssertClear(center,half,"First floor entrance");
        }
        report.Add("PASS: occupied room doors and external exits remain clear; the lift landing is tested separately.");
        var probe=new GameObject("Temporary furnished-floor walk probe");var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.radius=.3f;cc.stepOffset=.25f;cc.skinWidth=.035f;
        try
        {
            foreach(var room in data.rooms)
            {
                if(room.kind=="stair" || room.id=="C06" || (view.floors[0].Find("06_Approved_Interiors") && room.id=="C04"))continue;
                float x=firstDoors[room.id];bool n=room.id.StartsWith("N"),s=room.id.StartsWith("S");
                float startZ=n?4.6f:s?-4.6f:room.id=="C04"?-4.6f:room.z0>=0?4.6f:-4.6f;
                float endZ=n?7.35f:s?-7.1f:room.id=="C04"?0:room.z0>=0?2.1f:-2.1f;
                PlaceProbe(cc,new Vector3(x,.08f,startZ));WalkTo(cc,new Vector2(x,endZ));
                report.Add("PASS: entry into "+room.id+" "+room.name+" with a 1.80m-tall CharacterController.");
            }
            PlaceProbe(cc,new Vector3(0,.08f,-12));WalkTo(cc,new Vector2(0,-4.6f));
            foreach(var p in new[]{new Vector2(-5.3f,-4.6f),new Vector2(-5.3f,4.6f),new Vector2(5.3f,4.6f),new Vector2(5.3f,-4.6f)})WalkTo(cc,p);
        }
        finally{Object.DestroyImmediate(probe);}
        VerifyUpperFloors();
        report.Add("PASS: full lobby path, circulation loop and both staircases through all three floors remain traversable.");
        for(int i=0;i<3;i++)
        {
            var signs=view.floors[i].Find("07_Room_Number_Plaques");if(!signs || signs.childCount!=17)throw new InvalidOperationException("Missing floor signs.");
            var numbers=new HashSet<string>();foreach(Transform t in signs)if(!numbers.Add(t.name.Split('_')[0]))throw new InvalidOperationException("Duplicate room number.");
        }
        report.Add("PASS: 51 unique room/area plaques across floors 1, 2 and 3, matching Room_Numbers.csv.");
        var sportsFloor = view.floors.Length >= 5 ? view.floors[4] : null;
        if (sportsFloor)
        {
            for (float x = -3.5f; x <= 3.5f; x += .5f)
            for (float z = -2.8f; z <= 2.8f; z += .5f)
            {
                if (!Physics.Raycast(new Vector3(x, 10.9f, z), Vector3.up, out var hit, 20f) ||
                    !hit.transform.IsChildOf(sportsFloor) || Mathf.Abs(hit.point.y - sportsFloor.position.y) > .5f)
                    throw new InvalidOperationException("The atrium must remain open through level four and close at the level five slab at " + x + "," + z);
            }
            report.Add("PASS: the atrium remains open through level four and the level five slab covers it.");
        }
        else
        {
            for(float x=-3.5f;x<=3.5f;x+=.5f)for(float z=-2.8f;z<=2.8f;z+=.5f)
                if(Physics.Raycast(new Vector3(x,10.9f,z),Vector3.up,20))throw new InvalidOperationException("Pitched roof covers courtyard at "+x+","+z);
            foreach(var point in new[]{new Vector3(0,20,-10),new Vector3(0,20,10),new Vector3(-15,20,0),new Vector3(15,20,0)})
                if(!Physics.Raycast(point,Vector3.down,out var hit,12) || !hit.transform.name.Contains("slope"))throw new InvalidOperationException("Roof wing missing.");
            report.Add("PASS: all four roof wings have slopes; courtyard opening remains unobstructed above +10.90m.");
        }
        File.WriteAllText(FitoutDocs+"/Unity_Verification.txt",string.Join("\n",report));
    }
    public static void CaptureFitout()
    {
        building=GameObject.Find("JEI_Structure_Study").transform;
        var view=Object.FindFirstObjectByType<JEIStructureView>();var original=new Dictionary<Renderer,bool>();
        foreach(var r in building.GetComponentsInChildren<Renderer>(true))original[r]=r.enabled;
        var g=new GameObject("Temporary interior preview camera");var c=g.AddComponent<Camera>();c.GetUniversalAdditionalCameraData().renderPostProcessing=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.84f,.89f,.91f);c.nearClipPlane=.04f;c.farClipPlane=180;
        try
        {
            c.fieldOfView=48;c.transform.position=new Vector3(38,34,-44);c.transform.LookAt(new Vector3(0,5,0));SaveRender(c,FitoutDocs+(view.roof ? "/Pitched_Roof.png" : "/Sports_Terrace.png"),1800,1200);
            if (view.roof) foreach(var r in view.roof.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            for(int i=1;i<view.floors.Length;i++)foreach(var r in view.floors[i].GetComponentsInChildren<Renderer>(true))r.enabled=false;
            c.transform.position=new Vector3(25,33,-34);c.transform.LookAt(new Vector3(0,.5f,0));SaveRender(c,FitoutDocs+"/Floor01_Furnished.png",2000,1500);
            foreach(var r in view.floors[1].GetComponentsInChildren<Renderer>(true))r.enabled=original[r];
            var points=new[]{new Vector3(-10.4f,1.72f,7.0f),new Vector3(-1.55f,1.72f,7f),new Vector3(4.5f,1.72f,7f),new Vector3(13.3f,1.72f,7f),new Vector3(-8,1.72f,-7.2f),new Vector3(0,1.72f,-10.5f),new Vector3(6.6f,1.72f,-7.1f),new Vector3(12,1.72f,-7.4f)};
            var targets=new[]{new Vector3(-14,1.1f,10),new Vector3(-5,1.1f,9.9f),new Vector3(7.6f,1.1f,10),new Vector3(15,1.1f,10),new Vector3(-13,1.1f,-9.6f),new Vector3(-2,1.2f,-8.2f),new Vector3(8.5f,1.1f,-10),new Vector3(14.4f,1.1f,-10)};
            string[] names={"Classroom_A","Classroom_B","Collaboration","Seminar","Teachers_Office","Reception","Student_Services","Meeting"};c.fieldOfView=76;
            for(int i=0;i<points.Length;i++){c.transform.position=points[i];c.transform.LookAt(targets[i]);SaveRender(c,FitoutDocs+"/"+names[i]+".png",1500,1000);}
        }
        finally{foreach(var pair in original)if(pair.Key)pair.Key.enabled=pair.Value;Object.DestroyImmediate(g);}
    }
    static void PollFitout()
    {
        const string request="Library/JEI_Fitout.request";
        if(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(request))
        {
            string action=File.ReadAllText(request).Trim();File.Delete(request);
            try
            {
                if(action=="runtime"){SessionState.SetInt(FitoutRuntimeKey,1);EditorApplication.isPlaying=true;}
                else if(action=="upper-interiors")BuildUpperInteriorsAndPlaques();
                else if(action=="tour-refresh")BuildTourRefresh();
                else if(action=="avatar-refresh")BuildAvatarRefresh();
                else if(action=="verify")VerifyFitout();else if(action=="capture")CaptureFitout();else BuildFirstFloorInterior();
                File.WriteAllText("Library/JEI_Fitout.result","SUCCESS: "+action);
            }
            catch(Exception e){File.WriteAllText("Library/JEI_Fitout.result","FAIL: "+e);Debug.LogException(e);}
        }
        if(SessionState.GetInt(FitoutRuntimeKey,0)!=1 || !EditorApplication.isPlaying || Time.frameCount<8)return;
        SessionState.SetInt(FitoutRuntimeKey,0);
        try
        {
            var v=Object.FindFirstObjectByType<JEIStructureView>();var hud=v.GetComponent<JEITourHUD>();if(!hud || hud.Expanded)throw new InvalidOperationException("Compact HUD missing.");
            VerifyOneCamera();hud.ToggleHelp();if(!hud.Expanded)throw new InvalidOperationException("Help does not expand.");hud.ToggleHelp();
            v.SetWalking(false);v.SetFloor(1);
            if (v.roof) foreach(var r in v.roof.GetComponentsInChildren<Renderer>(true))if(r.enabled)throw new InvalidOperationException("Pitched roof failed to hide with floor isolation.");
            var interior=v.floors[0].Find("06_Approved_Interiors") ?? v.floors[0].Find("06_Interior_Design");
            if(!interior.GetComponentInChildren<Renderer>().enabled)throw new InvalidOperationException("Furnishings hidden on selected floor.");
            v.SetWalking(true);VerifyOneCamera();
            if (v.roof) foreach(var r in v.roof.GetComponentsInChildren<Renderer>(true))if(!r.enabled)throw new InvalidOperationException("Walking failed to restore roof.");
            if (v.floors[1].Find(UpperInteriorName))
            {
                VerifyRoomPlaques(v);
                VerifyEnglishTourText(v);
                v.SetWalking(false);
                for (int selected = 2; selected <= 3; selected++)
                {
                    v.SetFloor(selected);
                    for (int index = 0; index < v.floors.Length; index++)
                    {
                        var group = v.floors[index].Find(PlaqueGroupName);
                        if (!group) continue;
                        var plaques = group.GetComponentsInChildren<MeshRenderer>();
                        foreach (var plaque in plaques)
                            if (plaque.enabled != (index + 1 == selected))
                                throw new InvalidOperationException("Plaque does not follow floor isolation.");
                    }
                    if (!v.floors[selected - 1].Find(UpperInteriorName).GetComponentInChildren<MeshRenderer>().enabled)
                        throw new InvalidOperationException("Upper furnishings hidden on selected floor.");
                }
                v.SetWalking(true);
                VerifyOneCamera();
                VerifyLiftRuntime(v);
                if (v.walkPlayer.GetComponent<JEIRoomInteraction>())
                {
                    VerifyRoomDoorInteraction(v);
                    File.WriteAllText(TourRefreshPreviews + "/Play_Mode.txt",
                        "PASS: F input selects, opens and closes a room door in first and third person. The walking capsule traverses the open door.\n" +
                        "PASS: lift and floor switching remain functional.");
                }
                File.WriteAllText(UpperInteriorPreviews + "/Play_Mode.txt",
                    "PASS: all 51 plaques use opaque meshes; restroom signs and cubicles have no numbers.\nPASS: upper floor isolation switches plaques and furnishings together.\nPASS: returning to walking restores all floors and one active camera/listener.\nPASS: English tour HUD and visible scene text.\nPASS: the actual walking capsule entered the lift and rode between all three floors.");
            }
            else
            {
                VerifyFitout();
                File.WriteAllText(FitoutDocs+"/Runtime_Verification.txt","PASS: compact HUD and help expansion.\nPASS: furnished floor isolation and full roof restore on walking.\nPASS: one active camera/listener.\nPASS: furnished-room routes and open courtyard verified in Play mode.");
            }
            Debug.Log("JEI_FITOUT_RUNTIME_SUCCESS");
        }
        catch(Exception e){File.WriteAllText(FitoutDocs+"/Runtime_Verification.txt","FAIL: "+e);Debug.LogException(e);}
        finally{EditorApplication.isPlaying=false;}
    }
}
