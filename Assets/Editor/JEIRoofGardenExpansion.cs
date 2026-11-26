using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string RoofGardenName = "Floor_06__Sky_Garden__Level_21.60m";
    const float RoofGardenElevation = SportsFloorElevation + SportsClearHeight;
    const float RoofDeckThickness = .30f;
    static readonly Vector2 RoofFountainCenter = new Vector2(0, .35f);

    [MenuItem("Tools/JEI/Structure/Add Sixth Floor Roof Garden")]
    public static void BuildRoofGardenExpansion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before expanding the building.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before expanding the building.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        if (!view || !view.walkPlayer || view.floors.Length != 5 || view.floors.Any(floor => !floor))
            throw new InvalidOperationException("Build the existing five-floor tour first.");
        var floor5 = view.floors[4];
        var oldPerimeter = floor5.Find("Open-air perimeter and sports lighting");
        if (!oldPerimeter || Mathf.Abs(floor5.position.y - SportsFloorElevation) > .01f)
            throw new InvalidOperationException("The fifth-floor sports terrace was not found.");

        Directory.CreateDirectory("Library/JEI_InteriorBackups");
        File.Copy(ScenePath, "Library/JEI_InteriorBackups/BeforeRoofGarden_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        try
        {
            Object.DestroyImmediate(oldPerimeter.gameObject);
            floor5.name = "Floor_05__Indoor_Sports_Hall__Level_14.40m";
            var floor6 = Group(floor5.parent, RoofGardenName);
            floor6.gameObject.AddComponent<JEIGeneratedObject>();
            floor6.localPosition = Vector3.up * RoofGardenElevation;

            var frame = FitMat("Sports_Blue_Steel", new Color(.11f, .22f, .29f));
            var glazing = LoadSportsAccessMaterial("Assets/JEI_Fitout/Room_Clear_Glazing.mat");
            BuildSportsHall(floor5, frame, glazing);
            BuildRoofGarden(floor6, frame, glazing);
            BuildRoofTerraceStairs(floor5, floor6, frame);
            UpdateRoofGardenWayfinding(view.floors[3], floor5);
            view.floors = view.floors.Concat(new[] { floor6 }).ToArray();
            BuildTourLift(view);
            PolishSportsHallSurfaces(floor5, floor6);
            JEITourPerformanceSetup.InstallOnTour(view);
            ApplyWorldTextDepth(scene);

            Physics.SyncTransforms();
            VerifySportsAccess(view.floors[3], floor5);
            VerifySportsTerrace(floor5, floor6);
            VerifyRoofTerraceStairs(floor5, floor6);
            VerifyRoofGarden(floor5, floor6);
            VerifyExpandedLift(view);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the roof garden.");
            AssetDatabase.SaveAssets();
        }
        catch
        {
            EditorSceneManager.OpenScene(ScenePath);
            throw;
        }
        Debug.Log("JEI_ROOF_GARDEN_SUCCESS");
    }

    static void BuildSportsHall(Transform floor, Material frame, Material glazing)
    {
        var shell = Group(floor, "Double-height glazed sports hall");
        float height = SportsClearHeight - RoofDeckThickness;
        var baseWall = FitMat("RoofGarden_Hall_Limestone", new Color(.62f, .70f, .69f));
        var panel = FitMat("RoofGarden_Hall_Acoustic_Panel", new Color(.21f, .33f, .37f));
        var lamp = SportsSurface("RoofGarden_Hall_Light_Diffuser", new Color(.95f, .98f, .94f), 1.0f);
        foreach (float z in new[] { -12.85f, 12.85f })
            BuildHallElevation(shell, new Vector3(0, 0, z), 0f, 36f, 8, height, baseWall, frame, glazing);
        foreach (float x in new[] { -17.85f, 17.85f })
            BuildHallElevation(shell, new Vector3(x, 0, 0), 90f, 25.4f, 6, height, baseWall, frame, glazing);

        var lighting = Group(shell, "Ceiling sports lighting and acoustic panels");
        foreach (float x in new[] { -10.5f, 0f, 10.5f })
        foreach (float z in new[] { -8.5f, 0f, 8.5f })
        {
            // Both stair cores and the lift remain clear at their roof penetrations.
            if (Mathf.Abs(x - JEILiftController.ShaftX) < 1f && Mathf.Abs(z) < 1f) continue;
            D(lighting, "Acoustic ceiling field", new Vector3(x, height - .035f, z),
                new Vector3(4.65f, .07f, 1.28f), panel);
            D(lighting, "Fixture housing", new Vector3(x, height - .09f, z),
                new Vector3(3.72f, .08f, .32f), frame);
            D(lighting, "Diffuse LED strip", new Vector3(x, height - .14f, z),
                new Vector3(3.48f, .035f, .27f), lamp);
            var fill = Group(lighting, "High-bay light");
            fill.localPosition = new Vector3(x, height - .26f, z);
            var light = fill.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12.5f;
            light.intensity = 4.5f;
            light.color = new Color(.9f, .95f, 1f);
            light.shadows = LightShadows.None;
        }
    }

    static void BuildHallElevation(Transform root, Vector3 position, float yaw, float length, int bays,
        float height, Material stone, Material frame, Material glazing)
    {
        var wall = Group(root, "Sports hall window wall");
        wall.localPosition = position;
        wall.localRotation = Quaternion.Euler(0, yaw, 0);
        const float sill = 2.1f;
        const float lintel = 5.62f;
        Box(wall, "Lower ball-resistant wall", new Vector3(0, sill * .5f, 0), new Vector3(length, sill, .30f), stone);
        Box(wall, "Upper wall fascia", new Vector3(0, (lintel + height) * .5f, 0),
            new Vector3(length, height - lintel, .30f), stone);
        float module = length / bays;
        for (int i = 0; i < bays; i++)
        {
            float center = -length * .5f + (i + .5f) * module;
            var positionOnWall = new Vector3(center, (lintel + sill) * .5f, 0);
            AddOpeningGlass(wall, "High-level sports glazing", positionOnWall, new Vector3(module - .24f, lintel - sill, .06f), glazing, true);
            Box(wall, "Window jamb", new Vector3(center - module * .5f + .12f, positionOnWall.y, 0),
                new Vector3(.24f, lintel - sill, .27f), frame);
            D(wall, "Window transom", new Vector3(center, 4.43f, 0), new Vector3(module, .09f, .15f), frame);
        }
        Box(wall, "End jamb", new Vector3(length * .5f - .12f, (lintel + sill) * .5f, 0),
            new Vector3(.24f, lintel - sill, .27f), frame);
        D(wall, "Continuous sill trim", new Vector3(0, sill, 0), new Vector3(length, .12f, .37f), frame);
        D(wall, "Continuous head trim", new Vector3(0, lintel, 0), new Vector3(length, .12f, .37f), frame);
    }

    static void UpdateRoofGardenWayfinding(Transform floor4, Transform floor5)
    {
        var gallery = floor4.Find("03_Gallery_Seating_and_Wayfinding");
        foreach (var sign in gallery.GetComponentsInChildren<TextMesh>())
        {
            sign.text = "04 GALLERY   /   05 SPORTS";
            sign.characterSize = .027f;
            sign.gameObject.name = "Text__04 Gallery - 05 Sports";
        }
        var sports = floor5.Find("Sports terrace wayfinding");
        sports.name = "Sports hall wayfinding";
        foreach (var sign in sports.GetComponentsInChildren<TextMesh>())
        {
            sign.text = "05 SPORTS HALL   /   06 ROOF";
            sign.characterSize = .024f;
            sign.gameObject.name = "Text__05 Sports Hall - 06 Roof";
        }
    }

    sealed class RoofGardenMaterials
    {
        public readonly Material paving = FitMat("RoofGarden_Paving", new Color(.76f, .71f, .61f));
        public readonly Material ivory = FitMat("RoofGarden_Ivory_Stone", new Color(.91f, .85f, .71f));
        public readonly Material accent = FitMat("RoofGarden_Bronze_Inlay", new Color(.44f, .29f, .15f));
        public readonly Material rim = FitMat("RoofGarden_Planter_Stone", new Color(.67f, .57f, .41f));
        public readonly Material mulch = FitMat("RoofGarden_Red_Gravel", new Color(.57f, .31f, .22f));
        public readonly Material darkSoil = FitMat("RoofGarden_Planting_Soil", new Color(.22f, .18f, .13f));
        public readonly Material oak = FitMat("RoofGarden_Thermal_Wood", new Color(.61f, .37f, .22f));
        public readonly Material agave = FitMat("RoofGarden_Agave", new Color(.29f, .50f, .40f));
        public readonly Material broadLeaf = FitMat("RoofGarden_Green_Foliage", new Color(.19f, .44f, .24f));
        public readonly Material lime = FitMat("RoofGarden_Lime_Foliage", new Color(.48f, .61f, .27f));
        public readonly Material bloom = FitMat("RoofGarden_Copper_Bloom", new Color(.73f, .36f, .24f));
        public readonly Material water = FitMat("RoofGarden_Water", new Color(.13f, .43f, .50f));
        public readonly Material ripple = FitMat("RoofGarden_Water_Highlight", new Color(.42f, .67f, .66f));
    }

    static void BuildRoofGarden(Transform floor, Material frame, Material glazing)
    {
        var palette = new RoofGardenMaterials();
        BuildSportsFloorPlate(floor, false, palette.paving);
        BuildRoofGardenBalustrade(floor, palette, frame, glazing);
        BuildRoofGardenCourt(floor, palette);
        BuildRoofPlanting(floor, palette, true, -10.65f, 8.70f);
        BuildRoofPlanting(floor, palette, false, 8.50f, 9.20f);
        foreach (int side in new[] { -1, 1 })
            BuildRoofRestTerrace(floor, palette, frame, side * 7.3f);

        var arrival = Group(floor, "Roof garden wayfinding and path lights");
        foreach (int side in new[] { -1, 1 })
        {
            float x = side * 15.8f;
            Box(arrival, "Garden arrival marker", new Vector3(x, .92f, -5.8f),
                new Vector3(1.65f, 1.84f, .22f), frame);
            D(arrival, "Bronze wayfinding face", new Vector3(x, 1.34f, -5.673f),
                new Vector3(1.50f, .56f, .025f), palette.accent);
            Label(arrival, "06  SKY GARDEN", new Vector3(x, 1.34f, -5.656f), .021f, Color.white, 180f);
        }
        var diffuser = SportsSurface("RoofGarden_Bollard_Lens", new Color(1f, .83f, .55f), 1.1f);
        foreach (float x in new[] { -11.0f, 11.0f })
        foreach (float z in new[] { -6.55f, 6.10f })
        {
            Box(arrival, "Path bollard", new Vector3(x, .32f, z), new Vector3(.15f, .64f, .15f), frame);
            D(arrival, "Warm bollard lens", new Vector3(x, .60f, z), new Vector3(.16f, .06f, .16f), diffuser);
        }
    }

    static void BuildRoofGardenBalustrade(Transform floor, RoofGardenMaterials palette, Material frame, Material glazing)
    {
        var rail = Group(floor, "Rooftop continuous protective balustrade");
        foreach (float z in new[] { -12.85f, 12.85f })
            BuildGardenRailFace(rail, new Vector3(0, 0, z), 0, 36f, 12, palette.ivory, frame, glazing);
        foreach (float x in new[] { -17.85f, 17.85f })
            BuildGardenRailFace(rail, new Vector3(x, 0, 0), 90, 25.4f, 9, palette.ivory, frame, glazing);
    }

    static void BuildGardenRailFace(Transform root, Vector3 position, float yaw, float length, int bays,
        Material stone, Material frame, Material glazing)
    {
        var edge = Group(root, "Stone and glass roof edge");
        edge.localPosition = position;
        edge.localRotation = Quaternion.Euler(0, yaw, 0);
        Box(edge, "Terrace parapet", new Vector3(0, .34f, 0), new Vector3(length, .68f, .30f), stone);
        Box(edge, "Protective handrail", new Vector3(0, 1.28f, 0), new Vector3(length, .09f, .12f), frame);
        float step = length / bays;
        for (int i = 0; i < bays; i++)
        {
            float center = -length * .5f + (i + .5f) * step;
            AddOpeningGlass(edge, "Laminated balustrade glass", new Vector3(center, .968f, 0),
                new Vector3(step, .576f, .045f), glazing, true);
            D(edge, "Balustrade mullion", new Vector3(center - step * .5f + .04f, .97f, 0),
                new Vector3(.08f, .61f, .09f), frame);
        }
    }

    static void BuildRoofGardenCourt(Transform floor, RoofGardenMaterials palette)
    {
        var court = Group(floor, "Circular garden court and reflecting pool");
        court.localPosition = new Vector3(RoofFountainCenter.x, 0, RoofFountainCenter.y);
        GardenDisc(court, "Circular pale stone paving", Vector3.up * .009f, 6.6f, .018f, palette.ivory);
        SportArc(court, Vector2.zero, 6.50f, 0, 360f, 84, palette.accent, .024f, .041f);
        SportArc(court, Vector2.zero, 6.25f, 0, 360f, 84, palette.accent, .023f, .023f);
        for (int i = 0; i < 12; i++)
        {
            float rad = i * Mathf.PI / 6f;
            var bearing = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
            SportRod(court, "Radial paving inlay", bearing * 4.15f + Vector3.up * .024f,
                bearing * 6.24f + Vector3.up * .024f, .022f, palette.accent, .004f);
        }
        GardenDisc(court, "Low circular stone basin", Vector3.up * .205f, 3.54f, .41f, palette.rim, true);
        GardenDisc(court, "Reflecting water", Vector3.up * .416f, 3.26f, .018f, palette.water);
        SportArc(court, new Vector2(.28f, .18f), 1.9f, 10f, 92f, 20, palette.ripple, .435f, .032f);
        SportArc(court, new Vector2(-.15f, -.08f), 2.68f, 150f, 80f, 22, palette.ripple, .435f, .026f);
        GardenDisc(court, "Planted island curb", Vector3.up * .50f, .94f, .22f, palette.ivory);
        GardenDisc(court, "Island planting bed", Vector3.up * .619f, .79f, .035f, palette.darkSoil);
        BuildRoofTree(court, new Vector3(0, .64f, 0), 1f, palette);
        BuildGardenSucculent(court, new Vector3(-.42f, .64f, .39f), .33f, palette.agave, 1f);
        BuildGardenSucculent(court, new Vector3(.49f, .64f, -.35f), .32f, palette.lime, 2f);
    }

    static GameObject GardenDisc(Transform root, string name, Vector3 point, float radius, float height, Material material,
        bool solid = false)
    {
        var item = Shape(root, name, PrimitiveType.Cylinder, point, new Vector3(radius * 2f, height * .5f, radius * 2f), material);
        if (solid)
        {
            var collider = item.AddComponent<MeshCollider>();
            collider.sharedMesh = item.GetComponent<MeshFilter>().sharedMesh;
            collider.convex = true;
        }
        return item;
    }

    static void BuildRoofPlanting(Transform floor, RoofGardenMaterials palette, bool dry, float x, float z)
    {
        var bed = Group(floor, dry ? "Dry garden with red gravel and succulents" : "Woodland garden with layered foliage");
        bed.localPosition = new Vector3(x, 0, z);
        const float width = 7.7f;
        const float depth = 4.8f;
        Box(bed, "Raised limestone planter", new Vector3(0, .265f, 0), new Vector3(width, .53f, depth), palette.rim);
        D(bed, "Planting surface", new Vector3(0, .538f, 0),
            new Vector3(width - .38f, .026f, depth - .38f), dry ? palette.mulch : palette.darkSoil);
        if (dry)
        {
            for (int i = 0; i < 7; i++)
            {
                float px = (i % 4 - 1.5f) * 1.75f;
                float pz = i < 4 ? -.93f : 1.01f;
                BuildGardenSucculent(bed, new Vector3(px, .55f, pz), .66f + .065f * i,
                    i % 3 == 0 ? palette.lime : palette.agave, i * .3f);
            }
            foreach (var rock in new[] { new Vector3(-2.65f, .80f, .72f), new Vector3(.45f, .72f, .62f), new Vector3(2.67f, .75f, .64f) })
                Shape(bed, "Warm sandstone accent", PrimitiveType.Sphere, rock, new Vector3(.95f, .55f, .7f), palette.accent);
        }
        else
        {
            BuildRoofTree(bed, new Vector3(-1.9f, .55f, .33f), .91f, palette);
            BuildRoofTree(bed, new Vector3(1.52f, .55f, .45f), 1.09f, palette);
            for (int i = 0; i < 8; i++)
                BuildGardenSucculent(bed, new Vector3(-3f + i * .86f, .55f, -1.38f + .21f * (i % 3)),
                    .48f + .16f * (i % 3), i % 3 == 0 ? palette.lime : palette.broadLeaf, i);
            for (int i = 0; i < 6; i++)
                Shape(bed, "Bronze foliage mound", PrimitiveType.Sphere, new Vector3(-2.75f + 1.08f * i, .77f, 1.42f),
                    new Vector3(.78f, .56f, .69f), i % 2 == 0 ? palette.bloom : palette.broadLeaf);
        }

        RoofGardenBench(bed, new Vector3(0, 0, -2.35f), 3.8f, palette.ivory, palette.oak);
    }

    static void BuildRoofTree(Transform root, Vector3 origin, float size, RoofGardenMaterials palette)
    {
        var tree = Group(root, "Multi-stem garden tree");
        tree.localPosition = origin;
        for (int i = 0; i < 5; i++)
        {
            float rad = i * .4f * Mathf.PI;
            var tip = new Vector3(Mathf.Cos(rad) * .62f, 1.88f + .13f * (i % 3), Mathf.Sin(rad) * .55f) * size;
            GardenLeaf(tree, "Tree stem", Vector3.up * .15f, tip, .10f * size, palette.oak);
            Shape(tree, "Layered canopy", PrimitiveType.Sphere, tip + Vector3.up * .20f * size,
                new Vector3(1.35f, .92f, 1.28f) * size, i % 3 == 0 ? palette.lime : palette.broadLeaf);
        }
        Shape(tree, "Upper canopy", PrimitiveType.Sphere, Vector3.up * (2.58f * size),
            new Vector3(1.44f, 1.02f, 1.37f) * size, palette.agave);
    }

    static void BuildGardenSucculent(Transform root, Vector3 origin, float size, Material material, float phase)
    {
        for (int i = 0; i < 8; i++)
        {
            float rad = i * Mathf.PI * .25f + phase;
            var tip = origin + new Vector3(Mathf.Cos(rad) * .57f, .44f + .12f * (i % 3), Mathf.Sin(rad) * .57f) * size;
            GardenLeaf(root, "Succulent rosette leaf", origin, tip, size * .125f, material);
        }
    }

    static void GardenLeaf(Transform root, string name, Vector3 from, Vector3 to, float width, Material material)
    {
        Vector3 axis = to - from;
        var leaf = Shape(root, name, PrimitiveType.Capsule, (to + from) * .5f,
            new Vector3(width, axis.magnitude * .5f, width * .72f), material);
        leaf.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
    }

    static void BuildRoofRestTerrace(Transform floor, RoofGardenMaterials palette, Material frame, float x)
    {
        var terrace = Group(floor, "Roof promenade seating");
        terrace.localPosition = new Vector3(x, 0, -9.36f);
        D(terrace, "Pale stone seating terrace", new Vector3(0, .013f, 0),
            new Vector3(6.45f, .026f, 4.6f), palette.ivory);
        RoofGardenBench(terrace, new Vector3(0, 0, -1.65f), 4.55f, frame, palette.oak);
        GardenDisc(terrace, "Round communal table base", new Vector3(0, .37f, .15f), .18f, .74f, frame, true);
        GardenDisc(terrace, "Round communal tabletop", new Vector3(0, .75f, .15f), .72f, .085f, palette.oak, true);
        foreach (float offset in new[] { -1.66f, 1.66f })
        {
            Box(terrace, "Stone seat plinth", new Vector3(offset, .20f, .23f), new Vector3(.68f, .4f, .55f), palette.rim);
            Box(terrace, "Timber seat top", new Vector3(offset, .44f, .23f), new Vector3(.71f, .08f, .59f), palette.oak);
        }
    }

    static void RoofGardenBench(Transform parent, Vector3 seat, float length, Material support, Material wood)
    {
        Box(parent, "Garden bench pedestal", seat + Vector3.up * .215f, new Vector3(length - .12f, .43f, .35f), support);
        Box(parent, "Garden bench wood seat", seat + Vector3.up * .46f, new Vector3(length, .09f, .56f), wood);
    }

    static void VerifyRoofGarden(Transform floor5, Transform floor6)
    {
        float floorY = floor6.position.y;
        if (Mathf.Abs(floorY - floor5.position.y - 7.2f) > .015f)
            throw new InvalidOperationException("The sports hall must span two original storeys.");
        foreach (Vector2 point in new[] { Vector2.zero, new Vector2(-8.5f, 8.7f), new Vector2(0, -9f) })
        {
            var start = new Vector3(point.x, floor5.position.y + 4f, point.y);
            if (!Physics.Raycast(start, Vector3.up, out var hit, 4.0f) || !hit.transform.IsChildOf(floor6)
                || Mathf.Abs(hit.point.y - (floorY - RoofDeckThickness)) > .02f)
                throw new InvalidOperationException("The sports hall roof deck is missing at " + point);
        }

        foreach (Vector2 point in new[] { new Vector2(0, -5.8f), new Vector2(-5.0f, 6.7f), new Vector2(9.5f, 4.7f) })
        {
            if (Physics.Raycast(new Vector3(point.x, floorY + .2f, point.y), Vector3.up, 15f))
                throw new InvalidOperationException("The roof garden promenade must remain open-air.");
        }

        var probe = new GameObject("Temporary roof garden walkway probe");
        var capsule = probe.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.center = Vector3.up * .9f;
        capsule.radius = .3f;
        capsule.stepOffset = .25f;
        capsule.skinWidth = .035f;
        try
        {
            PlaceProbe(capsule, new Vector3(10.9f, floorY + .08f, 4.6f));
            foreach (Vector2 point in new[] { new Vector2(5.3f, 4.6f), new Vector2(0, 5.4f), new Vector2(-5.3f, 4.6f),
                         new Vector2(-7.0f, 1f), new Vector2(-7.0f, -5f), new Vector2(0, -5f),
                         new Vector2(9.8f, -5f), new Vector2(13.3f, -4.6f) })
                WalkTo(capsule, point);
            AssertHeight(capsule, floorY);
        }
        finally { Object.DestroyImmediate(probe); }

        foreach (Vector3 outward in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
        {
            float edgeDistance = Mathf.Abs(outward.x) > .5f ? 17.85f : 12.85f;
            var origin = new Vector3(0, floorY + 1.03f, 0) + outward * (edgeDistance - 1f);
            if (!Physics.Raycast(origin, outward, out var hit, 1.3f) || !hit.transform.IsChildOf(floor6))
                throw new InvalidOperationException("The roof garden edge has no physical guard at " + outward);
        }
    }
}
