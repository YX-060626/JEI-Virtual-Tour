using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string UpperInteriorName = "06_Furnished_Rooms";
    const string ImportedFurnishingMaterials = "Assets/JEI_UserInteriors/JEI_Furnishings/Materials/";
    const string UpperInteriorPreviews = "Documentation/UpperFloors/Furnished";

    [MenuItem("Tools/JEI/Interior/Furnish Upper Floors and Update Room Plaques")]
    public static void BuildUpperInteriorsAndPlaques()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before updating the scene.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the open scene before updating the interiors.");
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        if (!view || view.floors.Length != 3 || view.floors.Any(floor => !floor))
            throw new InvalidOperationException("The tour scene must contain all three floors.");

        Directory.CreateDirectory(FitoutDocs);
        Directory.CreateDirectory(UpperInteriorPreviews);
        Directory.CreateDirectory("Library/JEI_InteriorBackups");
        File.Copy(ScenePath, "Library/JEI_InteriorBackups/" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        InitFitoutMaterials();
        try
        {
            for (int level = 2; level <= 3; level++)
                BuildFurnishedUpperFloor(view.floors[0], view.floors[level - 1], level);

            BuildTourLift(view);
            RebuildRoomPlaques(view);
            UpdateAcademyWallSign(view.floors[0]);
            Physics.SyncTransforms();
            VerifyRoomPlaques(view);
            VerifyEnglishTourText(view);
            VerifyLiftGeometry(view);
            VerifyUpperFloors();
            VerifyUpperFurnishedRooms(view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the tour scene.");
            AssetDatabase.SaveAssets();
        }
        catch
        {
            EditorSceneManager.OpenScene(ScenePath);
            throw;
        }

        CaptureUpperInteriors(view);
        Debug.Log("JEI_UPPER_INTERIORS_SUCCESS: saved furnished upper floors and matching room plaques.");
    }

    static Plan UpperInteriorPlan(int level)
    {
        return JsonUtility.FromJson<Plan>(File.ReadAllText($"Assets/JEI_Blockout/Floor0{level}_Plan.json"));
    }

    static bool PerimeterRoom(Room room) => room.z0 >= 5.9f || room.z1 <= -5.9f;

    static float PerimeterDoorX(Plan layout, Room room)
    {
        float wallZ = room.z0 >= 5.9f ? room.z0 : room.z1;
        return layout.walls.Where(wall => wall.axis == "H" && Mathf.Abs(wall.at - wallZ) < .01f)
            .SelectMany(wall => wall.openings)
            .Single(door => door.kind == "door" && door.at > room.x0 && door.at < room.x1).at;
    }

    static void BuildFurnishedUpperFloor(Transform first, Transform floor, int level)
    {
        var previous = floor.Find(UpperInteriorName);
        if (previous)
        {
            if (!previous.GetComponent<JEIGeneratedObject>()) throw new InvalidOperationException("Existing interior is not generated.");
            Object.DestroyImmediate(previous.gameObject);
        }

        var root = Group(floor, UpperInteriorName);
        root.gameObject.AddComponent<JEIGeneratedObject>();
        var ivory = AssetDatabase.LoadAssetAtPath<Material>(ImportedFurnishingMaterials + "Warm_Ivory_Paint.mat");
        if (!ivory) throw new InvalidOperationException("The first-floor paint material is missing.");
        var walls = floor.Find("02_Walls__0.30m_external__0.20m_internal");
        foreach (var surface in walls.GetComponentsInChildren<MeshRenderer>())
            if (surface.name == "Wall") surface.sharedMaterial = ivory;

        var layout = UpperInteriorPlan(level);
        var zones = floor.Find("01_Rooms__numbered").Cast<Transform>().ToArray();
        foreach (var room in layout.rooms.Where(PerimeterRoom))
        {
            var zone = zones.Single(item => item.name.StartsWith(room.id + "__", StringComparison.Ordinal));
            zone.name = room.id + "__" + room.name;
            var surface = zone.GetComponentsInChildren<MeshRenderer>().Single(item => item.name.StartsWith("Zone floor__"));
            var finish = FitMat("Floor_" + room.id, Color.white);
            finish.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/JEI_UserInteriors/JEI_Furnishings/Textures/Pale_Oak_Floor.asset");
            finish.mainTextureScale = new Vector2((room.x1 - room.x0) / 2.4f, (room.z1 - room.z0) / 2.4f);
            EditorUtility.SetDirty(finish);
            surface.sharedMaterial = finish;

            var roomRoot = Group(root, room.id + "__" + room.name);
            FurnishUpperRoom(roomRoot, room, PerimeterDoorX(layout, room));
            FinishUpperWindowWall(roomRoot, room, layout);
            CombineRoomDecoration(roomRoot);
        }

        CopyUpperServiceRooms(first, root, zones, layout);
    }

    static void FinishUpperWindowWall(Transform parent, Room room, Plan layout)
    {
        bool north = room.z0 > 0;
        float wallZ = north ? room.z1 : room.z0;
        float inside = north ? -1 : 1;
        float wallFace = wallZ + inside * layout.externalWall * .5f;
        var timber = UpperFurnitureMaterial("Light_Oak", oak);
        var linen = UpperFurnitureMaterial("Warm_Ivory_Paint", white);
        D(parent, "Timber window-wall skirting", new Vector3((room.x0 + room.x1) * .5f, .085f, wallFace + inside * .014f),
            new Vector3(room.x1 - room.x0 - .30f, .13f, .035f), timber);
        var windows = layout.walls.Where(wall => wall.axis == "H" && wall.external && Mathf.Abs(wall.at - wallZ) < .01f)
            .SelectMany(wall => wall.openings)
            .Where(opening => opening.kind == "window" && opening.at - opening.width * .5f >= room.x0 &&
                opening.at + opening.width * .5f <= room.x1);
        foreach (var window in windows)
        {
            D(parent, "Pale oak window sill", new Vector3(window.at, .925f, wallFace + inside * .039f),
                new Vector3(window.width + .09f, .05f, .19f), timber);
            D(parent, "Linen blind headrail", new Vector3(window.at, 2.74f, wallFace + inside * .061f),
                new Vector3(window.width, .09f, .10f), linen);
            if (room.kind != "office") continue;
            foreach (int side in new[] { -1, 1 })
                for (int fold = 0; fold < 4; fold++)
                {
                    float x = window.at + side * (window.width * .5f - .045f - .052f * fold);
                    D(parent, "Linen curtain fold", new Vector3(x, 1.84f, wallFace + inside * (.07f + (fold % 2) * .027f)),
                        new Vector3(.07f, 1.76f, .065f), linen);
                }
        }
    }

    static void CopyUpperServiceRooms(Transform first, Transform root, Transform[] zones, Plan layout)
    {
        foreach (string sourceName in new[] { "09_Public_Restrooms", "08_Service_Areas" })
        {
            var source = first.Find(sourceName);
            if (!source) throw new InvalidOperationException("Missing first-floor fixtures: " + sourceName);
            var destination = Group(root, sourceName);
            foreach (Transform room in source)
            {
                string id = room.name.Substring(0, 3);
                if (id == "C06") continue; // The shared lift supplies its own cabin and landing doors.
                var copy = Object.Instantiate(room.gameObject, destination.transform, false);
                copy.name = room.name;
                var planRoom = layout.rooms.Single(item => item.id.EndsWith(id, StringComparison.Ordinal));
                var zone = zones.Single(item => item.name.StartsWith(planRoom.id + "__", StringComparison.Ordinal));
                zone.gameObject.SetActive(false);
                var light = Group(destination, id + " ceiling light");
                light.localPosition = new Vector3((planRoom.x0 + planRoom.x1) / 2f, 3.16f, (planRoom.z0 + planRoom.z1) / 2f);
                light.localRotation = Quaternion.Euler(90, 0, 0);
                var lamp = light.gameObject.AddComponent<Light>();
                lamp.type = LightType.Spot;
                lamp.spotAngle = 100;
                lamp.range = 4.2f;
                lamp.intensity = 4.5f;
                lamp.color = new Color(1f, .94f, .86f);
                D(light, "Ceiling diffuser", Vector3.zero, new Vector3(.5f, .5f, .04f), FitMat("Light_Diffuser", Color.white));
            }
        }
    }

    static void VerifyUpperFurnishedRooms(JEIStructureView view)
    {
        using var openDoors = new OpenRoomDoors();
        var probe = new GameObject("Temporary furnished floor walk probe");
        var controller = probe.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = new Vector3(0, .9f, 0);
        controller.radius = .3f;
        controller.skinWidth = .035f;
        controller.stepOffset = .25f;
        var report = new List<string> { "PASS: 51 wood-finish triangular signs, with unnumbered restroom signs and no stall numbers.",
            "PASS: the reception academy sign uses an opaque oak print with English lettering, and active scene labels contain no Chinese text.",
            "PASS: lift geometry protects all three shaft landings when the cabin is absent." };
        try
        {
            for (int level = 2; level <= 3; level++)
            {
                var floor = view.floors[level - 1];
                var layout = UpperInteriorPlan(level);
                var rooms = floor.Find(UpperInteriorName);
                float y = floor.position.y;
                foreach (var room in layout.rooms.Where(PerimeterRoom))
                {
                    var placed = rooms.Find(room.id + "__" + room.name);
                    if (!placed || !placed.GetComponentInChildren<Collider>() || !placed.GetComponentInChildren<MeshRenderer>())
                        throw new InvalidOperationException("Missing furniture in " + room.id);
                    float direction = room.z0 >= 5.9f ? 1 : -1;
                    bool convertedOffice = room.id == "02-S01";
                    bool refurnishedOffice = room.id == "02-S02" || room.id == "03-N04" || room.id == "03-N05";
                    bool classroomUpdate = room.id == "02-S04" || room.id == "03-N02" || room.id == "03-N03" || room.id == "03-S03";
                    float approach = refurnishedOffice || classroomUpdate ? 12f : convertedOffice ? 8.85f : 7.55f;
                    CheckRoomEntry(controller, y, PerimeterDoorX(layout, room), direction * 4.6f, direction * approach);
                    string contents = "";
                    if (classroomUpdate)
                    {
                        var stations = placed.Cast<Transform>();
                        bool computerLab = room.id == "03-S03";
                        string stationName = computerLab ? "Computer workstation" : "Student writing desk";
                        int desks = stations.Count(item => item.name == stationName);
                        int seats = stations.Count(item => item.name == "Student seat");
                        if (desks < 20 || seats != desks || !placed.Find("Teacher lectern"))
                            throw new InvalidOperationException("Missing classroom desks, seats or lectern in " + room.id);
                        contents = $" with {desks} {(computerLab ? "computer workstations" : "student desks")}";
                    }
                    report.Add("PASS: " + room.id + " " + room.name + " is furnished" + contents + " and accessible from the corridor.");
                }
                foreach (var path in new[] { new Vector2(-9.5f, 2.7f), new Vector2(-9.5f, -2.65f), new Vector2(8, 2.1f), new Vector2(8, -2.8f), new Vector2(10.9f, -2.8f) })
                    CheckRoomEntry(controller, y, path.x, Mathf.Sign(path.y) * 4.6f, path.y);
                if (Physics.Raycast(new Vector3(0, y + .1f, 0), Vector3.up, 20))
                    throw new InvalidOperationException("Furniture or roof covers the atrium.");
                report.Add("PASS: floor " + level + " restroom and service entrances are reachable, and the atrium is open to the sky.");
            }
        }
        finally { Object.DestroyImmediate(probe); }
        report.Add("PASS: both stairs and upper corridors passed the existing three-floor traversal check.");
        File.WriteAllLines(UpperInteriorPreviews + "/Verification.txt", report);
    }

    static void CheckRoomEntry(CharacterController controller, float y, float x, float startZ, float endZ)
    {
        PlaceProbe(controller, new Vector3(x, y + .08f, startZ));
        WalkTo(controller, new Vector2(x, endZ));
        AssertHeight(controller, y);
        WalkTo(controller, new Vector2(x, startZ));
    }

    static void CaptureUpperInteriors(JEIStructureView view)
    {
        var root = view.floors[0].parent;
        var renderers = root.GetComponentsInChildren<Renderer>(true).ToDictionary(item => item, item => item.enabled);
        var lights = root.GetComponentsInChildren<Light>(true).ToDictionary(item => item, item => item.enabled);
        var cameraObject = new GameObject("Temporary upper interior preview camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.88f, .91f, .93f);
        camera.nearClipPlane = .04f;
        camera.farClipPlane = 150;

        try
        {
            for (int level = 2; level <= 3; level++)
            {
                var floor = view.floors[level - 1];
                foreach (var item in renderers)
                    item.Key.enabled = item.Value && item.Key.transform.IsChildOf(floor);
                foreach (var item in lights)
                    item.Key.enabled = item.Value && (item.Key.type == LightType.Directional || item.Key.transform.IsChildOf(floor));
                float y = floor.position.y;
                PreviewUpper(camera, "Floor0" + level + "_Furnished", new Vector3(23, y + 34, -32), new Vector3(0, y + .6f, 0), 40);

                foreach (var item in renderers) item.Key.enabled = item.Value;
                foreach (var item in lights) item.Key.enabled = item.Value;
                if (level == 2)
                {
                    PreviewUpper(camera, "Floor02_Computer_Lab", new Vector3(11.3f, y + 1.75f, 6.37f), new Vector3(12.3f, y + 1.15f, 10.7f), 78);
                    PreviewUpper(camera, "Floor02_Teacher_Office", new Vector3(-14.825f, y + 1.68f, -6.5f), new Vector3(-14.825f, y + 1.22f, -10.3f), 77);
                    PreviewUpper(camera, "Floor02_Faculty_Office", new Vector3(-8.5f, y + 1.68f, -6.47f), new Vector3(-8.5f, y + 1.2f, -10.35f), 81);
                    PreviewUpper(camera, "Floor02_Lecture_Classroom_2-007", new Vector3(5.7f, y + 1.85f, -6.33f), new Vector3(5.7f, y + 1.1f, -10.1f), 85);
                }
                else
                {
                    PreviewUpper(camera, "Floor03_General_Classroom_3-002", new Vector3(-7.2f, y + 1.85f, 6.33f), new Vector3(-7.2f, y + 1.1f, 10.1f), 85);
                    PreviewUpper(camera, "Floor03_Large_Classroom_3-003", new Vector3(1.1f, y + 1.85f, 6.33f), new Vector3(1.1f, y + 1.1f, 10.1f), 88);
                    PreviewUpper(camera, "Floor03_Computer_Lab_3-008", new Vector3(11.925f, y + 1.85f, -6.33f), new Vector3(11.925f, y + 1.1f, -10.1f), 94);
                    PreviewUpper(camera, "Floor03_Program_Office", new Vector3(8.7f, y + 1.68f, 6.44f), new Vector3(8.7f, y + 1.22f, 10.3f), 81);
                    PreviewUpper(camera, "Floor03_Advisor_Office", new Vector3(14.825f, y + 1.68f, 6.44f), new Vector3(14.825f, y + 1.22f, 10.3f), 77);
                }
            }
            PreviewUpper(camera, "Room_Plaque", new Vector3(-12.7f, 1.75f, 4.05f), new Vector3(-11.33f, 1.8f, 5.75f), 54);
            PreviewUpper(camera, "Reception_View", new Vector3(0, 1.67f, -10.9f), new Vector3(0, 1.65f, -3.3f), 64);
            PreviewUpper(camera, "Academy_Wall_Sign", new Vector3(-.63f, 1.72f, -10.48f), new Vector3(-3.15f, 1.77f, -9.62f), 51);
            PreviewUpper(camera, "Restroom", new Vector3(-9.46f, 3.6f + 1.68f, 3.21f), new Vector3(-10.8f, 3.6f + 1.15f, 1.5f), 77);
            CaptureTourLift(view, UpperInteriorPreviews + "/Lift_Entrance.png");
        }
        finally
        {
            foreach (var item in renderers) item.Key.enabled = item.Value;
            foreach (var item in lights) item.Key.enabled = item.Value;
            Object.DestroyImmediate(cameraObject);
        }
    }

    static void PreviewUpper(Camera camera, string name, Vector3 position, Vector3 target, float fieldOfView)
    {
        camera.transform.position = position;
        camera.transform.LookAt(target);
        camera.fieldOfView = fieldOfView;
        SaveRender(camera, UpperInteriorPreviews + "/" + name + ".png", 1500, 1000);
    }
}
