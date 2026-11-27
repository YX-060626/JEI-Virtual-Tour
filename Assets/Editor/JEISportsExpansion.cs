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
    const string SportsFloorName = "Floor_05__Sports_Terrace__Level_14.40m";
    const float SportsFloorElevation = 14.4f;
    const float SportsClearHeight = 7.2f;

    [MenuItem("Tools/JEI/Structure/Add Cafe and Sports Floors")]
    public static void BuildSportsExpansion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before expanding the building.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scenes before expanding the building.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        var view = Object.FindFirstObjectByType<JEIStructureView>();
        if (!view || !view.walkPlayer || view.floors.Length != 3 || view.floors.Any(floor => !floor))
            throw new InvalidOperationException("The three existing tour floors are required.");
        var buildingRoot = view.floors[0].parent;
        if (!view.roof || view.roof.parent != buildingRoot || view.roof.name != "Roof__Level_10.80m")
            throw new InvalidOperationException("The original 10.8 m roof could not be identified.");

        Directory.CreateDirectory("Library/JEI_InteriorBackups");
        File.Copy(ScenePath, "Library/JEI_InteriorBackups/BeforeSports_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        try
        {
            Object.DestroyImmediate(view.roof.gameObject);
            view.roof = null;
            BuildCourtyardCafe(view);
            var floor4 = BuildSportsAccess(view);
            var floor5 = Group(buildingRoot, SportsFloorName);
            floor5.gameObject.AddComponent<JEIGeneratedObject>();
            floor5.localPosition = Vector3.up * SportsFloorElevation;
            BuildSportsTerrace(floor5);
            BuildStairOpeningWalls(view.floors[2], floor4);
            BuildStairOpeningWalls(floor4, floor5);
            CloseGalleryStairEnds(floor4, floor5);
            view.floors = view.floors.Concat(new[] { floor4, floor5 }).ToArray();
            BuildTourLift(view);
            VerifyExpandedLift(view);
            Physics.SyncTransforms();
            VerifySportsAccess(floor4, floor5);
            VerifySportsTerrace(floor5);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the expanded building.");
            AssetDatabase.SaveAssets();
        }
        catch
        {
            EditorSceneManager.OpenScene(ScenePath);
            throw;
        }
        Debug.Log("JEI_SPORTS_EXPANSION_SUCCESS");
    }

    static void BuildSportsTerrace(Transform level)
    {
        var deck = FitMat("Sports_Concrete_Deck", new Color(.56f, .62f, .63f));
        var steel = FitMat("Sports_Blue_Steel", new Color(.11f, .22f, .29f));
        var blue = SportsSurface("Sports_Court_Blue", new Color(.17f, .39f, .53f), .18f);
        var blueDark = SportsSurface("Sports_Court_Blue_Dark", new Color(.10f, .27f, .38f), .15f);
        var clay = FitMat("Sports_Court_Clay", new Color(.74f, .36f, .24f));
        var green = SportsSurface("Sports_Court_Green", new Color(.19f, .42f, .35f), .21f);
        var line = SportsSurface("Sports_Court_Marking", new Color(.97f, .94f, .82f), .6f);
        var tile = FitMat("Sports_Wet_Tile", new Color(.75f, .83f, .82f));
        var wood = FitMat("Sports_Changing_Oak", new Color(.66f, .48f, .30f));
        var silver = FitMat("Sports_Brushed_Silver", new Color(.63f, .70f, .72f));

        BuildSportsFloorPlate(level, false, deck);
        BuildSportsArrivalGuards(level, steel);
        BuildSportsPerimeter(level, deck, steel, line);
        BuildBasketballCourt(level, blue, blueDark, clay, line, steel);
        BuildBadmintonCourt(level, green, line, steel);
        BuildTableTennisArea(level, clay, blueDark, line, steel);
        BuildSportsChangingRooms(level, tile, steel, wood, silver, line);

        var benches = Group(level, "Spectator benches");
        for (int i = 0; i < 3; i++)
        {
            float x = -13f + i * 3.05f;
            Box(benches, "Oak seat", new Vector3(x, .48f, 10.45f), new Vector3(2.55f, .11f, .47f), wood);
            Box(benches, "Seat support", new Vector3(x, .21f, 10.45f), new Vector3(2.26f, .42f, .27f), steel);
            Box(benches, "Backrest", new Vector3(x, .83f, 10.64f), new Vector3(2.55f, .19f, .09f), wood);
        }
        var signage = Group(level, "Sports terrace wayfinding");
        foreach (int side in new[] { -1, 1 })
        {
            float x = side * 15f;
            Box(signage, "Stair arrival direction sign", new Vector3(x, 2.42f, -4.65f),
                new Vector3(2.53f, .43f, .08f), steel, false);
            Label(signage, "05   SPORTS TERRACE", new Vector3(x, 2.42f, -4.60f), .036f, Color.white, 180f);
        }
    }

    static Material SportsSurface(string name, Color color, float fill)
    {
        var material = FitMat(name, color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * fill);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void BuildSportsPerimeter(Transform level, Material deck, Material steel, Material lamp)
    {
        var perimeter = Group(level, "Open-air perimeter and sports lighting");
        foreach (int side in new[] { -1, 1 })
        {
            float z = side * 12.9f;
            float x = side * 17.9f;
            Box(perimeter, "North/south safety parapet", new Vector3(0, .49f, z), new Vector3(36f, .98f, .2f), deck);
            Box(perimeter, "East/west safety parapet", new Vector3(x, .49f, 0), new Vector3(.2f, .98f, 25.6f), deck);
            Box(perimeter, "End cap", new Vector3(0, 1.02f, z), new Vector3(36f, .08f, .24f), steel);
            Box(perimeter, "Side cap", new Vector3(x, 1.02f, 0), new Vector3(.24f, .08f, 25.6f), steel);
        }

        // Open vertical fences retain balls while letting daylight reach the terrace.
        foreach (float z in new[] { -12.9f, 12.9f })
        {
            Box(perimeter, "High ball-stop rail", new Vector3(0, 3.12f, z), new Vector3(35.7f, .075f, .075f), steel);
            Box(perimeter, "Middle ball-stop rail", new Vector3(0, 2.05f, z), new Vector3(35.7f, .052f, .052f), steel);
            for (int i = -49; i <= 49; i++)
                Box(perimeter, "Ball-stop upright", new Vector3(i * .36f, 2.08f, z),
                    new Vector3(.034f, 2.08f, .046f), steel, false);
            var barrier = Group(perimeter, "Ball-stop collision");
            Solid(barrier, new Vector3(0, 2.07f, z), new Vector3(35.7f, 2.1f, .05f));
        }
        foreach (int side in new[] { -1, 1 })
        foreach (float z in new[] { -11.7f, 11.7f })
        {
            float x = side * 16.75f;
            Box(perimeter, "7.2 m court light mast", new Vector3(x, SportsClearHeight * .5f, z),
                new Vector3(.22f, SportsClearHeight, .22f), steel);
            Box(perimeter, "Floodlight housing", new Vector3(x - side * .32f, SportsClearHeight - .12f, z),
                new Vector3(.74f, .24f, .37f), steel, false);
            Box(perimeter, "Floodlight lens", new Vector3(x - side * .40f, SportsClearHeight - .25f, z),
                new Vector3(.55f, .028f, .29f), lamp, false);
        }
    }

    static void BuildBasketballCourt(Transform level, Material blue, Material dark, Material clay, Material paint, Material steel)
    {
        var court = Group(level, "Central basketball court");
        court.localRotation = Quaternion.Euler(0, 90f, 0);
        Box(court, "Blue court finish", new Vector3(0, .012f, 0), new Vector3(24.8f, .024f, 12.7f), blue, false);
        SportRectangle(court, Vector2.zero, 24f, 12f, paint);
        SportLine(court, new Vector2(0, -6f), new Vector2(0, 6f), paint);
        SportArc(court, Vector2.zero, 1.65f, 0, 360, 56, paint, .052f, .055f);

        foreach (int side in new[] { -1, 1 })
        {
            float end = side * 12f;
            float hoop = side * 10.55f;
            Box(court, "Painted key", new Vector3(side * 9.6f, .019f, 0), new Vector3(4.8f, .017f, 3.55f), dark, false);
            SportRectangle(court, new Vector2(side * 9.6f, 0), 4.8f, 3.55f, paint);
            SportArc(court, new Vector2(side * 7.2f, 0), 1.775f, side > 0 ? 90 : -90, 180, 28, paint, .052f, .055f);
            SportArc(court, new Vector2(hoop, 0), 5.1f, side > 0 ? 90 : -90, 180, 44, paint, .052f, .055f);
            foreach (int edge in new[] { -1, 1 })
                SportLine(court, new Vector2(hoop, edge * 5.1f), new Vector2(end, edge * 5.1f), paint);

            Box(court, "Padded upright", new Vector3(side * 12.75f, 1.59f, 0), new Vector3(.33f, 3.18f, .35f), steel);
            Box(court, "Basketball pole padding", new Vector3(side * 12.75f, .85f, 0),
                new Vector3(.49f, 1.70f, .52f), dark, false);
            Box(court, "Backboard arm", new Vector3(side * 11.83f, 3.47f, 0),
                new Vector3(1.98f, .13f, .19f), steel, false);
            Box(court, "Basketball backboard", new Vector3(side * 11.02f, 3.46f, 0),
                new Vector3(.09f, 1.05f, 1.80f), paint);
            Box(court, "Backboard target outline", new Vector3(side * 10.967f, 3.24f, 0),
                new Vector3(.016f, .43f, .62f), steel, false);
            Box(court, "Backboard target center", new Vector3(side * 10.953f, 3.24f, 0),
                new Vector3(.015f, .36f, .54f), paint, false);
            Box(court, "Rim attachment", new Vector3(side * 10.89f, 3.05f, 0),
                new Vector3(.18f, .055f, .16f), clay, false);
            SportArc(court, new Vector2(hoop, 0), .23f, 0, 360, 24, clay, 3.05f, .028f);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 2f * Mathf.PI / 10f;
                float next = angle + .28f;
                var rimEdge = new Vector3(hoop + .22f * Mathf.Cos(angle), 3.03f, .22f * Mathf.Sin(angle));
                var netEdge = new Vector3(hoop + .13f * Mathf.Cos(next), 2.74f, .13f * Mathf.Sin(next));
                SportRod(court, "Basket net cord", rimEdge, netEdge, .011f, paint);
            }
            SportArc(court, new Vector2(hoop, 0), .13f, 0, 360, 18, paint, 2.74f, .013f);
        }
    }

    static void BuildBadmintonCourt(Transform level, Material green, Material paint, Material steel)
    {
        var court = Group(level, "Badminton court");
        court.localRotation = Quaternion.Euler(0, 90f, 0);
        var center = new Vector2(2.1f, -9.58f);
        Box(court, "Badminton court finish", new Vector3(center.x, .012f, center.y),
            new Vector3(14.45f, .024f, 6.65f), green, false);
        SportRectangle(court, center, 13.4f, 6.1f, paint);
        foreach (int side in new[] { -1, 1 })
        {
            foreach (float offset in new[] { 1.98f, 5.94f })
                SportLine(court, center + new Vector2(side * offset, -3.05f), center + new Vector2(side * offset, 3.05f), paint);
            SportLine(court, center + new Vector2(side * 1.98f, 0), center + new Vector2(side * 6.7f, 0), paint);
            SportLine(court, center + new Vector2(-6.7f, side * 2.59f), center + new Vector2(6.7f, side * 2.59f), paint);
            Box(court, "Badminton post", new Vector3(center.x, .775f, center.y + side * 3.1f),
                new Vector3(.065f, 1.55f, .065f), steel);
        }
        Box(court, "Net white headband", new Vector3(center.x, 1.54f, center.y),
            new Vector3(.027f, .055f, 6.2f), paint, false);
        for (int i = 0; i <= 8; i++)
            Box(court, "Badminton net horizontal cord", new Vector3(center.x, .79f + i * .09f, center.y),
                new Vector3(.012f, .013f, 6.2f), steel, false);
        for (int i = -24; i <= 24; i++)
            Box(court, "Badminton net vertical cord", new Vector3(center.x, 1.15f, center.y + i * .125f),
                new Vector3(.010f, .73f, .010f), steel, false);
        Solid(court, new Vector3(center.x, 1.16f, center.y), new Vector3(.032f, .76f, 6.2f));
    }

    static void BuildTableTennisArea(Transform level, Material clay, Material blue, Material paint, Material steel)
    {
        var area = Group(level, "Table tennis area");
        Box(area, "Terracotta anti-slip finish", new Vector3(12.5f, .012f, 9.5f),
            new Vector3(9.3f, .024f, 5.5f), clay, false);
        for (int i = 0; i < 2; i++)
        {
            var table = Group(area, "Table tennis table " + (i + 1));
            table.localPosition = new Vector3(10.25f + i * 4.5f, 0, 9.5f);
            table.localRotation = Quaternion.Euler(0, 90f, 0);
            Box(table, "Regulation table top", new Vector3(0, .74f, 0), new Vector3(2.74f, .04f, 1.525f), blue);
            foreach (int side in new[] { -1, 1 })
            {
                Box(table, "End stripe", new Vector3(side * 1.355f, .764f, 0), new Vector3(.035f, .008f, 1.525f), paint, false);
                Box(table, "Side stripe", new Vector3(0, .764f, side * .747f), new Vector3(2.74f, .008f, .030f), paint, false);
                Box(table, "Support trestle", new Vector3(side * .93f, .36f, 0), new Vector3(.12f, .72f, .93f), steel);
            }
            Box(table, "Doubles center line", new Vector3(0, .765f, 0), new Vector3(2.74f, .008f, .013f), paint, false);
            Box(table, "Table net", new Vector3(0, .837f, 0), new Vector3(.03f, .15f, 1.68f), steel, false);
            Box(table, "Net tape", new Vector3(0, .916f, 0), new Vector3(.035f, .018f, 1.68f), paint, false);
        }
    }

    static void BuildSportsChangingRooms(Transform level, Material tile, Material steel, Material wood, Material silver, Material paint)
    {
        var rooms = Group(level, "Changing rooms and showers");
        const float front = -7.45f;
        const float back = -12.67f;
        const float roomHeight = 2.65f;
        const float left = 8.85f;
        foreach (float edge in new[] { left, 13.32f, 17.58f })
            Box(rooms, "Privacy wall", new Vector3(edge, roomHeight * .5f, (front + back) * .5f),
                new Vector3(.16f, roomHeight, front - back), tile);
        Box(rooms, "Interior rear wall", new Vector3(13.21f, roomHeight * .5f, back),
            new Vector3(8.87f, roomHeight, .14f), tile);
        for (int i = 0; i < 2; i++)
        {
            float x = left + .10f + 4.42f * i;
            Box(rooms, "Slatted entry screen", new Vector3(x + 3.02f, roomHeight * .5f, front),
                new Vector3(2.98f, roomHeight, .16f), steel);
            Box(rooms, "Privacy return behind entrance", new Vector3(x + 2.12f, roomHeight * .5f, front - 1.52f),
                new Vector3(.14f, roomHeight, 2.20f), tile);
            Box(rooms, "Slip resistant room floor", new Vector3(x + 2.07f, .012f, (front + back) * .5f),
                new Vector3(4.19f, .024f, front - back), tile, false);
            Label(rooms, i == 0 ? "CHANGING" : "SHOWERS", new Vector3(x + 3.01f, 2.13f, front + .09f),
                .044f, Color.white, 180f);
        }
        for (int i = 0; i < 6; i++)
        {
            float x = 9.25f + i * .63f;
            Box(rooms, "Changing locker", new Vector3(x, .98f, -12.19f), new Vector3(.57f, 1.94f, .52f), wood);
            Box(rooms, "Locker handle", new Vector3(x + .15f, 1.09f, -11.916f),
                new Vector3(.047f, .19f, .036f), silver, false);
        }
        Box(rooms, "Changing bench seat", new Vector3(11.39f, .46f, -10.78f),
            new Vector3(2.65f, .105f, .39f), wood);
        Box(rooms, "Changing bench frame", new Vector3(11.39f, .19f, -10.78f),
            new Vector3(2.43f, .38f, .28f), steel);
        for (int i = 0; i < 3; i++)
        {
            float x = 14.08f + 1.32f * i;
            Box(rooms, "Shower tray", new Vector3(x, .035f, -11.93f), new Vector3(1.18f, .07f, 1.22f), paint, false);
            if (i < 2)
                Box(rooms, "Shower partition", new Vector3(x + .66f, 1.11f, -11.86f),
                    new Vector3(.075f, 2.05f, 1.40f), tile);
            Box(rooms, "Shower riser", new Vector3(x, 1.56f, -12.55f),
                new Vector3(.042f, 1.12f, .045f), silver, false);
            Box(rooms, "Shower arm", new Vector3(x, 2.09f, -12.40f),
                new Vector3(.045f, .044f, .31f), silver, false);
            Box(rooms, "Rain shower head", new Vector3(x, 2.06f, -12.22f),
                new Vector3(.26f, .045f, .23f), silver, false);
            Box(rooms, "Linear floor drain", new Vector3(x, .072f, -11.63f),
                new Vector3(.59f, .006f, .052f), steel, false);
        }
    }

    static void SportRectangle(Transform root, Vector2 center, float width, float depth, Material material)
    {
        var a = center + new Vector2(-width, -depth) * .5f;
        var b = center + new Vector2(width, -depth) * .5f;
        var c = center + new Vector2(width, depth) * .5f;
        var d = center + new Vector2(-width, depth) * .5f;
        SportLine(root, a, b, material);
        SportLine(root, b, c, material);
        SportLine(root, c, d, material);
        SportLine(root, d, a, material);
    }

    static void SportLine(Transform root, Vector2 a, Vector2 b, Material material) =>
        SportRod(root, "Court marking", new Vector3(a.x, .052f, a.y), new Vector3(b.x, .052f, b.y), .052f, material, .004f);

    static void SportArc(Transform root, Vector2 center, float radius, float start, float sweep, int count, Material material, float height, float width)
    {
        Vector3 Point(int index)
        {
            float radians = (start + sweep * index / count) * Mathf.Deg2Rad;
            return new Vector3(center.x + radius * Mathf.Cos(radians), height, center.y + radius * Mathf.Sin(radians));
        }
        for (int i = 0; i < count; i++)
            SportRod(root, "Court arc or rim", Point(i), Point(i + 1), width, material, height < .10f ? .004f : width);
    }

    static void SportRod(Transform root, string name, Vector3 from, Vector3 to, float width, Material material, float thickness = .011f)
    {
        Vector3 offset = to - from;
        var part = Box(root, name, (from + to) * .5f, new Vector3(width, thickness, offset.magnitude + .004f), material, false);
        part.transform.localRotation = Quaternion.LookRotation(offset);
        var renderer = part.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static void VerifySportsTerrace(Transform level, Transform ceiling = null)
    {
        void ExpectDeck(float x, float z)
        {
            if (!Physics.Raycast(new Vector3(x, SportsFloorElevation + .15f, z), Vector3.down, out var hit, .4f)
                || !hit.transform.IsChildOf(level) || Mathf.Abs(hit.point.y - SportsFloorElevation) > .03f)
                throw new InvalidOperationException($"Sports terrace floor missing at ({x}, {z}).");
        }
        foreach (float x in new[] { -3.7f, 0, 3.7f })
        foreach (float z in new[] { -3f, 0, 3f })
            ExpectDeck(x, z);
        ExpectDeck(-9f, -5f);
        ExpectDeck(12.5f, 9.5f);
        ExpectDeck(10f, -9.5f);
        ExpectDeck(15f, -9.5f);
        var probe = new GameObject("Temporary sports terrace walking probe");
        var capsule = probe.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.radius = .3f;
        capsule.center = Vector3.up * .9f;
        capsule.stepOffset = .25f;
        capsule.skinWidth = .035f;
        try
        {
            void WalkFromLift(params Vector2[] waypoints)
            {
                PlaceProbe(capsule, new Vector3(10.9f, SportsFloorElevation + .08f, 4.6f));
                foreach (var target in waypoints) WalkTo(capsule, target);
                AssertHeight(capsule, SportsFloorElevation);
            }
            WalkFromLift(new Vector2(0, 4.6f), Vector2.zero, new Vector2(-10.5f, 0));
            WalkFromLift(new Vector2(12.5f, 6.8f), new Vector2(12.5f, 9.5f));
            WalkFromLift(new Vector2(7.5f, 4.6f), new Vector2(7.5f, -6.6f),
                new Vector2(9.76f, -6.6f), new Vector2(9.76f, -9.35f));
            WalkFromLift(new Vector2(7.5f, 4.6f), new Vector2(7.5f, -6.6f),
                new Vector2(14.17f, -6.6f), new Vector2(14.17f, -9.35f));
        }
        finally { Object.DestroyImmediate(probe); }

        bool overhead = Physics.Raycast(new Vector3(0, SportsFloorElevation + .2f, 0), Vector3.up, out var roofHit, SportsClearHeight);
        if (ceiling ? !overhead || !roofHit.transform.IsChildOf(ceiling) : overhead)
            throw new InvalidOperationException("Unexpected ceiling layout above the central court.");
    }
}
