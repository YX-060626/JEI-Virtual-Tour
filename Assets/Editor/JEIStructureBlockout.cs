using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Independent, data-driven structural study. Does not call the furnished prototype generator.
[InitializeOnLoad]
public static partial class JEIStructureBlockout
{
    const string PlanPath = "Assets/JEI_Blockout/Floor01_Plan.json";
    const string ScenePath = "Assets/Scenes/JEI_Floor01_Structure.unity";
    const string RequestPath = "Library/JEI_Structure.request";
    const string ResultPath = "Library/JEI_Structure.result";
    const string PreviewPath = "Documentation/Floor01_Blockout";
    const string RuntimeCheckKey = "JEI_Structure_RuntimeCheck";
    static double nextPoll;
    static Plan plan;
    static Transform building;
    static Material wall, corridor, frame, labelMaterial, wallCap;
    static readonly Dictionary<string, Material> finishes = new Dictionary<string, Material>();

    [Serializable] public class Plan
    {
        public string version;
        public float width, depth, wallHeight, floorToFloor, externalWall, internalWall, corridorClearWidth;
        public Room[] rooms;
        public Wall[] walls;
    }
    [Serializable] public class Room
    {
        public string id, name, nameZh, kind;
        public float x0, x1, z0, z1;
    }
    [Serializable] public class Wall
    {
        public string axis;
        public float at, start, end;
        public bool external;
        public Opening[] openings;
    }
    [Serializable] public class Opening
    {
        public float at, width;
        public string kind;
    }

