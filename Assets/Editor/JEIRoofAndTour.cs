using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string RoofPreviewPath = "Documentation/RoofAndTour";
    const string RoofRuntimeKey = "JEI_RoofTour_RuntimeStage";

    [InitializeOnLoadMethod]
    static void RegisterRoofChecks() { EditorApplication.update += PollRoofChecks; }

    [MenuItem("Tools/JEI/Structure/Add Roof and Walking Perspectives")]
    public static void BuildRoofAndTour()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        // Preserve any unsaved scene edits before opening the target scene.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !scene.isDirty) continue;
            string backup = "Assets/Scenes/JEI_Recovery_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Cannot back up unsaved scene.");
            if (!string.IsNullOrEmpty(scene.path) && !EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot preserve scene edits.");
        }
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        building = GameObject.Find("JEI_Structure_Study").transform;
        var view = building.GetComponentInChildren<JEIStructureView>(true);
        if (view.floors.Length != 3) throw new InvalidOperationException("Three existing floors are required.");
        if (building.Find("Roof__Level_10.80m")) throw new InvalidOperationException("Roof already exists; existing work preserved.");
        Directory.CreateDirectory(RoofPreviewPath);
        wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/JEI_Blockout/Materials/Structure_Walls.mat");
        frame = AssetDatabase.LoadAssetAtPath<Material>("Assets/JEI_Blockout/Materials/Door_and_Window_Frame.mat");
        var slabMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/JEI_Blockout/Materials/Circulation.mat");
        if (!wall || !frame || !slabMaterial) throw new InvalidOperationException("Existing structure materials are missing.");
        var roof = Group(building, "Roof__Level_10.80m");
        roof.gameObject.AddComponent<JEIGeneratedObject>();
        roof.localPosition = new Vector3(0, 10.8f, 0);
        var slab = Group(roof, "01_RoofSlab__300mm__open_courtyard");
        // Roof underside meets third-floor wall tops at +10.50m. Only the courtyard stays open.
        Box(slab, "North roof slab", new Vector3(0, -.15f, 8.1f), new Vector3(36, .3f, 9.8f), slabMaterial);
        Box(slab, "South roof slab", new Vector3(0, -.15f, -8.1f), new Vector3(36, .3f, 9.8f), slabMaterial);
        Box(slab, "West roof slab", new Vector3(-10.95f, -.15f, 0), new Vector3(14.1f, .3f, 6.4f), slabMaterial);
        Box(slab, "East roof slab", new Vector3(10.95f, -.15f, 0), new Vector3(14.1f, .3f, 6.4f), slabMaterial);
        var parapet = Group(roof, "02_Perimeter_parapet__700mm");
        foreach (float z in new[] { -12.9f, 12.9f })
            Box(parapet, "Parapet north south", new Vector3(0, .35f, z), new Vector3(36, .7f, .2f), wall);
        foreach (float x in new[] { -17.9f, 17.9f })
            Box(parapet, "Parapet east west", new Vector3(x, .35f, 0), new Vector3(.2f, .7f, 25.6f), wall);
        var curb = Group(roof, "03_Courtyard_opening_curb");
        foreach (float z in new[] { -3.3f, 3.3f })
            Box(curb, "Courtyard curb north south", new Vector3(0, .35f, z), new Vector3(8.2f, .7f, .2f), wall);
        foreach (float x in new[] { -4f, 4f })
            Box(curb, "Courtyard curb east west", new Vector3(x, .35f, 0), new Vector3(.2f, .7f, 6.4f), wall);

        var tour = view.walkPlayer.GetComponent<JEITourController>();
        if (!tour || !tour.view) throw new InvalidOperationException("Existing walking camera is required.");
        if (!tour.avatar)
        {
            var avatar = Group(tour.transform, "Tour Avatar__simple_mannequin");
            var bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/JEI_Blockout/Materials/Collaborative.mat");
            AvatarPart(avatar, "Torso", PrimitiveType.Capsule, new Vector3(0, 1.08f, 0), new Vector3(.45f, .32f, .28f), bodyMaterial);
            AvatarPart(avatar, "Head", PrimitiveType.Sphere, new Vector3(0, 1.57f, 0), Vector3.one * .29f, wall);
            foreach (int side in new[] { -1, 1 })
            {
                AvatarPart(avatar, "Leg", PrimitiveType.Capsule, new Vector3(side * .12f, .40f, 0), new Vector3(.17f, .35f, .18f), frame);
                AvatarPart(avatar, "Arm", PrimitiveType.Capsule, new Vector3(side * .30f, 1.06f, 0), new Vector3(.12f, .28f, .12f), bodyMaterial);
            }
            tour.avatar = avatar;
        }
        tour.view.GetComponent<Camera>().nearClipPlane = .05f;
        view.roof = roof;
        view.startInFirstPerson = true;
        // The project was pointing to a removed SampleScene; build the actual tour scene.
        var buildScenes = new List<EditorBuildSettingsScene>();
        foreach (var entry in EditorBuildSettings.scenes)
            if (File.Exists(entry.path) && entry.path != ScenePath) buildScenes.Add(entry);
        buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Physics.SyncTransforms();
        VerifyRoofGeometry(roof);
        VerifyUpperFloors();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Could not save roof scene.");
        AssetDatabase.SaveAssets();
        SaveRender(view.GetComponent<Camera>(), RoofPreviewPath + "/Roof_Overview.png", 1800, 1200);
        File.WriteAllText(RoofPreviewPath + "/Geometry_Verification.txt", "PASS: roof covers all occupied third-floor rooms, corridors, stairwells and service cores.\nPASS: 7.80 x 6.40m central courtyard remains open through the roof.\nPASS: roof underside +10.50m, top +10.80m; 0.70m parapet.\nPASS: both stairs and all upper rooms remain walkable with roof present.");
        Debug.Log("JEI_ROOF_BUILD_SUCCESS");
    }

    static void AvatarPart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = material;
        // Hidden in the saved scene; the tour controller shows it only in third person.
        obj.GetComponent<Renderer>().enabled = false;
    }

    static void VerifyRoofGeometry(Transform roof)
    {
        for (float x = -17; x <= 17; x += 1)
        for (float z = -12; z <= 12; z += 1)
        {
            bool courtyard = Mathf.Abs(x) < 3.9f && Mathf.Abs(z) < 3.2f;
            bool hit = Physics.Raycast(new Vector3(x, 10, z), Vector3.up, out var contact, 1);
            if (courtyard ? hit : !hit || !contact.transform.IsChildOf(roof))
                throw new InvalidOperationException("Roof coverage mismatch at " + x + ", " + z);
        }
    }

    public static void CheckRoofAndTourRuntime()
    {
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetInt(RoofRuntimeKey, 1);
        EditorApplication.isPlaying = true;
    }

    static void PollRoofChecks()
    {
        const string request = "Library/JEI_RoofTour.request";
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(request))
        {
            string action = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (action == "runtime") CheckRoofAndTourRuntime(); else BuildRoofAndTour();
                File.WriteAllText("Library/JEI_RoofTour.result", "SUCCESS: " + action);
            }
            catch (Exception error) { File.WriteAllText("Library/JEI_RoofTour.result", "FAIL: " + error); Debug.LogException(error); }
        }
        int stage = SessionState.GetInt(RoofRuntimeKey, 0);
        if (stage == 0) return;
        if (stage >= 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(RoofRuntimeKey, 0);
            if (Application.isBatchMode) EditorApplication.Exit(stage == 2 ? 0 : 1);
            return;
        }
        if (stage != 1 || !EditorApplication.isPlaying || Time.frameCount < 8) return;
        try
        {
            VerifyRoofTourRuntime();
            SessionState.SetInt(RoofRuntimeKey, 2);
            Debug.Log("JEI_ROOF_RUNTIME_SUCCESS");
        }
        catch (Exception error)
        {
            SessionState.SetInt(RoofRuntimeKey, 3);
            File.WriteAllText(RoofPreviewPath + "/Runtime_Verification.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
        finally { EditorApplication.isPlaying = false; }
    }

    static void VerifyRoofTourRuntime()
    {
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        var tour = view.walkPlayer.GetComponent<JEITourController>();
        var cc = tour.GetComponent<CharacterController>();
        VerifyOneCamera();
        if (!view.IsWalking || tour.IsThirdPerson || !tour.isActiveAndEnabled) throw new InvalidOperationException("Default first-person mode failed.");
        var originalKeyboard = UnityEngine.InputSystem.Keyboard.current;
        var testKeyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        try
        {
            PressTourKey(testKeyboard, UnityEngine.InputSystem.Key.V, tour);
            if (!tour.IsThirdPerson) throw new InvalidOperationException("V did not enter third person.");
            PressTourKey(testKeyboard, UnityEngine.InputSystem.Key.V, tour);
            if (tour.IsThirdPerson) throw new InvalidOperationException("V did not return to first person.");
            PressTourKey(testKeyboard, UnityEngine.InputSystem.Key.Tab, view);
            if (view.IsWalking) throw new InvalidOperationException("Tab did not enter overview.");
            PressTourKey(testKeyboard, UnityEngine.InputSystem.Key.R, view);
            if (view.roof.GetComponentInChildren<Renderer>().enabled) throw new InvalidOperationException("R did not hide roof.");
            PressTourKey(testKeyboard, UnityEngine.InputSystem.Key.Tab, view);
            if (!view.IsWalking) throw new InvalidOperationException("Tab did not return to walking.");
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(testKeyboard);
            if (originalKeyboard != null) originalKeyboard.MakeCurrent();
        }
        SaveRender(tour.view.GetComponent<Camera>(), RoofPreviewPath + "/FirstPerson.png", 1500, 1000);
        Vector3 originalPosition = tour.transform.position;
        tour.SetThirdPerson(true);
        if (!tour.IsThirdPerson || tour.transform.position != originalPosition) throw new InvalidOperationException("Perspective switch moved the player.");
        VerifyOneCamera();
        PlaceProbe(cc, new Vector3(0, 7.25f, -9));
        tour.transform.rotation = Quaternion.identity;
        tour.SetThirdPerson(true);
        if (tour.CameraDistance < 2.5f || !tour.avatar.GetComponentInChildren<Renderer>().enabled) throw new InvalidOperationException("Third-person camera/avatar missing in an open room.");
        SaveRender(tour.view.GetComponent<Camera>(), RoofPreviewPath + "/ThirdPerson.png", 1500, 1000);
        // Back up to the solid third-floor partition at x=-6.5, away from any opening.
        PlaceProbe(cc, new Vector3(-6f, 7.25f, -9));
        tour.transform.rotation = Quaternion.Euler(0, 90, 0);
        tour.SetThirdPerson(true);
        if (tour.CameraDistance > .3f || tour.view.position.x <= -6.4f) throw new InvalidOperationException("Third-person camera passed through the wall.");
        tour.SetThirdPerson(false);
        if (Vector3.Distance(tour.view.position, tour.transform.position + Vector3.up * 1.65f) > .01f) throw new InvalidOperationException("First-person eye position failed.");
        if (tour.avatar.GetComponentInChildren<Renderer>().enabled) throw new InvalidOperationException("Avatar obstructs first person.");
        view.SetWalking(false);
        VerifyOneCamera();
        view.SetFloor(3);
        foreach (var r in view.roof.GetComponentsInChildren<Renderer>()) if (r.enabled) throw new InvalidOperationException("Roof obstructs floor isolation.");
        foreach (var c in view.roof.GetComponentsInChildren<Collider>()) if (!c.enabled) throw new InvalidOperationException("Roof collision disabled by isolation.");
        view.SetFloor(0);
        view.SetRoofVisible(false);
        foreach (var r in view.roof.GetComponentsInChildren<Renderer>()) if (r.enabled) throw new InvalidOperationException("Roof toggle failed.");
        view.SetWalking(true);
        VerifyOneCamera();
        foreach (var r in view.roof.GetComponentsInChildren<Renderer>()) if (!r.enabled) throw new InvalidOperationException("Walking did not restore roof.");
        // Recheck both complete stair routes under the roof in actual Play mode.
        VerifyUpperFloors();
        File.WriteAllText(RoofPreviewPath + "/Runtime_Verification.txt", "PASS: Play starts in first person with one active camera/listener.\nPASS: Input System keyboard events V, V, Tab, R, Tab trigger perspective, overview and roof controls.\nPASS: first/third person switch preserves player position; avatar shown only in third person.\nPASS: third-person camera retracts before the solid wall, avoiding clipping.\nPASS: overview and floor isolation hide the roof visually, retain collisions, and restore it when walking.\nPASS: both staircases remain traversable from floor 1 to 3 and back in actual Play mode.");
    }

    static void PressTourKey(UnityEngine.InputSystem.Keyboard keyboard, UnityEngine.InputSystem.Key key, MonoBehaviour receiver,
        string inputMethod = "Update")
    {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
        UnityEngine.InputSystem.InputSystem.Update();
        receiver.SendMessage(inputMethod, SendMessageOptions.RequireReceiver);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        UnityEngine.InputSystem.InputSystem.Update();
    }
}
