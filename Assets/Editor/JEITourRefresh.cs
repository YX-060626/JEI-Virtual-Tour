using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string TourRefreshPreviews = "Documentation/TourRefresh";

    [MenuItem("Tools/JEI/Tour/Update Tour Avatar")]
    public static void BuildAvatarRefresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            throw new InvalidOperationException("Stop Play mode before updating the avatar.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the open scene before updating the avatar.");
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        if (!view || !view.walkPlayer) throw new InvalidOperationException("The tour player is missing.");

        Directory.CreateDirectory("Library/JEI_InteriorBackups");
        Directory.CreateDirectory(TourRefreshPreviews);
        File.Copy(ScenePath, "Library/JEI_InteriorBackups/Avatar_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        try
        {
            BuildHeroineAvatar(view);
            var avatar = view.walkPlayer.GetComponent<JEITourController>().avatar;
            if (!avatar.GetComponent<JEIAvatarMotion>() || avatar.GetComponentInChildren<Collider>(true))
                throw new InvalidOperationException("The avatar must be articulated and use the player's existing capsule.");
            CaptureHeroinePreview(view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the avatar to the scene.");
            AssetDatabase.SaveAssets();
        }
        catch
        {
            if (!Application.isPlaying) EditorSceneManager.OpenScene(ScenePath);
            throw;
        }
        Debug.Log("JEI_AVATAR_REFRESH_SUCCESS");
    }

    [MenuItem("Tools/JEI/Tour/Update Room Openings and Tour Avatar")]
    public static void BuildTourRefresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            throw new InvalidOperationException("Stop Play mode before updating the building.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the open scene before updating the building.");
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        if (!view || !view.walkPlayer || view.floors.Length != 3 || view.floors.Any(floor => !floor))
            throw new InvalidOperationException("Open the three-floor tour scene first.");

        Directory.CreateDirectory("Library/JEI_InteriorBackups");
        Directory.CreateDirectory(TourRefreshPreviews);
        File.Copy(ScenePath, "Library/JEI_InteriorBackups/" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        try
        {
            InitFitoutMaterials();
            BuildRoomOpenings(view);
            if (!view.walkPlayer.GetComponent<JEIRoomInteraction>())
                view.walkPlayer.AddComponent<JEIRoomInteraction>();
            BuildHeroineAvatar(view);
            VerifyRoomOpenings(view);
            VerifyUpperFloors();
            VerifyUpperFurnishedRooms(view);
            VerifyLiftGeometry(view);
            VerifyEnglishTourText(view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the updated scene.");
            AssetDatabase.SaveAssets();
        }
        catch
        {
            if (!Application.isPlaying) EditorSceneManager.OpenScene(ScenePath);
            throw;
        }

        CaptureOpeningPreview(view);
        CaptureHeroinePreview(view);
        Debug.Log("JEI_TOUR_REFRESH_SUCCESS");
    }

    // Existing circulation checks should measure usable routes with doors open.
    sealed class OpenRoomDoors : IDisposable
    {
        readonly Dictionary<JEIRoomDoor, bool> states = new Dictionary<JEIRoomDoor, bool>();

        public OpenRoomDoors()
        {
            foreach (var door in Object.FindObjectsByType<JEIRoomDoor>(FindObjectsSortMode.None))
            {
                states[door] = door.IsOpen;
                door.SetOpenImmediate(true);
            }
            Physics.SyncTransforms();
        }

        public void Dispose()
        {
            foreach (var state in states)
                if (state.Key) state.Key.SetOpenImmediate(state.Value);
            Physics.SyncTransforms();
        }
    }

    static void VerifyRoomOpenings(JEIStructureView view)
    {
        Physics.SyncTransforms();
        var report = new List<string>();
        var probe = new GameObject("Temporary room doorway walk probe");
        var controller = probe.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = Vector3.up * .9f;
        controller.radius = .3f;
        controller.skinWidth = .035f;
        controller.stepOffset = .25f;
        try
        {
            for (int floorIndex = 0; floorIndex < view.floors.Length; floorIndex++)
            {
                var floor = view.floors[floorIndex];
                var leaves = floor.GetComponentsInChildren<JEIRoomDoor>();
                var entrances = leaves.GroupBy(door => door.transform.position).ToArray();
                if (entrances.Length < 13) throw new InvalidOperationException("Missing room doors on floor " + (floorIndex + 1));
                foreach (var entrance in entrances)
                {
                    var first = entrance.First();
                    Vector3 inward = first.transform.forward;
                    Vector3 center = entrance.Key;
                    Vector3 outside = center - inward * .81f;
                    // Prove the entire capsule clears the frame; some desks require a turn after entering.
                    Vector3 inside = center + inward * .53f;
                    try
                    {
                        foreach (var leaf in entrance)
                        {
                            Vector3 hitLine = new Vector3(leaf.InteractionPoint.x, center.y + 1f, leaf.InteractionPoint.z) - inward * .81f;
                            var hits = Physics.RaycastAll(hitLine, inward, 1.45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                            if (!hits.Any(hit => hit.collider.GetComponentInParent<JEIRoomDoor>() == leaf))
                                throw new InvalidOperationException("A closed room door does not block its opening: " + leaf.transform.parent.name);
                            AdvanceRoomDoor(leaf, true);
                        }
                        Physics.SyncTransforms();
                        PlaceProbe(controller, outside + Vector3.up * .08f);
                        WalkTo(controller, new Vector2(inside.x, inside.z));
                        AssertHeight(controller, floor.position.y);
                        WalkTo(controller, new Vector2(outside.x, outside.z));
                    }
                    finally
                    {
                        foreach (var leaf in entrance) leaf.SetOpenImmediate(false);
                        Physics.SyncTransforms();
                    }
                }
                var openings = floor.Find(RoomOpeningsRootName);
                int glazedWindows = openings.Cast<Transform>().Count(item => item.name.StartsWith("Fixed window ", StringComparison.Ordinal) &&
                    item.GetComponentInChildren<BoxCollider>());
                if (glazedWindows != (floorIndex == 0 ? 17 : 22)) throw new InvalidOperationException("Missing fixed glazing on floor " + (floorIndex + 1));
                report.Add($"PASS: floor {floorIndex + 1} has {entrances.Length} working doorways ({leaves.Length} leaves) and {glazedWindows} fixed windows. Every door swings clear and the walking capsule can enter and exit.");
            }
        }
        finally { Object.DestroyImmediate(probe); }
        File.WriteAllLines(TourRefreshPreviews + "/Room_Opening_Verification.txt", report);
    }

    static void AdvanceRoomDoor(JEIRoomDoor leaf, bool expectedOpen)
    {
        if (!leaf.TryToggle()) throw new InvalidOperationException("Cannot move door " + leaf.transform.parent.name);
        FinishRoomDoorMotion(leaf, expectedOpen);
    }

    static void FinishRoomDoorMotion(JEIRoomDoor leaf, bool expectedOpen)
    {
        if (leaf.TargetOpen != expectedOpen) throw new InvalidOperationException("Door input did not select the expected state.");
        for (int step = 0; step < 40 && leaf.IsMoving; step++) leaf.AdvanceMotion(.05f);
        Physics.SyncTransforms();
        if (leaf.IsMoving || leaf.IsOpen != expectedOpen)
            throw new InvalidOperationException("Door swing is obstructed at " + leaf.transform.parent.name + " on " + leaf.transform.position);
    }

    static void VerifyRoomDoorInteraction(JEIStructureView view)
    {
        if (!view.walkPlayer.TryGetComponent<JEIRoomInteraction>(out var input))
            throw new InvalidOperationException("The walking player has no room door input handler.");
        var tour = view.walkPlayer.GetComponent<JEITourController>();
        var capsule = tour.GetComponent<CharacterController>();
        var door = view.floors[0].GetComponentsInChildren<JEIRoomDoor>().First(leaf => leaf.transform.parent.name.StartsWith("N01 "));
        Vector3 origin = tour.transform.position;
        Quaternion rotation = tour.transform.rotation;
        Vector3 entrance = door.transform.position;
        Vector3 inward = door.transform.forward;
        Vector3 outside = entrance - inward * 1.12f;
        Vector3 inside = entrance + inward * .9f;
        var previousKeyboard = Keyboard.current;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var previousLock = Cursor.lockState;
        try
        {
            Cursor.lockState = CursorLockMode.Locked;
            PlaceProbe(capsule, outside + Vector3.up * .08f);
            tour.transform.rotation = Quaternion.LookRotation(inward);
            tour.SetThirdPerson(false);
            tour.view.LookAt(door.InteractionPoint);
            Physics.SyncTransforms();
            if (JEIRoomDoor.FindLookTarget(tour) != door) throw new InvalidOperationException("Door cannot be selected in first person.");
            PressTourKey(keyboard, Key.F, input, "ReadDoorInput");
            FinishRoomDoorMotion(door, true);
            WalkTo(capsule, new Vector2(inside.x, inside.z));
            WalkTo(capsule, new Vector2(outside.x, outside.z));
            Physics.SyncTransforms();
            tour.SetThirdPerson(true);
            tour.view.LookAt(tour.view.position + (door.InteractionPoint - tour.transform.TransformPoint(Vector3.up * 1.65f)));
            if (JEIRoomDoor.FindLookTarget(tour) != door) throw new InvalidOperationException("Open door cannot be selected in third person.");
            PressTourKey(keyboard, Key.F, input, "ReadDoorInput");
            FinishRoomDoorMotion(door, false);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            if (previousKeyboard != null) previousKeyboard.MakeCurrent();
            Cursor.lockState = previousLock;
            door.SetOpenImmediate(false);
            PlaceProbe(capsule, origin);
            tour.transform.rotation = rotation;
            tour.SetThirdPerson(false);
            Physics.SyncTransforms();
        }
    }

    static void CaptureOpeningPreview(JEIStructureView view)
    {
        var cameraObject = new GameObject("Temporary room opening preview camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camera.nearClipPlane = .04f;
        camera.farClipPlane = 100f;
        camera.fieldOfView = 65f;
        try
        {
            camera.transform.position = new Vector3(-12.35f, 1.66f, 4.04f);
            camera.transform.LookAt(new Vector3(-10.9f, 1.24f, 5.95f));
            SaveRender(camera, TourRefreshPreviews + "/Classroom_Door.png", 1400, 960);
            using var opened = new OpenRoomDoors();
            camera.transform.position = new Vector3(11.925f, 7.2f + 1.70f, -6.64f);
            camera.transform.LookAt(new Vector3(11.925f, 7.2f + 1.26f, -12f));
            camera.fieldOfView = 86f;
            SaveRender(camera, TourRefreshPreviews + "/Computer_Lab_Windows.png", 1400, 960);
        }
        finally { Object.DestroyImmediate(cameraObject); }
    }

    static void CaptureHeroinePreview(JEIStructureView view)
    {
        var tour = view.walkPlayer.GetComponent<JEITourController>();
        var studio = new GameObject("Temporary heroine preview studio");
        studio.transform.position = new Vector3(31f, 0, -15f);
        try
        {
            var model = Object.Instantiate(tour.avatar.gameObject, studio.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.SetActive(true);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.enabled = true;

            var stage = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.76f, .79f, .79f) };
            stage.hideFlags = HideFlags.HideAndDontSave;
            var floor = Box(studio.transform, "Portrait studio floor", new Vector3(0, -.035f, 0), new Vector3(12f, .07f, 12f), stage, false);

            var fill = Group(studio.transform, "Warm portrait fill").gameObject.AddComponent<Light>();
            fill.transform.localPosition = new Vector3(1.15f, 2.35f, 2.1f);
            fill.type = LightType.Point;
            fill.range = 5.5f;
            fill.intensity = 4.3f;
            fill.color = new Color(1f, .92f, .82f);

            var camera = Group(studio.transform, "Character preview camera").gameObject.AddComponent<Camera>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.83f, .87f, .88f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = .03f;
            camera.farClipPlane = 9f;
            camera.transform.localPosition = new Vector3(1.03f, 1.18f, 3.07f);
            camera.transform.LookAt(studio.transform.position + Vector3.up * .90f);
            try
            {
                SaveRender(camera, TourRefreshPreviews + "/Heroine.png", 960, 1180);
                floor.SetActive(false);
                camera.fieldOfView = 32f;
                camera.transform.localPosition = new Vector3(.43f, 1.59f, 1.40f);
                camera.transform.LookAt(studio.transform.position + Vector3.up * 1.46f);
                SaveRender(camera, TourRefreshPreviews + "/Heroine_Portrait.png", 900, 1040);
            }
            finally { Object.DestroyImmediate(stage); }
        }
        finally { Object.DestroyImmediate(studio); }
    }
}