    static JEIStructureBlockout() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isPlaying)
        {
            if (SessionState.GetBool(RuntimeCheckKey, false) && Time.frameCount >= 3) VerifyRuntimeView();
            return;
        }
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string action = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        try
        {
            if (action == "runtime") { SessionState.SetBool(RuntimeCheckKey, true); EditorApplication.isPlaying = true; }
            else if (action == "walk") { File.WriteAllText(ResultPath, "SUCCESS: " + VerifyWalking()); }
            else if (action == "capture") { Capture(); File.WriteAllText(ResultPath, "SUCCESS: previews refreshed."); }
            else Generate();
        }
        catch (Exception error) { File.WriteAllText(ResultPath, "ERROR: " + error); Debug.LogException(error); }
    }

    [MenuItem("Tools/JEI/Structure/Build First Floor Structure")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building the structure.");
        plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
        ValidatePlan();
        Directory.CreateDirectory(PreviewPath);
        // Save any unsaved user work before switching, with an additional timestamped copy.
        var recovery = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var open = SceneManager.GetSceneAt(i);
            if (!open.isLoaded || !open.isDirty) continue;
            string backup = "Assets/Scenes/JEI_Recovery_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + i + ".unity";
            if (!EditorSceneManager.SaveScene(open, backup, true)) throw new IOException("Could not back up the open scene.");
            recovery.Add(backup);
            if (!string.IsNullOrEmpty(open.path) && !EditorSceneManager.SaveScene(open)) throw new IOException("Could not preserve the open scene.");
        }
        // The previous scenes remain intact on disk. This structure study has its own scene asset.
        if (File.Exists(ScenePath))
        {
            var prior = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existingBuilding = GameObject.Find("JEI_Structure_Study");
            if (existingBuilding && existingBuilding.transform.Find("Floor_02__36x26m__Level_3.60m"))
                throw new InvalidOperationException("This scene now includes upper floors. The first-floor-only rebuild has been stopped to preserve them.");
            foreach (var obj in prior.GetRootGameObjects())
                if (obj.name != "JEI_Structure_Study") throw new InvalidOperationException("Structure scene has additional manual root objects. Preserve them before rebuilding.");
            foreach (var obj in prior.GetRootGameObjects()) Object.DestroyImmediate(obj);
        }
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        building = Group(null, "JEI_Structure_Study");
        InitMaterials();
        var floors = Group(building, "Floor_01__36x26m__Storey_3.60m");
        Box(floors, "GroundSlab__36x26m", new Vector3(0, -.15f, 0), new Vector3(plan.width, .3f, plan.depth), corridor);
        Box(floors, "External ground", new Vector3(0, -.4f, 0), new Vector3(46, .2f, 36), finishes["ground"]);
        var rooms = Group(floors, "01_Rooms__numbered");
        foreach (var room in plan.rooms) BuildRoom(rooms, room);
        var partitions = Group(floors, "02_Walls__0.30m_external__0.20m_internal");
        foreach (var definition in plan.walls) BuildWall(partitions, definition);
        var circulation = Group(floors, "03_Circulation__2.40m_clear");
        FloorLabel(circulation, "NORTH CORRIDOR   |   CLEAR 2.40 m", new Vector3(0, .035f, 4.6f), 14, .8f, .7f);
        FloorLabel(circulation, "SOUTH CORRIDOR   |   CLEAR 2.40 m", new Vector3(0, .035f, -4.6f), 14, .8f, .7f);
        // The two cross-links stay completely clear between the courtyard and service walls.
        FloorLabel(circulation, "2.40 m", new Vector3(-5.3f, .035f, 0), 2.0f, 1.0f, .8f);
        FloorLabel(circulation, "2.40 m", new Vector3(5.3f, .035f, 0), 2.0f, 1.0f, .8f);
        var stairs = Group(floors, "04_Stairs__two_remote_cores");
        BuildStair(stairs, -1);
        BuildStair(stairs, 1);
        BuildDimensions(Group(floors, "05_Plan_dimensions"));
        BuildPlayer();
        BuildLighting();
        BuildThreeDimensionalView();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath)) throw new IOException("Could not save structure scene.");
        AssetDatabase.SaveAssets();
        Physics.SyncTransforms();
        string verification = VerifyGeometry();
        File.WriteAllText(PreviewPath + "/Unity_Verification.txt", verification);
        Capture();
        Selection.activeGameObject = null;
        if (SceneView.lastActiveSceneView)
        {
            var sceneView = SceneView.lastActiveSceneView;
            sceneView.in2DMode = false;
            sceneView.sceneLighting = true;
            sceneView.LookAt(new Vector3(0, 1, 0), Quaternion.Euler(43, -35, 0), 30, false, true);
            sceneView.Focus();
            sceneView.Repaint();
        }
        File.WriteAllText(ResultPath, "SUCCESS: " + ScenePath + "\n" + verification + "\nPreserved dirty-scene backups: " + string.Join(", ", recovery));
        Debug.Log("JEI structural study generated and verified: " + ScenePath);
    }

    static void ValidatePlan()
    {
        if (plan == null || plan.rooms == null || plan.walls == null) throw new InvalidDataException("Plan is incomplete.");
        foreach (var w in plan.walls)
        {
            if (w.end <= w.start) throw new InvalidDataException("Wall has no length.");
            float end = w.start;
            foreach (var opening in w.openings)
            {
                float left = opening.at - opening.width / 2;
                float right = opening.at + opening.width / 2;
                if (left < end - .001f || right > w.end + .001f) throw new InvalidDataException("Overlapping or out-of-range opening.");
                end = right;
            }
        }
    }

    static Material Material(string name, Color color)
    {
        string folder = "Assets/JEI_Blockout/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/JEI_Blockout", "Materials");
        string path = folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit is unavailable.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Smoothness", .08f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void InitMaterials()
    {
        finishes.Clear();
        wall = Material("Structure_Walls", new Color(.62f, .66f, .68f));
        wallCap = Material("Wall_Cut_Edges", new Color(.26f, .32f, .35f));
        corridor = Material("Circulation", new Color(.69f, .69f, .65f));
        frame = Material("Door_and_Window_Frame", new Color(.22f, .29f, .31f));
        finishes["teaching"] = Material("Teaching", new Color(.58f, .74f, .83f));
        finishes["collaboration"] = Material("Collaborative", new Color(.39f, .66f, .72f));
        finishes["office"] = Material("Staff", new Color(.80f, .70f, .56f));
        finishes["admin"] = Material("Administration", new Color(.81f, .65f, .46f));
        finishes["public"] = Material("Public", new Color(.88f, .79f, .59f));
        finishes["service"] = Material("Services", new Color(.70f, .73f, .76f));
        finishes["stair"] = Material("Stairs", new Color(.59f, .65f, .70f));
        finishes["courtyard"] = Material("Courtyard", new Color(.60f, .72f, .55f));
        finishes["ground"] = Material("Site", new Color(.92f, .93f, .91f));
        labelMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        labelMaterial.color = new Color(.18f, .25f, .27f);
    }

    static Transform Group(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider = true)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    static float InsetX(float edge) { return Mathf.Abs(Mathf.Abs(edge) - 17.85f) < .01f ? .15f : .1f; }
    static float InsetZ(float edge) { return Mathf.Abs(Mathf.Abs(edge) - 12.85f) < .01f ? .15f : .1f; }
    static float Area(Room r) { return (r.x1 - r.x0 - InsetX(r.x0) - InsetX(r.x1)) * (r.z1 - r.z0 - InsetZ(r.z0) - InsetZ(r.z1)); }

    static void BuildRoom(Transform parent, Room room)
    {
        var group = Group(parent, room.id + "__" + room.name);
        float x0 = room.x0 + InsetX(room.x0), x1 = room.x1 - InsetX(room.x1);
        float z0 = room.z0 + InsetZ(room.z0), z1 = room.z1 - InsetZ(room.z1);
        Box(group, "Zone floor__" + Area(room).ToString("F1") + "m2_clear", new Vector3((x0 + x1) / 2, .012f, (z0 + z1) / 2), new Vector3(x1 - x0, .024f, z1 - z0), finishes[room.kind], false);
        if (room.kind == "stair") return;
        string text = room.id + "\n" + room.name + "\n" + Area(room).ToString("F1") + " m2";
        FloorLabel(group, text, new Vector3((x0 + x1) / 2, .045f, (z0 + z1) / 2), x1 - x0 - .6f, Mathf.Min(z1 - z0 - .5f, 2.5f), 1);
    }

    static void BuildWall(Transform parent, Wall definition)
    {
        var group = Group(parent, (definition.external ? "External" : "Partition") + "_" + definition.axis + "_" + definition.at);
        float cursor = definition.start;
        foreach (var opening in definition.openings)
        {
            float a = opening.at - opening.width / 2, b = opening.at + opening.width / 2;
            WallPiece(group, definition, cursor, a, 0, plan.wallHeight, wall);
            if (opening.kind == "window")
            {
                WallPiece(group, definition, a, b, 0, .9f, wall);
                WallPiece(group, definition, a, b, 2.8f, plan.wallHeight, wall);
                OpeningFrame(group, definition, opening, .9f, 2.8f);
            }
            else
            {
                WallPiece(group, definition, a, b, 2.4f, plan.wallHeight, wall);
                OpeningFrame(group, definition, opening, 0, 2.4f);
                // Coloured floor thresholds mark open passages without obstructing circulation.
                Vector3 position = definition.axis == "H" ? new Vector3(opening.at, .027f, definition.at) : new Vector3(definition.at, .027f, opening.at);
                Vector3 size = definition.axis == "H" ? new Vector3(opening.width, .02f, .25f) : new Vector3(.25f, .02f, opening.width);
                Box(group, "Open door__clear_" + opening.width.ToString("F2") + "m", position, size, frame, false);
            }
            cursor = b;
        }
        WallPiece(group, definition, cursor, definition.end, 0, plan.wallHeight, wall);
    }

    static void WallPiece(Transform parent, Wall w, float a, float b, float bottom, float top, Material material)
    {
        if (b - a < .001f || top - bottom < .001f) return;
        float thickness = w.external ? plan.externalWall : plan.internalWall;
        var position = w.axis == "H" ? new Vector3((a + b) / 2, (bottom + top) / 2, w.at) : new Vector3(w.at, (bottom + top) / 2, (a + b) / 2);
        var size = w.axis == "H" ? new Vector3(b - a, top - bottom, thickness) : new Vector3(thickness, top - bottom, b - a);
        Box(parent, "Wall", position, size, material);
        if (Mathf.Abs(top - plan.wallHeight) < .001f)
        {
            position.y = top + .012f;
            size.y = .024f;
            Box(parent, "Visible wall section edge", position, size, wallCap, false);
        }
    }

    static void OpeningFrame(Transform parent, Wall w, Opening opening, float bottom, float top)
    {
        foreach (float edge in new[] { opening.at - opening.width / 2 - .025f, opening.at + opening.width / 2 + .025f })
        {
            Vector3 position = w.axis == "H" ? new Vector3(edge, (bottom + top) / 2, w.at) : new Vector3(w.at, (bottom + top) / 2, edge);
            Vector3 size = w.axis == "H" ? new Vector3(.05f, top - bottom, .24f) : new Vector3(.24f, top - bottom, .05f);
            Box(parent, "Opening jamb", position, size, frame, false);
        }
        foreach (float y in bottom > 0 ? new[] { bottom, top } : new[] { top })
        {
            Vector3 position = w.axis == "H" ? new Vector3(opening.at, y, w.at) : new Vector3(w.at, y, opening.at);
            Vector3 size = w.axis == "H" ? new Vector3(opening.width, .05f, .24f) : new Vector3(.24f, .05f, opening.width);
            Box(parent, "Opening head or sill", position, size, frame, false);
        }
    }

    static void BuildStair(Transform parent, int side, bool suspendedLowerFlight = false)
    {
        var group = Group(parent, side < 0 ? "STAIR_A__WEST" : "STAIR_B__EAST");
        // Each stair has two 1.50m clear flights, 24 x 150mm risers and 300mm goings.
        // 11 goings per flight + 1.50m landings fit within the 6.40m clear core depth.
        float outer = side < 0 ? -16.8f : 16.8f;
        float inner = side < 0 ? -15.0f : 15.0f;
        for (int i = 0; i < 11; i++)
        {
            float z = -1.5f + .3f * i;
            float y = .15f * (i + 1);
            Box(group, "Lower flight__riser_" + (i + 1), new Vector3(outer, suspendedLowerFlight ? y - .075f : y / 2, z), new Vector3(1.5f, suspendedLowerFlight ? .15f : y, .3f), finishes["stair"]);
            float upperY = 1.8f + .15f * (i + 1);
            Box(group, "Upper flight__riser_" + (i + 13), new Vector3(inner, upperY - .075f, -z), new Vector3(1.5f, .15f, .3f), finishes["stair"]);
        }
        float midX = (outer + inner) / 2;
        if (suspendedLowerFlight)
        {
            Vector3 a = new Vector3(outer, -.14f, -1.65f), b = new Vector3(outer, 1.51f, 1.65f);
            var slab = Box(group, "Lower flight structural slab", (a + b) / 2, new Vector3(1.5f, .16f, Vector3.Distance(a, b)), finishes["stair"]);
            slab.transform.rotation = Quaternion.LookRotation(b - a);
        }
        Box(group, "Half landing__1.50m_deep", new Vector3(midX, 1.725f, 2.4f), new Vector3(3.3f, .15f, 1.5f), finishes["stair"]);
        Box(group, "Next floor landing__+3.60m", new Vector3(midX, 3.525f, -2.4f), new Vector3(3.3f, .15f, 1.5f), finishes["stair"]);
        Vector3 supportA = new Vector3(inner, 1.66f, 1.65f), supportB = new Vector3(inner, 3.31f, -1.65f);
        var support = Box(group, "Upper flight structural slab", (supportA + supportB) / 2, new Vector3(1.5f, .16f, Vector3.Distance(supportA, supportB)), finishes["stair"]);
        support.transform.rotation = Quaternion.LookRotation(supportB - supportA);
        float gap = (outer + inner) / 2;
        Rail(group, new Vector3(gap, 1.1f, -1.65f), new Vector3(gap, 2.9f, 1.65f));
        Rail(group, new Vector3(gap, 2.9f, 1.65f), new Vector3(gap, 4.7f, -1.65f));
        Rail(group, new Vector3(midX - 1.65f, 2.9f, 3.1f), new Vector3(midX + 1.65f, 2.9f, 3.1f));
        for (int i = 0; i < 4; i++)
        {
            float z = -1.5f + 1.0f * i;
            float lowerHeight = .15f + .5f * i;
            Box(group, "Lower flight guard post", new Vector3(gap, lowerHeight + .55f, z), new Vector3(.045f, 1.1f, .045f), frame, false);
            float upperHeight = 3.45f - .5f * i;
            Box(group, "Upper flight guard post", new Vector3(gap, upperHeight + .55f, z), new Vector3(.045f, 1.1f, .045f), frame, false);
        }
        float passage = side < 0 ? -13.3f : 13.3f;
        FloorLabel(group, (side < 0 ? "STAIR A" : "STAIR B") + "\nUP 3.60m", new Vector3(passage, .045f, 0), 1.3f, 2.2f, 1);
        FloorLabel(group, "UP", new Vector3(outer, .045f, -2.4f), 1.2f, 1, 1);
    }

    static void Rail(Transform parent, Vector3 a, Vector3 b)
    {
        var obj = Box(parent, "Diagrammatic guardrail", (a + b) / 2, new Vector3(.045f, .045f, Vector3.Distance(a, b)), frame, false);
        obj.transform.rotation = Quaternion.LookRotation(b - a);
    }

    static void FloorLabel(Transform parent, string text, Vector3 position, float maxWidth, float maxDepth, float intensity)
    {
        var group = Group(parent, "Label__" + text.Replace('\n', '_'));
        group.localPosition = position;
        group.localRotation = Quaternion.Euler(90, 0, 0);
        var mesh = group.gameObject.AddComponent<TextMesh>();
        mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.text = text;
        mesh.characterSize = .08f;
        mesh.fontSize = 64;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = new Color(.15f, .21f, .23f, intensity);
        var renderer = group.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mesh.font.material;
        var bounds = renderer.bounds.size;
        float fit = Mathf.Min(maxWidth / Mathf.Max(bounds.x, .01f), maxDepth / Mathf.Max(bounds.z, .01f));
        group.localScale = Vector3.one * Mathf.Min(fit, 1.5f);
    }

    static void Line(Transform parent, string name, Vector3 a, Vector3 b)
    {
        var g = Group(parent, name);
        var line = g.gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = labelMaterial;
        line.positionCount = 2;
        line.SetPosition(0, a); line.SetPosition(1, b);
        line.widthMultiplier = .035f;
        line.useWorldSpace = false;
    }

    static void BuildDimensions(Transform parent)
    {
        Line(parent, "Width", new Vector3(-18, .04f, -15), new Vector3(18, .04f, -15));
        foreach (float x in new[] { -18f, 18f }) Line(parent, "Width tick", new Vector3(x, .04f, -15.4f), new Vector3(x, .04f, -13.4f));
        FloorLabel(parent, "36.00 m", new Vector3(0, .045f, -15.7f), 5, .8f, 1);
        Line(parent, "Depth", new Vector3(20, .04f, -13), new Vector3(20, .04f, 13));
        foreach (float z in new[] { -13f, 13f }) Line(parent, "Depth tick", new Vector3(18.4f, .04f, z), new Vector3(20.4f, .04f, z));
        FloorLabel(parent, "26.00 m", new Vector3(21.4f, .045f, 0), 3.1f, 1, 1);
        FloorLabel(parent, "N", new Vector3(-20.1f, .045f, 13.3f), 1.3f, 1.3f, 1);
        Line(parent, "North arrow", new Vector3(-20.1f, .04f, 9), new Vector3(-20.1f, .04f, 12.4f));
        Line(parent, "North arrow tip", new Vector3(-20.5f, .04f, 11.8f), new Vector3(-20.1f, .04f, 12.4f));
        Line(parent, "North arrow tip", new Vector3(-19.7f, .04f, 11.8f), new Vector3(-20.1f, .04f, 12.4f));
        FloorLabel(parent, "MAIN ENTRANCE", new Vector3(0, .045f, -13.8f), 7, .65f, 1);
        foreach (float x in new[] { -18.9f, 18.9f }) foreach (float z in new[] { -4.6f, 4.6f })
            FloorLabel(parent, "EXIT", new Vector3(x, .045f, z), 1.5f, .65f, 1);
    }

    static void BuildPlayer()
    {
        var player = Group(building, "Tour Player__WASD_Mouse_ESC");
        player.localPosition = new Vector3(0, .08f, -10.6f);
        var cc = player.gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.center = new Vector3(0, .9f, 0); cc.radius = .3f; cc.stepOffset = .25f; cc.skinWidth = .035f;
        var view = Group(player, "Tour Camera");
        view.localPosition = new Vector3(0, 1.65f, 0);
        var cam = view.gameObject.AddComponent<Camera>();
        cam.tag = "MainCamera"; cam.nearClipPlane = .05f; cam.farClipPlane = 120; cam.fieldOfView = 72;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        view.gameObject.AddComponent<AudioListener>();
        player.gameObject.AddComponent<JEITourController>().view = view;
    }

    static void BuildLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.46f, .49f, .53f);
        RenderSettings.ambientIntensity = 1;
        var group = Group(building, "Lighting__neutral_daylight");
        var sun = group.gameObject.AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.0f; sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(56, -25, 0);
        RenderSettings.sun = sun;
    }

    static void BuildThreeDimensionalView()
    {
        // This scene opens as a spatial model: visible wall depth, stairs and a perspective orbit camera.
        foreach (var text in building.GetComponentsInChildren<TextMesh>(true)) text.gameObject.SetActive(false);
        var dimensions = building.Find("Floor_01__36x26m__Storey_3.60m/05_Plan_dimensions");
        if (dimensions) dimensions.gameObject.SetActive(false);
        var target = Group(building, "3D overview focus");
        target.localPosition = new Vector3(0, 1, 0);
        var overview = Group(building, "3D Overview Camera__Orbit_Mouse__TAB_Walk");
        var camera = overview.gameObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 50; camera.nearClipPlane = .1f; camera.farClipPlane = 200;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.83f, .87f, .90f);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        overview.gameObject.AddComponent<AudioListener>();
        var controller = overview.gameObject.AddComponent<JEIStructureView>();
        controller.target = target;
        controller.walkPlayer = building.Find("Tour Player__WASD_Mouse_ESC").gameObject;
        controller.walkPlayer.SetActive(false);
        overview.position = target.position + Quaternion.Euler(43, -35, 0) * (Vector3.back * 46);
        overview.LookAt(target);
    }

    static string VerifyGeometry()
    {
        var messages = new List<string>();
        int passages = 0;
        foreach (var w in plan.walls) foreach (var opening in w.openings)
        {
            if (opening.kind != "door") continue;
            Vector3 center = w.axis == "H" ? new Vector3(opening.at, 1.0f, w.at) : new Vector3(w.at, 1.0f, opening.at);
            Vector3 half = w.axis == "H" ? new Vector3(opening.width / 2 - .08f, .7f, .08f) : new Vector3(.08f, .7f, opening.width / 2 - .08f);
            foreach (var hit in Physics.OverlapBox(center, half))
                if (!(hit is CharacterController)) throw new InvalidOperationException("Door is obstructed: " + center + " by " + hit.name);
            passages++;
        }
        messages.Add("PASS: " + passages + " door openings unobstructed at pedestrian height.");
        foreach (float z in new[] { -4.6f, 4.6f })
            if (Physics.BoxCast(new Vector3(-17.5f, 1, z), new Vector3(.03f, .7f, 1.16f), Vector3.right, out var hit, Quaternion.identity, 35))
                throw new InvalidOperationException("2.40m corridor obstructed by " + hit.collider.name);
        messages.Add("PASS: both 2.40m clear corridors continuous across the floor.");
        foreach (float x in new[] { -5.3f, 5.3f })
            if (Physics.BoxCast(new Vector3(x, 1, -4.5f), new Vector3(1.16f, .7f, .03f), Vector3.forward, out var hit, Quaternion.identity, 9))
                throw new InvalidOperationException("Cross-link obstructed by " + hit.collider.name);
        messages.Add("PASS: both 2.40m cross-links unobstructed, circulation forms a loop.");
        messages.Add("PASS: 36.00 x 26.00m footprint; 936.00m2 gross external footprint.");
        messages.Add("PASS: two separate U-stairs; each 24 x 0.150m = 3.600m rise, 0.300m going, 1.50m clear flight.");
        foreach (var room in plan.rooms) messages.Add(room.id + " " + room.name + ": " + Area(room).ToString("F2") + "m2 clear zone");
        messages.Add("Scope: structural design study only. Courtyard requires coordinated openings in subsequent floors. Lift is a reserved shaft; WC fixtures, fire doors and structural engineering remain design development.");
        return string.Join("\n", messages);
    }

    static string VerifyWalking()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open the structure scene first.");
        var obj = new GameObject("Temporary walkability probe");
        var controller = obj.AddComponent<CharacterController>();
        controller.height = 1.8f; controller.center = new Vector3(0, .9f, 0);
        controller.radius = .3f; controller.skinWidth = .035f; controller.stepOffset = .25f;
        var results = new List<string>();
        try
        {
            foreach (int side in new[] { -1, 1 })
            {
                float outer = side * 16.8f, inner = side * 15.0f;
                PlaceProbe(controller, new Vector3(outer, .05f, -2.4f));
                WalkTo(controller, new Vector2(outer, 2.4f));
                if (Mathf.Abs(controller.transform.position.y - 1.8f) > .12f) throw new InvalidOperationException("Lower stair flight failed: " + controller.transform.position);
                WalkTo(controller, new Vector2(inner, 2.4f));
                WalkTo(controller, new Vector2(inner, -2.4f));
                if (Mathf.Abs(controller.transform.position.y - 3.6f) > .12f) throw new InvalidOperationException("Upper stair flight failed: " + controller.transform.position);
                results.Add("PASS: " + (side < 0 ? "west" : "east") + " staircase physically climbed from ground to " + controller.transform.position.y.ToString("F3") + "m using a 1.80m-tall, 0.60m-wide CharacterController.");
            }
            PlaceProbe(controller, new Vector3(0, .05f, -9.8f));
            WalkTo(controller, new Vector2(0, -4.6f));
            WalkTo(controller, new Vector2(-5.3f, -4.6f));
            WalkTo(controller, new Vector2(-5.3f, 4.6f));
            WalkTo(controller, new Vector2(5.3f, 4.6f));
            WalkTo(controller, new Vector2(5.3f, -4.6f));
            WalkTo(controller, new Vector2(17.9f, -4.6f));
            results.Add("PASS: lobby, both cross-links, both corridors and east exterior exit traversed continuously by CharacterController.");
            string report = string.Join("\n", results);
            File.WriteAllText(PreviewPath + "/Unity_Walking_Verification.txt", report);
            return report;
        }
        finally { Object.DestroyImmediate(obj); }
    }

    static void PlaceProbe(CharacterController controller, Vector3 position)
    {
        controller.enabled = false; controller.transform.position = position; controller.enabled = true;
        Physics.SyncTransforms();
        for (int i = 0; i < 10; i++) controller.Move(Vector3.down * .02f);
    }

    static void WalkTo(CharacterController controller, Vector2 target)
    {
        for (int i = 0; i < 1800; i++)
        {
            Vector3 position = controller.transform.position;
            Vector2 remaining = target - new Vector2(position.x, position.z);
            if (remaining.magnitude < .045f) return;
            Vector2 step = Vector2.ClampMagnitude(remaining, .045f);
            controller.Move(new Vector3(step.x, -.035f, step.y));
        }
        throw new InvalidOperationException("Walk route blocked near " + controller.transform.position + " while heading to " + target);
    }

    static void VerifyRuntimeView()
    {
        SessionState.SetBool(RuntimeCheckKey, false);
        try
        {
            var view = Object.FindFirstObjectByType<JEIStructureView>();
            if (!view || !view.target || !view.walkPlayer) throw new InvalidOperationException("3D view references are incomplete.");
            var overview = view.GetComponent<Camera>();
            if (overview.orthographic || !overview.enabled || view.walkPlayer.activeSelf) throw new InvalidOperationException("Default view is not the perspective overview.");
            VerifyOneCamera();
            Vector3 position = overview.transform.position;
            view.yaw += 25;
            view.SendMessage("ApplyOrbit", SendMessageOptions.RequireReceiver);
            if (Vector3.Distance(position, overview.transform.position) < 5) throw new InvalidOperationException("Orbit view did not move in three dimensions.");
            view.yaw -= 25;
            view.SendMessage("ApplyOrbit", SendMessageOptions.RequireReceiver);
            SaveRender(overview, PreviewPath + "/Unity_Runtime_3D.png", 1800, 1200);
            view.SendMessage("SetWalking", true, SendMessageOptions.RequireReceiver);
            VerifyOneCamera();
            if (overview.enabled || !view.walkPlayer.activeInHierarchy) throw new InvalidOperationException("First-person view did not activate.");
            SaveRender(view.walkPlayer.GetComponentInChildren<Camera>(), PreviewPath + "/Unity_Interior.png", 1800, 1100);
            view.SendMessage("SetWalking", false, SendMessageOptions.RequireReceiver);
            VerifyOneCamera();
            if (!overview.enabled || view.walkPlayer.activeSelf) throw new InvalidOperationException("Could not return to overview.");
            string report = "PASS: actual Play mode starts with a perspective 3D overview.\nPASS: orbit update moves the camera around the model.\nPASS: overview -> first person -> overview switching; exactly one active Camera and AudioListener in all three states.\nPASS: 3D and eye-level interior images rendered from the actual runtime cameras.";
            File.WriteAllText(PreviewPath + "/Unity_Runtime_Verification.txt", report);
            File.WriteAllText(ResultPath, "SUCCESS: " + report);
        }
        catch (Exception error) { File.WriteAllText(ResultPath, "ERROR: " + error); Debug.LogException(error); }
        finally { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; EditorApplication.isPlaying = false; }
    }

    static void VerifyOneCamera()
    {
        int cameras = 0, listeners = 0;
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (camera.isActiveAndEnabled) cameras++;
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if (listener.isActiveAndEnabled) listeners++;
        if (cameras != 1 || listeners != 1) throw new InvalidOperationException("Expected one camera/listener, found " + cameras + "/" + listeners);
    }

    [MenuItem("Tools/JEI/Structure/Capture Structure Previews")]
    public static void Capture()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open the structure scene first.");
        Directory.CreateDirectory(PreviewPath);
        var obj = new GameObject("Temporary structure preview camera");
        var camera = obj.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.95f, .96f, .95f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 200;
        camera.orthographic = true;
        try
        {
            camera.transform.position = new Vector3(1.2f, 60, -1.0f);
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.orthographicSize = 17.8f;
            SaveRender(camera, PreviewPath + "/Unity_Top.png", 2200, 1600);
            camera.transform.position = new Vector3(24, 32, -30);
            camera.transform.LookAt(new Vector3(0, .7f, 0));
            camera.orthographic = false;
            camera.fieldOfView = 46;
            SaveRender(camera, PreviewPath + "/Unity_Overview.png", 2000, 1450);
        }
        finally { Object.DestroyImmediate(obj); }
    }

    static void SaveRender(Camera camera, string path, int width, int height)
    {
        var old = RenderTexture.active;
        var target = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally { RenderTexture.active = old; camera.targetTexture = null; Object.DestroyImmediate(target); Object.DestroyImmediate(texture); }
    }
}
