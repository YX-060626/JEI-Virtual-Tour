using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    static readonly Color SportsIvoryText = new Color(.98f, .94f, .85f);

    [MenuItem("Tools/JEI/Structure/Polish Sports Hall")]
    public static void PolishSportsHall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before updating the sports hall.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scenes before updating the sports hall.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        if (!view || view.floors.Length != 6 || !view.floors[4] || !view.floors[5])
            throw new InvalidOperationException("Open the six-floor JEI tour first.");

        Directory.CreateDirectory("Library/JEI_InteriorBackups");
        File.Copy(ScenePath, "Library/JEI_InteriorBackups/BeforeHallPolish_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        try
        {
            var floor5 = view.floors[4];
            var floor6 = view.floors[5];
            PolishSportsHallSurfaces(floor5, floor6);
            var steel = FitMat("Sports_Blue_Steel", new Color(.11f, .22f, .29f));
            BuildRoofStairSupports(floor5, steel);
            ApplyWorldTextDepth(scene);
            JEITourPerformanceSetup.InstallOnTour(view);
            Physics.SyncTransforms();
            VerifySportsAccess(view.floors[3], floor5);
            VerifySportsTerrace(floor5, floor6);
            VerifyRoofTerraceStairs(floor5, floor6);
            VerifyRoofGarden(floor5, floor6);
            VerifyExpandedLift(view);
            VerifySportsHallMounts(floor5);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the sports hall.");
            AssetDatabase.SaveAssets();
        }
        catch
        {
            EditorSceneManager.OpenScene(ScenePath);
            throw;
        }
        Debug.Log("JEI_SPORTS_HALL_POLISH_SUCCESS");
    }

    static void PolishSportsHallSurfaces(Transform floor5, Transform floor6)
    {
        ApplySportsEntryOak(floor5);
        BuildMountedSportsWayfinding(floor5);

        int merged = 0;
        merged += BakeSmallDecorations(floor5.Find("Central basketball court"), "Basketball");
        merged += BakeSmallDecorations(floor5.Find("Badminton court"), "Badminton");
        merged += BakeSmallDecorations(floor5.Find("Table tennis area"), "TableTennis");
        merged += BakeSmallDecorations(floor6.Find("Circular garden court and reflecting pool"), "ReflectingPool");
        merged += BakeSmallDecorations(floor6.Find("Dry garden with red gravel and succulents"), "DryGarden");
        merged += BakeSmallDecorations(floor6.Find("Woodland garden with layered foliage"), "WoodlandGarden");
        Debug.Log($"JEI_DECOR_BATCHING: {merged} fewer MeshRenderers across the courts and roof gardens.");
    }

    static void ApplySportsEntryOak(Transform floor)
    {
        var rooms = floor.Find("Changing rooms and showers");
        if (!rooms) throw new InvalidOperationException("The sports changing rooms are missing.");
        var oak = FitMat("Sports_Entry_Natural_Oak", new Color(.96f, .88f, .76f));
        oak.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FitoutAssets + "/Oak_Planks.png");
        oak.mainTextureScale = new Vector2(1.35f, 1.15f);
        oak.SetFloat("_Smoothness", .22f);
        EditorUtility.SetDirty(oak);
        var plaque = FitMat("Sports_Entry_Smoked_Oak", new Color(.37f, .235f, .141f));

        foreach (Transform panel in rooms)
        {
            if (panel.name != "Slatted entry screen") continue;
            panel.GetComponent<MeshRenderer>().sharedMaterial = oak;
        }
        foreach (var label in rooms.GetComponentsInChildren<TextMesh>())
        {
            if (label.text != "CHANGING" && label.text != "SHOWERS") continue;
            Vector3 point = label.transform.localPosition;
            string boardName = label.text + " smoked oak plaque";
            if (!rooms.Find(boardName))
                D(rooms, boardName, new Vector3(point.x, point.y, point.z + .005f),
                    new Vector3(1.66f, .35f, .034f), plaque);
            label.transform.localPosition = new Vector3(point.x, point.y, -7.33f);
            label.characterSize = .036f;
            label.color = SportsIvoryText;
            AssignWorldTextDepth(label);
        }
    }

    static void BuildMountedSportsWayfinding(Transform floor)
    {
        var old = floor.Find("Sports hall wayfinding");
        if (old) Object.DestroyImmediate(old.gameObject);
        var wing = Group(floor, "Sports hall wayfinding");
        var wood = FitMat("Sports_Entry_Smoked_Oak", new Color(.37f, .235f, .141f));
        var steel = FitMat("Sports_Blue_Steel", new Color(.11f, .22f, .29f));
        foreach (int side in new[] { -1, 1 })
        {
            var mount = Group(wing, side == -1 ? "West wall-mounted sign" : "East wall-mounted sign");
            mount.localPosition = new Vector3(side * 17.68f, 1.72f, -4.15f);
            mount.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
            D(mount, "Mounting backplate set into wall", new Vector3(0, 0, .035f),
                new Vector3(2.92f, .42f, .13f), steel);
            D(mount, "Oak wayfinding face", new Vector3(0, 0, -.044f),
                new Vector3(2.84f, .37f, .055f), wood);
            Label(mount, "05  SPORTS HALL   /   06  ROOF", new Vector3(0, 0, -.091f), .025f, SportsIvoryText);
        }
    }

    static int BakeSmallDecorations(Transform root, string assetId)
    {
        if (!root) return 0;
        var merges = root.GetComponentsInChildren<MeshRenderer>()
            .Where(renderer => renderer.enabled && !renderer.GetComponent<Collider>()
                && renderer.sharedMaterial && renderer.sharedMaterial.renderQueue <= 2500
                && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh)
            .GroupBy(renderer => new { renderer.sharedMaterial, renderer.shadowCastingMode, renderer.receiveShadows })
            .Where(group => group.Count() >= 4)
            .ToArray();

        int removed = 0;
        foreach (var batch in merges)
        {
            var first = batch.First();
            string name = $"{assetId}_{batch.Key.sharedMaterial.name}_{(int)batch.Key.shadowCastingMode}_{batch.Key.receiveShadows}";
            string path = $"{FitoutAssets}/SportsOptimized_{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var created = !mesh;
            if (created) mesh = new Mesh();
            else mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(batch.Select(renderer => new CombineInstance
            {
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
                transform = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix
            }).ToArray(), true, true);
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);

            var baked = Group(root, "Combined details - " + name);
            baked.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = baked.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = batch.Key.sharedMaterial;
            renderer.shadowCastingMode = batch.Key.shadowCastingMode;
            renderer.receiveShadows = batch.Key.receiveShadows;
            renderer.lightProbeUsage = first.lightProbeUsage;
            renderer.reflectionProbeUsage = first.reflectionProbeUsage;

            foreach (var source in batch)
            {
                Object.DestroyImmediate(source.GetComponent<MeshFilter>());
                Object.DestroyImmediate(source);
                removed++;
            }
            removed--;
        }
        return removed;
    }

    static void VerifySportsHallMounts(Transform floor)
    {
        var signs = floor.Find("Sports hall wayfinding");
        foreach (Transform mount in signs)
        {
            var label = mount.GetComponentInChildren<TextMesh>();
            Vector3 towardWall = mount.forward;
            if (!Physics.Raycast(label.transform.position - towardWall * .10f, towardWall, out var hit, .4f)
                || !hit.transform.IsChildOf(floor.Find("Double-height glazed sports hall")))
                throw new InvalidOperationException("This wayfinding sign is not mounted against the perimeter wall: " + mount.name);
        }
    }
}
