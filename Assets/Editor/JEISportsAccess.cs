using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string SportsGalleryName = "Floor_04__Atrium_Gallery__Level_10.80m";
    const string SportsStairExtensionName = "05_Stairs__to_floor_04";
    const string GalleryBalustradeName = "02_Atrium_Glass_Balustrade";
    const string GalleryGlassSideName = "Full-height glazed side";
    const string GalleryRailMaterialPath = "Assets/JEI_Blockout/Materials/Door_and_Window_Frame.mat";
    const string GalleryGlassSourcePath = "Assets/JEI_UserInteriors/JEI_Furnishings/Materials/S01_Clear_Office_Glass.mat";
    const string AtriumCeilingPanelName = "Slab panel over the atrium";
    const float SportsSlabThickness = .30f;

    static Transform BuildSportsAccess(JEIStructureView view)
    {
        var floor3 = view.floors[2];
        var buildingRoot = floor3.parent;
        var existingStair = floor3.Find("04_Stairs__arrival_only");
        if (!existingStair)
            throw new InvalidOperationException("The third-floor stair landings are missing.");

        var floor4 = buildingRoot.Find(SportsGalleryName);
        if (floor4) Object.DestroyImmediate(floor4.gameObject);
        var extension = floor3.Find(SportsStairExtensionName);
        if (extension) Object.DestroyImmediate(extension.gameObject);

        var railMaterial = LoadSportsAccessMaterial(GalleryRailMaterialPath);
        var stairMaterial = LoadSportsAccessMaterial("Assets/JEI_Blockout/Materials/Stairs.mat");
        var glazing = LoadSportsAccessMaterial(GalleryGlassSourcePath);
        var facade = FitMat("Gallery_Limestone", new Color(.73f, .75f, .73f));
        var gallerySlab = FitMat("Gallery_Terrazzo", new Color(.61f, .64f, .63f));
        var benchWood = FitMat("Oak_Furniture", new Color(.64f, .43f, .25f));

        foreach (string name in new[] { "West opening guards", "East opening guards" })
        {
            var guards = existingStair.Find(name);
            if (!guards) throw new InvalidOperationException("Missing third-floor guard group: " + name);
            foreach (string terminalGuard in new[] { "Terminal landing guard", "Terminal stair well end guard" })
            {
                var guard = guards.Find(terminalGuard);
                if (guard) Object.DestroyImmediate(guard.gameObject);
            }
        }

        var newExtension = Group(floor3, SportsStairExtensionName);
        var gallery = Group(buildingRoot, SportsGalleryName);
        gallery.localPosition = floor3.localPosition + Vector3.up * 3.6f;

        BuildSportsFloorPlate(gallery, true, gallerySlab);
        BuildGalleryFacade(gallery, facade, railMaterial, glazing);
        float soffitHeight = SportsFloorElevation - gallery.localPosition.y - SportsSlabThickness;
        BuildGalleryAtriumGlazing(gallery, soffitHeight, railMaterial, glazing);
        var galleryStairs = Group(gallery, "04_Stairs__to_floor_05");

        // The existing switchback geometry aligns with the shaft and the third-floor access doors.
        Material previousFrame = frame;
        bool hadStairFinish = finishes.TryGetValue("stair", out var previousStairFinish);
        try
        {
            frame = railMaterial;
            finishes["stair"] = stairMaterial;
            foreach (int side in new[] { -1, 1 })
            {
                BuildStair(newExtension, side, true);
                BuildStair(galleryStairs, side, true);
            }
        }
        finally
        {
            frame = previousFrame;
            if (hadStairFinish) finishes["stair"] = previousStairFinish;
            else finishes.Remove("stair");
        }
        // Directions on signs use actual destination numbers. Suppress the blockout's floor diagrams.
        foreach (var text in newExtension.GetComponentsInChildren<TextMesh>())
            text.gameObject.SetActive(false);
        foreach (var text in galleryStairs.GetComponentsInChildren<TextMesh>())
            text.gameObject.SetActive(false);
        AddSportsStairGuards(galleryStairs, railMaterial, false);
        BuildGalleryFurnishings(gallery, railMaterial, benchWood);
        InstallGalleryLibrary(gallery);
        return gallery;
    }

    static Material LoadSportsAccessMaterial(string path)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) throw new InvalidOperationException("Missing sports access material: " + path);
        return material;
    }

    static void BuildSportsFloorPlate(Transform level, bool openAtrium, Material slabMaterial)
    {
        var slab = Group(level, "00_Slab");
        SportsSlabPanel(slab, -18, 18, -13, -3.2f, slabMaterial);
        SportsSlabPanel(slab, -18, 18, 3.2f, 13, slabMaterial);
        SportsSlabPanel(slab, -14.2f, -3.9f, -3.2f, 3.2f, slabMaterial);
        // Keep the lift shaft open through both new levels. A landing bridges to the shaft at Z=3.2.
        SportsSlabPanel(slab, 3.9f, 9.5f, -3.2f, 3.2f, slabMaterial);
        SportsSlabPanel(slab, 12.3f, 14.2f, -3.2f, 3.2f, slabMaterial);
        SportsSlabPanel(slab, 9.5f, 12.3f, -3.2f, .1f, slabMaterial);
        SportsSlabPanel(slab, -18, -17.6f, -3.2f, 3.2f, slabMaterial);
        SportsSlabPanel(slab, 17.6f, 18, -3.2f, 3.2f, slabMaterial);
        SportsSlabPanel(slab, -17.6f, -14.2f, -3.2f, -1.65f, slabMaterial);
        SportsSlabPanel(slab, 14.2f, 17.6f, -3.2f, -1.65f, slabMaterial);
        if (!openAtrium)
            SportsSlabPanel(slab, -3.9f, 3.9f, -3.2f, 3.2f, slabMaterial, AtriumCeilingPanelName);
    }

    static void SportsSlabPanel(Transform slab, float xMin, float xMax, float zMin, float zMax, Material material,
        string name = "Slab panel")
    {
        Box(slab, name, new Vector3((xMin + xMax) * .5f, -SportsSlabThickness * .5f, (zMin + zMax) * .5f),
            new Vector3(xMax - xMin, SportsSlabThickness, zMax - zMin), material);
    }

    static void BuildSportsArrivalGuards(Transform floor5, Material railMaterial)
    {
        AddSportsStairGuards(Group(floor5, "04_Stairs__arrival_only"), railMaterial, true);
    }

    static void AddSportsStairGuards(Transform parent, Material railMaterial, bool terminal)
    {
        foreach (int side in new[] { -1, 1 })
        {
            var guard = Group(parent, side < 0 ? "West opening guards" : "East opening guards");
            Box(guard, "Inner stair opening guard", new Vector3(side * 14.25f, .55f, .775f),
                new Vector3(.10f, 1.1f, 4.85f), railMaterial);
            Box(guard, "North stair opening guard", new Vector3(side * 15.9f, .55f, 3.25f),
                new Vector3(3.4f, 1.1f, .10f), railMaterial);
            if (terminal)
                Box(guard, "Unused south half guard", new Vector3(side * 16.8f, .55f, -1.70f),
                    new Vector3(1.5f, 1.1f, .10f), railMaterial);
        }
    }

    static void CloseGalleryStairEnds(Transform gallery, Transform sportsFloor)
    {
        var stairs = gallery.Find("04_Stairs__to_floor_05");
        var wallMaterial = LoadSportsAccessMaterial("Assets/JEI_Blockout/Materials/Structure_Walls.mat");
        float height = sportsFloor.position.y - gallery.position.y - .30f;
        const float innerEdge = 14.20f;
        const float glazingFace = 17.83f;

        foreach (int side in new[] { -1, 1 })
        {
            var guards = stairs.Find(side < 0 ? "West opening guards" : "East opening guards");
            var wall = guards.Find("North stairwell wall");
            if (!wall) wall = guards.Find("North stair opening guard");
            if (!wall) throw new InvalidOperationException("The north gallery stair guard is missing.");

            // The north is a closed end. Keep the wall beyond the turning landing and join both side faces.
            wall.name = "North stairwell wall";
            wall.localPosition = new Vector3(side * (innerEdge + glazingFace) * .5f, height * .5f, 3.25f);
            wall.localScale = new Vector3(glazingFace - innerEdge, height, .10f);
            wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        }
    }

    static void BuildGalleryFacade(Transform floor, Material stone, Material metal, Material glass)
    {
        var envelope = Group(floor, "01_Glazed_Perimeter");
        foreach (float z in new[] { -12.85f, 12.85f })
            BuildGalleryFacadeFace(envelope, new Vector3(0, 0, z), 0, 36, 5, stone, metal, glass);
        foreach (float x in new[] { -17.85f, 17.85f })
            BuildGalleryFacadeFace(envelope, new Vector3(x, 0, 0), 90, 25.4f, 4, stone, metal, glass);
    }

    static void BuildGalleryFacadeFace(Transform parent, Vector3 position, float yaw, float length, int bays,
        Material stone, Material metal, Material glass)
    {
        var face = Group(parent, "Window wall");
        face.localPosition = position;
        face.localRotation = Quaternion.Euler(0, yaw, 0);
        Box(face, "Limestone sill wall", new Vector3(0, .46f, 0), new Vector3(length, .92f, .30f), stone);
        Box(face, "Limestone header", new Vector3(0, 3.07f, 0), new Vector3(length, .46f, .30f), stone);
        float bayWidth = length / bays;
        for (int i = 0; i <= bays; i++)
        {
            float x = -length * .5f + bayWidth * i;
            float width = .32f;
            if (i == 0 || i == bays)
            {
                width *= .5f;
                x += i == 0 ? width * .5f : -width * .5f;
            }
            Box(face, "Limestone pier", new Vector3(x, 1.88f, 0), new Vector3(width, 1.92f, .30f), stone);
        }
        for (int i = 0; i < bays; i++)
        {
            float x = (i + .5f) * bayWidth - length * .5f;
            float windowWidth = bayWidth - .32f;
            AddOpeningGlass(face, "Glazed bay", new Vector3(x, 1.88f, 0), new Vector3(windowWidth, 1.92f, .04f), glass, true);
            foreach (float y in new[] { .95f, 2.81f })
                D(face, "Aluminium window rail", new Vector3(x, y, 0), new Vector3(windowWidth, .07f, .18f), metal);
            foreach (float offset in new[] { -windowWidth * .5f + .03f, 0, windowWidth * .5f - .03f })
                D(face, "Aluminium mullion", new Vector3(x + offset, 1.88f, 0), new Vector3(.06f, 1.92f, .18f), metal);
        }
    }

    public static bool InstallGalleryAtriumGlazing(Transform gallery, Transform sportsFloor)
    {
        if (!gallery || !sportsFloor) throw new InvalidOperationException("The gallery and sports floor are required.");
        var slab = sportsFloor.Find("00_Slab/" + AtriumCeilingPanelName);
        if (!slab) throw new InvalidOperationException("The slab above the gallery atrium is missing.");

        float height = gallery.InverseTransformPoint(slab.TransformPoint(Vector3.down * .5f)).y;
        var metal = LoadSportsAccessMaterial(GalleryRailMaterialPath);
        var sourceGlass = LoadSportsAccessMaterial(GalleryGlassSourcePath);
        bool changed = BuildGalleryAtriumGlazing(gallery, height, metal, sourceGlass);
        if (changed) EditorSceneManager.MarkSceneDirty(gallery.gameObject.scene);
        return changed;
    }

    static Material LoadGalleryAtriumGlass(Material source)
    {
        const string name = "Gallery_Clear_Atrium_Glass";
        string path = FitoutAssets + "/" + name + ".mat";
        var glass = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (glass) return glass;

        glass = new Material(source) { name = name };
        var tint = new Color(.83f, .91f, .93f, .075f);
        glass.SetColor("_BaseColor", tint);
        glass.SetColor("_Color", tint);
        glass.SetFloat("_Cull", (float)CullMode.Back);
        AssetDatabase.CreateAsset(glass, path);
        return glass;
    }

    static bool BuildGalleryAtriumGlazing(Transform gallery, float height, Material metal, Material sourceGlass)
    {
        if (height <= 1.1f) throw new InvalidOperationException("The gallery soffit must be above the handrail.");
        var current = gallery.Find(GalleryBalustradeName);
        if (HasFullHeightGlazing(current, height)) return false;

        var glass = LoadGalleryAtriumGlass(sourceGlass);
        var replacement = Group(gallery, "New atrium glazing");
        try
        {
            foreach (float z in new[] { -3.24f, 3.24f })
                BuildGalleryGlazingSide(replacement, new Vector3(0, 0, z), 0f, 7.88f, height, metal, glass, true);
            foreach (float x in new[] { -3.94f, 3.94f })
                BuildGalleryGlazingSide(replacement, new Vector3(x, 0, 0), 90f, 6.48f, height, metal, glass, false);
        }
        catch
        {
            Object.DestroyImmediate(replacement.gameObject);
            throw;
        }

        if (!current) replacement.name = GalleryBalustradeName;
        else
        {
            while (current.childCount > 0) Object.DestroyImmediate(current.GetChild(0).gameObject);
            while (replacement.childCount > 0) replacement.GetChild(0).SetParent(current, false);
            Object.DestroyImmediate(replacement.gameObject);
        }
        return true;
    }

    static bool HasFullHeightGlazing(Transform balustrade, float height)
    {
        if (!balustrade || balustrade.childCount != 4) return false;
        foreach (Transform side in balustrade)
        {
            if (side.name != GalleryGlassSideName || !side.TryGetComponent<BoxCollider>(out var barrier) ||
                Mathf.Abs(barrier.size.y - height) > .001f)
                return false;
        }
        return true;
    }

    static void BuildGalleryGlazingSide(Transform parent, Vector3 center, float yaw, float length, float height,
        Material metal, Material glass, bool addCornerPosts)
    {
        const int bays = 4;
        const float frameWidth = .045f;
        const float frameDepth = .064f;
        const float sillHeight = .045f;
        const float headHeight = .032f;

        var edge = Group(parent, GalleryGlassSideName);
        edge.localPosition = center;
        edge.localRotation = Quaternion.Euler(0, yaw, 0);

        float railLength = length + (addCornerPosts ? frameWidth : -frameDepth);
        D(edge, "Recessed sill channel", new Vector3(0, sillHeight / 2, 0),
            new Vector3(railLength, sillHeight, frameDepth), metal);
        D(edge, "Soffit channel", new Vector3(0, height - headHeight / 2, 0),
            new Vector3(railLength, headHeight, frameDepth), metal);
        D(edge, "Slim handrail", new Vector3(0, 1.09f, 0),
            new Vector3(railLength, .034f, .070f), metal);

        for (int joint = 0; joint <= bays; joint++)
        {
            bool corner = joint == 0 || joint == bays;
            if (corner && !addCornerPosts) continue;
            float x = length * ((float)joint / bays - .5f);
            D(edge, corner ? "Corner jamb" : "Slim mullion", new Vector3(x, height / 2, 0),
                new Vector3(frameWidth, height, frameDepth), metal);
        }

        float paneHeight = height - sillHeight - headHeight + .008f;
        float paneCenter = (height + sillHeight - headHeight) / 2;
        for (int bay = 0; bay < bays; bay++)
        {
            float x = length * ((bay + .5f) / bays - .5f);
            AddOpeningGlass(edge, "Full-height clear glazing", new Vector3(x, paneCenter, 0),
                new Vector3(length / bays - .040f, paneHeight, .018f), glass, false);
        }

        // One continuous barrier keeps the joints between panes and frame members closed to the player.
        Solid(edge, new Vector3(0, height * .5f, 0), new Vector3(length, height, .04f));
    }

    static void BuildGalleryFurnishings(Transform floor, Material metal, Material benchWood)
    {
        var fitout = Group(floor, "03_Gallery_Seating_and_Wayfinding");
        var signMaterial = FitMat("Gallery_Deep_Green", new Color(.09f, .27f, .25f));
        var diffuser = FitMat("Gallery_Warm_Light_Diffuser", new Color(.98f, .95f, .84f));
        foreach (int side in new[] { -1, 1 })
        {
            D(fitout, "Gallery directional sign", new Vector3(side * 13.3f, 2.75f, -2.8f),
                new Vector3(2.7f, .36f, .07f), signMaterial);
            foreach (float offset in new[] { -.93f, .93f })
                D(fitout, "Wayfinding suspension", new Vector3(side * 13.3f + offset, 3.1f, -2.8f),
                    new Vector3(.032f, .34f, .032f), metal);
            Label(fitout, "04  ATRIUM GALLERY   |   05  SPORTS", new Vector3(side * 13.3f, 2.75f, -2.846f),
                .05f, Color.white);

            foreach (float z in new[] { -9.7f, 9.7f })
            {
                float x = side * 7f;
                D(fitout, "Oak gallery bench seat", new Vector3(x, .45f, z),
                    new Vector3(2.5f, .14f, .57f), benchWood, true);
                foreach (float offset in new[] { -.98f, .98f })
                    D(fitout, "Gallery bench base", new Vector3(x + offset, .19f, z),
                        new Vector3(.14f, .38f, .48f), metal, true);
            }
        }

        foreach (float z in new[] { -8.5f, 8.5f })
        foreach (float x in new[] { -12f, 0f, 12f })
        {
            var lamp = Group(fitout, "Gallery ceiling light");
            lamp.localPosition = new Vector3(x, 3.20f, z);
            D(lamp, "Fixture housing", Vector3.up * .07f, new Vector3(2.08f, .07f, .25f), metal);
            D(lamp, "Light diffuser", Vector3.up * .022f, new Vector3(1.97f, .04f, .22f), diffuser);
            var light = lamp.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 2.4f;
            light.color = new Color(1f, .95f, .87f);
            light.shadows = LightShadows.None;
        }
    }

    static void VerifySportsAccess(Transform floor4, Transform floor5)
    {
        using var openDoors = new OpenRoomDoors();
        var probe = new GameObject("Temporary upper stair walking probe");
        var capsule = probe.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.center = Vector3.up * .9f;
        capsule.radius = .3f;
        capsule.stepOffset = .25f;
        capsule.skinWidth = .035f;
        try
        {
            var floors = new[] { floor4.position.y - 3.6f, floor4.position.y, floor5.position.y };
            foreach (int side in new[] { -1, 1 })
            {
                float outer = side * 16.8f, inner = side * 15f, passage = side * 13.3f;
                PlaceProbe(capsule, new Vector3(passage, floors[0] + .05f, -4.6f));
                for (int i = 0; i < floors.Length - 1; i++)
                {
                    WalkTo(capsule, new Vector2(passage, -2.4f));
                    WalkTo(capsule, new Vector2(outer, -2.4f));
                    WalkTo(capsule, new Vector2(outer, 2.4f));
                    AssertHeight(capsule, floors[i] + 1.8f);
                    WalkTo(capsule, new Vector2(inner, 2.4f));
                    WalkTo(capsule, new Vector2(inner, -2.4f));
                    AssertHeight(capsule, floors[i + 1]);
                    WalkTo(capsule, new Vector2(passage, -2.4f));
                    WalkTo(capsule, new Vector2(passage, -4.6f));
                    AssertHeight(capsule, floors[i + 1]);
                }
                for (int i = floors.Length - 2; i >= 0; i--)
                {
                    WalkTo(capsule, new Vector2(passage, -2.4f));
                    WalkTo(capsule, new Vector2(inner, -2.4f));
                    WalkTo(capsule, new Vector2(inner, 2.4f));
                    AssertHeight(capsule, floors[i] + 1.8f);
                    WalkTo(capsule, new Vector2(outer, 2.4f));
                    WalkTo(capsule, new Vector2(outer, -2.4f));
                    WalkTo(capsule, new Vector2(passage, -2.4f));
                    AssertHeight(capsule, floors[i]);
                    WalkTo(capsule, new Vector2(passage, -4.6f));
                }
            }
            if (Physics.Raycast(new Vector3(0, floor4.position.y + .2f, 0), Vector3.down, 1f))
                throw new InvalidOperationException("The fourth-floor plate covers the courtyard opening.");
            if (!Physics.Raycast(new Vector3(0, floor5.position.y + .2f, 0), Vector3.down, out var hit, 1f) ||
                !hit.transform.IsChildOf(floor5))
                throw new InvalidOperationException("The fifth-floor plate does not cover the courtyard opening.");
        }
        finally { Object.DestroyImmediate(probe); }
    }
}
