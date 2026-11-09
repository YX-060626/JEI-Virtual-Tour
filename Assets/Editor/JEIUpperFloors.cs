using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Shares the first-floor wall, opening, stair and material builders.
public static partial class JEIStructureBlockout
{
    const string UpperPreviewPath = "Documentation/UpperFloors";
    const string UpperRuntimeKey = "JEI_UpperFloors_RuntimeStage";

    [InitializeOnLoadMethod]
    static void RegisterUpperRuntimeCheck() { EditorApplication.update += PollUpperRuntimeCheck; }

    public static void CheckUpperFloorsRuntime()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetInt(UpperRuntimeKey, 1);
        EditorApplication.isPlaying = true;
    }

    static void PollUpperRuntimeCheck()
    {
        int stage = SessionState.GetInt(UpperRuntimeKey, 0);
        if (stage == 0) return;
        if (stage >= 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(UpperRuntimeKey, 0);
            if (Application.isBatchMode) EditorApplication.Exit(stage == 2 ? 0 : 1);
            return;
        }
        if (stage != 1 || !EditorApplication.isPlaying || Time.frameCount < 5) return;
        try
        {
            var view = Object.FindFirstObjectByType<JEIStructureView>();
            if (!view || view.floors.Length != 3) throw new InvalidOperationException("Three floor view references are missing.");
            VerifyOneCamera();
            var camera = view.GetComponent<Camera>();
            if (!camera.enabled || camera.orthographic || view.walkPlayer.activeSelf)
                throw new InvalidOperationException("Expected a perspective overview on start.");
            for (int number = 1; number <= 3; number++)
            {
                view.SetFloor(number);
                for (int index = 0; index < 3; index++)
                {
                    var slabRenderer = view.floors[index].GetComponentInChildren<MeshRenderer>();
                    if (slabRenderer.enabled != (index + 1 == number)) throw new InvalidOperationException("Floor isolation failed.");
                    foreach (var collider in view.floors[index].GetComponentsInChildren<Collider>())
                        if (!collider.enabled) throw new InvalidOperationException("Floor isolation disabled a collider.");
                }
                if (number > 1) SaveRender(camera, UpperPreviewPath + "/Floor0" + number + "_Runtime.png", 1500, 1000);
            }
            view.SendMessage("SetWalking", true, SendMessageOptions.RequireReceiver);
            VerifyOneCamera();
            foreach (var floor in view.floors)
                if (!floor.GetComponentInChildren<MeshRenderer>().enabled) throw new InvalidOperationException("Walking did not restore every floor.");
            if (camera.enabled || !view.walkPlayer.activeSelf) throw new InvalidOperationException("Walking did not activate.");
            view.SendMessage("SetWalking", false, SendMessageOptions.RequireReceiver);
            VerifyOneCamera();
            if (!camera.enabled || view.walkPlayer.activeSelf) throw new InvalidOperationException("Could not return to overview.");
            File.WriteAllText(UpperPreviewPath + "/Unity_Runtime_Verification.txt",
                "PASS: actual Play mode starts with the complete three-floor perspective model.\nPASS: floors 1, 2 and 3 isolate correctly without disabling collisions.\nPASS: walking restores all floors; overview -> walking -> overview keeps exactly one active Camera and AudioListener.\nPASS: floor 2 and 3 previews rendered from the runtime camera.");
            SessionState.SetInt(UpperRuntimeKey, 2);
            Debug.Log("JEI_UPPER_RUNTIME_SUCCESS");
        }
        catch (Exception error)
        {
            SessionState.SetInt(UpperRuntimeKey, 3);
            File.WriteAllText(UpperPreviewPath + "/Unity_Runtime_Verification.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
        finally { EditorApplication.isPlaying = false; }
    }

    [MenuItem("Tools/JEI/Structure/Add Second and Third Floors")]
    public static void BuildUpperFloors()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before building floors.");
        // Never discard unsaved work when this is invoked from another scene.
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        building = GameObject.Find("JEI_Structure_Study").transform;
        var first = building.Find("Floor_01__36x26m__Storey_3.60m");
        if (!first) throw new InvalidOperationException("The original structural first floor is required.");
        if (building.Find("Floor_02__36x26m__Level_3.60m") || building.Find("Floor_03__36x26m__Level_7.20m"))
            throw new InvalidOperationException("Upper floors already exist. Existing edits have been preserved.");
        InitMaterials();
        var levels = new[] { first, BuildUpperFloor(2), BuildUpperFloor(3) };
        var view = building.GetComponentInChildren<JEIStructureView>(true);
        view.floors = levels;
        view.target.localPosition = new Vector3(0, 4.3f, 0);
        view.transform.position = view.target.position + Quaternion.Euler(38, -35, 0) * Vector3.back * 57;
        view.transform.LookAt(view.target);
        Physics.SyncTransforms();
        Directory.CreateDirectory(UpperPreviewPath);
        // Validation happens before saving the asset, so a failed build does not overwrite it.
        VerifyUpperFloors();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath))
            throw new IOException("Could not save the three-floor structure.");
        AssetDatabase.SaveAssets();
        CaptureUpperFloors();
        Selection.activeGameObject = levels[2].gameObject;
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0, 4.3f, 0), Quaternion.Euler(38, -35, 0), 35, false, true);
        Debug.Log("JEI_UPPER_FLOORS_SUCCESS: saved, verified and rendered floors 2 and 3.");
    }

    static Transform BuildUpperFloor(int number)
    {
        plan = JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/JEI_Blockout/Floor0" + number + "_Plan.json"));
        ValidatePlan();
        var level = Group(building, number == 2 ? "Floor_02__36x26m__Level_3.60m" : "Floor_03__36x26m__Level_7.20m");
        // Build at the origin so the shared builders' world-space rotations stay identical.
        BuildUpperSlab(Group(level, "00_Slab__atrium_stair_and_lift_openings"));
        var rooms = Group(level, "01_Rooms__numbered");
        foreach (var room in plan.rooms)
        {
            if (room.kind == "courtyard" || room.kind == "stair" || room.name == "LIFT RESERVE")
            {
                Group(rooms, room.id + "__" + room.name + "__VOID");
                continue;
            }
            BuildRoom(rooms, room);
        }
        var walls = Group(level, "02_Walls__0.30m_external__0.20m_internal");
        foreach (var definition in plan.walls) BuildWall(walls, definition);
        Group(level, "03_Circulation__2.40m_clear");
        var stairs = Group(level, number == 2 ? "04_Stairs__to_floor_03" : "04_Stairs__arrival_only");
        if (number == 2)
        {
            BuildStair(stairs, -1, true);
            BuildStair(stairs, 1, true);
        }
        foreach (int side in new[] { -1, 1 })
        {
            // Guard the shaft edge alongside the full-height circulation strip.
            // The south landing remains open to both incoming and outgoing flights.
            var guard = Group(stairs, side < 0 ? "West opening guards" : "East opening guards");
            Box(guard, "Stair opening edge guard", new Vector3(side * 14.25f, .55f, .775f),
                new Vector3(.10f, 1.1f, 4.85f), frame);
            if (number == 3)
            {
                // No fourth-floor flight. Protect the unused half of the south opening.
                Box(guard, "Terminal landing guard", new Vector3(side * 16.8f, .55f, -1.70f),
                    new Vector3(1.5f, 1.1f, .10f), frame);
                Box(guard, "Terminal stair well end guard", new Vector3(side * 15.9f, .55f, 3.15f),
                    new Vector3(3.4f, 1.1f, .10f), frame);
            }
        }
        foreach (var text in level.GetComponentsInChildren<TextMesh>(true)) text.gameObject.SetActive(false);
        level.localPosition = new Vector3(0, (number - 1) * plan.floorToFloor, 0);
        return level;
    }

    static Rect[] UpperVoids()
    {
        // Rect's y coordinate represents the plan's Z coordinate. Slab edges meet wall faces.
        return new[] {
            Rect.MinMaxRect(-3.9f, -3.2f, 3.9f, 3.2f),
            Rect.MinMaxRect(9.5f, .1f, 12.3f, 3.2f),
            Rect.MinMaxRect(-17.6f, -1.65f, -14.2f, 3.2f),
            Rect.MinMaxRect(14.2f, -1.65f, 17.6f, 3.2f)
        };
    }

    static void BuildUpperSlab(Transform parent)
    {
        var patches = new List<Rect> { Rect.MinMaxRect(-18, -13, 18, 13) };
        foreach (var hole in UpperVoids())
        {
            var remaining = new List<Rect>();
            foreach (var p in patches)
            {
                float x0 = Mathf.Max(p.xMin, hole.xMin), x1 = Mathf.Min(p.xMax, hole.xMax);
                float z0 = Mathf.Max(p.yMin, hole.yMin), z1 = Mathf.Min(p.yMax, hole.yMax);
                if (x1 <= x0 || z1 <= z0) { remaining.Add(p); continue; }
                AddPatch(remaining, p.xMin, p.yMin, x0, p.yMax);
                AddPatch(remaining, x1, p.yMin, p.xMax, p.yMax);
                AddPatch(remaining, x0, p.yMin, x1, z0);
                AddPatch(remaining, x0, z1, x1, p.yMax);
            }
            patches = remaining;
        }
        int i = 0;
        foreach (var p in patches)
            Box(parent, "Slab panel " + (++i), new Vector3(p.center.x, -.15f, p.center.y),
                new Vector3(p.width, .3f, p.height), corridor);
    }

    static void AddPatch(List<Rect> patches, float x0, float z0, float x1, float z1)
    {
        if (x1 - x0 > .001f && z1 - z0 > .001f) patches.Add(Rect.MinMaxRect(x0, z0, x1, z1));
    }

    [MenuItem("Tools/JEI/Structure/Verify Three Floor Structure")]
    public static void VerifyUpperFloors()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open the structure scene first.");
        var report = new List<string>();
        Physics.SyncTransforms();
        for (int number = 2; number <= 3; number++)
        {
            var upperPlan = JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/JEI_Blockout/Floor0" + number + "_Plan.json"));
            float y = (number - 1) * upperPlan.floorToFloor;
            int doors = 0;
            foreach (var w in upperPlan.walls) foreach (var o in w.openings)
            {
                if (o.kind != "door") continue;
                var center = w.axis == "H" ? new Vector3(o.at, y + 1, w.at) : new Vector3(w.at, y + 1, o.at);
                var half = w.axis == "H" ? new Vector3(o.width / 2 - .08f, .85f, .08f) : new Vector3(.08f, .85f, o.width / 2 - .08f);
                AssertClear(center, half, "F" + number + " door");
                doors++;
            }
            foreach (float z in new[] { -4.6f, 4.6f }) AssertClear(new Vector3(0, y + 1, z), new Vector3(17.6f, .85f, 1.16f), "Corridor");
            foreach (float x in new[] { -5.3f, 5.3f }) AssertClear(new Vector3(x, y + 1, 0), new Vector3(1.16f, .85f, 4.5f), "Cross-link");
            foreach (var hole in UpperVoids())
            {
                // Test only the new slab plane: stair treads below are expected.
                // North end of the stair hole stays clear of the incoming south landing.
                var center = new Vector3(hole.center.x, y - .25f, hole.center.y);
                foreach (var hit in Physics.OverlapBox(center, new Vector3(hole.width / 2 - .12f, .02f, hole.height / 2 - .20f)))
                    if (hit.name.StartsWith("Slab panel") || hit.name.StartsWith("Zone floor"))
                        throw new InvalidOperationException("Slab covers the floor opening at " + center);
            }
            report.Add("PASS: Floor " + number + ": " + doors + " clear doorways; 2.40m corridor loop; courtyard, lift and both stair openings clear.");
            foreach (var r in upperPlan.rooms)
                if (r.id.Contains("-N") || r.id.Contains("-S")) report.Add(r.id + " " + r.name + ": " + Area(r).ToString("F2") + " m2 clear.");
        }
        var obj = new GameObject("Temporary multistorey walking probe");
        var cc = obj.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.center = new Vector3(0, .9f, 0); cc.radius = .3f; cc.stepOffset = .25f; cc.skinWidth = .035f;
        try
        {
            foreach (int side in new[] { -1, 1 })
            {
                float outer = side * 16.8f, inner = side * 15f, passage = side * 13.3f;
                PlaceProbe(cc, new Vector3(passage, .05f, -4.6f));
                for (int storey = 0; storey < 2; storey++)
                {
                    WalkTo(cc, new Vector2(passage, -2.4f));
                    WalkTo(cc, new Vector2(outer, -2.4f));
                    WalkTo(cc, new Vector2(outer, 2.4f));
                    AssertHeight(cc, storey * 3.6f + 1.8f);
                    WalkTo(cc, new Vector2(inner, 2.4f));
                    WalkTo(cc, new Vector2(inner, -2.4f));
                    AssertHeight(cc, (storey + 1) * 3.6f);
                    WalkTo(cc, new Vector2(passage, -2.4f));
                    WalkTo(cc, new Vector2(passage, -4.6f));
                }
                for (int storey = 1; storey >= 0; storey--)
                {
                    WalkTo(cc, new Vector2(passage, -2.4f));
                    WalkTo(cc, new Vector2(inner, -2.4f));
                    WalkTo(cc, new Vector2(inner, 2.4f));
                    WalkTo(cc, new Vector2(outer, 2.4f));
                    WalkTo(cc, new Vector2(outer, -2.4f));
                    WalkTo(cc, new Vector2(passage, -2.4f));
                    AssertHeight(cc, storey * 3.6f);
                    WalkTo(cc, new Vector2(passage, -4.6f));
                }
                report.Add("PASS: " + (side < 0 ? "west" : "east") + " stair climbed continuously 0 -> 3.60 -> 7.20m and descended back to 0m with 1.80m CharacterController; landings connect to corridors.");
            }
            for (int number = 2; number <= 3; number++)
            {
                float y = (number - 1) * 3.6f;
                PlaceProbe(cc, new Vector3(-13.3f, y + .05f, -4.6f));
                foreach (var p in new[] { new Vector2(-5.3f,-4.6f), new Vector2(-5.3f,4.6f), new Vector2(5.3f,4.6f), new Vector2(5.3f,-4.6f), new Vector2(13.3f,-4.6f) }) WalkTo(cc, p);
                AssertHeight(cc, y);
                var p2 = JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/JEI_Blockout/Floor0" + number + "_Plan.json"));
                foreach (var r in p2.rooms)
                {
                    if (!r.id.Contains("-N") && !r.id.Contains("-S")) continue;
                    float x = (r.x0 + r.x1) / 2, z = (r.z0 + r.z1) / 2;
                    PlaceProbe(cc, new Vector3(x, y + .05f, z > 0 ? 4.6f : -4.6f));
                    WalkTo(cc, new Vector2(x, z));
                    AssertHeight(cc, y);
                }
                report.Add("PASS: Floor " + number + " corridor loop and all eight perimeter rooms traversed physically.");
            }
        }
        finally { Object.DestroyImmediate(obj); }
        Directory.CreateDirectory(UpperPreviewPath);
        File.WriteAllText(UpperPreviewPath + "/Unity_Verification.txt", string.Join("\n", report));
        Debug.Log(string.Join("\n", report));
    }

    static void AssertClear(Vector3 center, Vector3 half, string context)
    {
        foreach (var hit in Physics.OverlapBox(center, half))
            if (!(hit is CharacterController)) throw new InvalidOperationException(context + " blocked at " + center + " by " + hit.name);
    }

    static void AssertHeight(CharacterController cc, float y)
    {
        if (Mathf.Abs(cc.transform.position.y - y) > .12f)
            throw new InvalidOperationException("Expected walking height " + y + ", got " + cc.transform.position);
    }

    [MenuItem("Tools/JEI/Structure/Capture Upper Floors")]
    public static void CaptureUpperFloors()
    {
        var root = GameObject.Find("JEI_Structure_Study").transform;
        var levels = new[] { root.Find("Floor_01__36x26m__Storey_3.60m"), root.Find("Floor_02__36x26m__Level_3.60m"), root.Find("Floor_03__36x26m__Level_7.20m") };
        var renderers = new Dictionary<Renderer, bool>();
        foreach (var floor in levels) foreach (var r in floor.GetComponentsInChildren<Renderer>(true)) renderers[r] = r.enabled;
        var obj = new GameObject("Temporary upper floor preview camera");
        var camera = obj.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.91f, .94f, .95f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 200;
        Directory.CreateDirectory(UpperPreviewPath);
        try
        {
            camera.fieldOfView = 46;
            camera.transform.position = new Vector3(38, 36, -44);
            camera.transform.LookAt(new Vector3(0, 4.2f, 0));
            SaveRender(camera, UpperPreviewPath + "/All_Floors.png", 1800, 1300);
            for (int index = 1; index <= 2; index++)
            {
                for (int j = 0; j < levels.Length; j++) foreach (var r in levels[j].GetComponentsInChildren<Renderer>(true)) r.enabled = j == index && renderers[r];
                float y = index * 3.6f;
                camera.orthographic = false;
                camera.transform.position = new Vector3(26, y + 34, -34);
                camera.transform.LookAt(new Vector3(0, y + .7f, 0));
                SaveRender(camera, UpperPreviewPath + "/Floor0" + (index + 1) + "_Overview.png", 1800, 1300);
                camera.orthographic = true; camera.orthographicSize = 15;
                camera.transform.position = new Vector3(0, y + 60, 0);
                camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                SaveRender(camera, UpperPreviewPath + "/Floor0" + (index + 1) + "_Top.png", 1800, 1300);
            }
        }
        finally
        {
            foreach (var pair in renderers) pair.Key.enabled = pair.Value;
            Object.DestroyImmediate(obj);
        }
    }
}
