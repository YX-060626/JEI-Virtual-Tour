using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string TourLiftRoot = "Elevator__tour_stops";
    const string LegacyTourLiftRoot = "Elevator__three_stops";
    const string TourLiftLanding = "10_Elevator_Landing";

    static void BuildTourLift(JEIStructureView view)
    {
        var first = view.floors[0];
        var buildingRoot = first.parent;
        RemoveGeneratedLift(buildingRoot.Find(TourLiftRoot));
        RemoveGeneratedLift(buildingRoot.Find(LegacyTourLiftRoot));
        for (int i = 0; i < view.floors.Length; i++) RemoveGeneratedLift(view.floors[i].Find(TourLiftLanding));

        var reservedDoor = first.Find("08_Service_Areas/C06__Lift_entrance_reserve");
        if (reservedDoor) reservedDoor.gameObject.SetActive(false);
        for (int i = 1; i < Mathf.Min(3, view.floors.Length); i++) EnsureLiftWallOpening(view.floors[i]);

        var shell = FitMat("Lift_Warm_Sage", new Color(.42f, .5f, .46f));
        var door = FitMat("Lift_Satin_Green", new Color(.25f, .35f, .32f));
        var trim = FitMat("Lift_Graphite", new Color(.13f, .19f, .17f));
        var panel = FitMat("Lift_Inset_Oak", new Color(.67f, .47f, .29f));
        var steel = FitMat("Lift_Brushed_Silver", new Color(.63f, .67f, .64f));
        var floorFinish = FitMat("Lift_Stone_Floor", new Color(.42f, .42f, .39f));
        var ready = LiftLamp("Lift_Ready_Lamp", new Color(.45f, .9f, .68f), .7f);
        var standby = FitMat("Lift_Standby_Lamp", new Color(.12f, .28f, .20f));
        var whiteLamp = LiftLamp("Lift_Ceiling_Lamp", new Color(1f, .93f, .81f), .75f);

        var upperShaft = view.floors.Length > 3
            ? FitMat("Lift_Upper_Shaft", new Color(.72f, .74f, .70f))
            : null;
        var stops = new JEILiftController.Landing[view.floors.Length];
        for (int i = 0; i < stops.Length; i++)
        {
            var level = view.floors[i];
            var landing = Group(level, TourLiftLanding);
            landing.gameObject.AddComponent<JEIGeneratedObject>();
            if (i >= 3)
            {
                float height = i + 1 < stops.Length
                    ? level.InverseTransformPoint(view.floors[i + 1].position).y
                    : 3.3f;
                bool rooftop = i == 5 && i == stops.Length - 1;
                BuildUpperLiftShaft(landing, height, upperShaft, rooftop);
            }
            stops[i] = BuildLiftLanding(landing, level, i + 1, door, trim, panel, steel, standby);
        }

        var root = Group(buildingRoot, TourLiftRoot);
        root.gameObject.AddComponent<JEIGeneratedObject>();
        var cabin = Group(root, "Moving elevator cabin");
        BuildLiftCabin(cabin, stops.Length, shell, trim, panel, steel, floorFinish, whiteLamp);
        var left = LiftDoor(cabin, "Cabin left sliding door", JEILiftController.CabinFrontZ - .027f, door, trim);
        var right = LiftDoor(cabin, "Cabin right sliding door", JEILiftController.CabinFrontZ - .027f, door, trim);
        var controller = root.gameObject.AddComponent<JEILiftController>();
        controller.Configure(view, cabin, left, right, stops, ready, standby);
        Physics.SyncTransforms();
    }

    static void BuildUpperLiftShaft(Transform landing, float height, Material finish, bool rooftop)
    {
        const float shaftX = JEILiftController.ShaftX;
        const float frontZ = JEILiftController.LandingZ;
        const float rearZ = .10f;
        const float shaftWidth = 2.80f;
        const float wallThickness = .16f;
        const float doorWidth = JEILiftController.DoorWidth;
        const float doorHeight = JEILiftController.DoorHeight;

        D(landing, "Shaft rear wall", new Vector3(shaftX, height * .5f, rearZ),
            new Vector3(shaftWidth + wallThickness, height, wallThickness), finish, true);
        foreach (int side in new[] { -1, 1 })
        {
            D(landing, "Shaft side wall", new Vector3(shaftX + side * shaftWidth * .5f, height * .5f, (rearZ + frontZ) * .5f),
                new Vector3(wallThickness, height, frontZ - rearZ), finish, true);
            D(landing, "Landing portal wall", new Vector3(shaftX + side * (shaftWidth + wallThickness + doorWidth) * .25f, height * .5f, frontZ),
                new Vector3((shaftWidth + wallThickness - doorWidth) * .5f, height, .20f), finish, true);
        }

        D(landing, "Landing portal lintel", new Vector3(shaftX, (height + doorHeight) * .5f, frontZ),
            new Vector3(doorWidth, height - doorHeight, .20f), finish, true);

        if (rooftop)
            D(landing, "Weather cap over elevator", new Vector3(shaftX, height + .10f, (rearZ + frontZ) * .5f),
                new Vector3(shaftWidth + .38f, .20f, frontZ - rearZ + .38f), finish, true);
    }

    static void RemoveGeneratedLift(Transform item)
    {
        if (!item) return;
        if (!item.GetComponent<JEIGeneratedObject>())
            throw new InvalidOperationException("An existing non-generated object uses the elevator group's name: " + item.name);
        Object.DestroyImmediate(item.gameObject);
    }

    static void EnsureLiftWallOpening(Transform floor)
    {
        var walls = floor.Find("02_Walls__0.30m_external__0.20m_internal");
        if (!walls) throw new InvalidOperationException("Cannot locate the elevator landing wall.");

        float leftEdge = JEILiftController.ShaftX - JEILiftController.DoorWidth * .5f;
        float rightEdge = JEILiftController.ShaftX + JEILiftController.DoorWidth * .5f;
        foreach (Transform partition in walls)
        {
            if (partition.name != "Partition_H_3.3") continue;
            foreach (Transform segment in partition)
            {
                if (segment.name != "Wall" || segment.localScale.y < JEILiftController.DoorHeight) continue;
                float start = segment.localPosition.x - segment.localScale.x * .5f;
                float end = segment.localPosition.x + segment.localScale.x * .5f;
                if (start > leftEdge || end < rightEdge) continue;

                // Retain the wall material, collider and top trim. Split only the solid bay in front of the shaft.
                var right = Object.Instantiate(segment.gameObject, partition, false).transform;
                right.name = "Wall";
                var header = Object.Instantiate(segment.gameObject, partition, false).transform;
                header.name = "Wall";
                ResizeLiftWallSegment(segment, start, leftEdge);
                ResizeLiftWallSegment(right, rightEdge, end);
                ResizeLiftWallSegment(header, leftEdge, rightEdge);
                Vector3 headerSize = header.localScale;
                headerSize.y -= JEILiftController.DoorHeight;
                header.localScale = headerSize;
                Vector3 headerPosition = header.localPosition;
                headerPosition.y += JEILiftController.DoorHeight * .5f;
                header.localPosition = headerPosition;
                return;
            }
        }
        // A shell regenerated from the updated plans already has the same opening.
    }

    static void ResizeLiftWallSegment(Transform segment, float start, float end)
    {
        Vector3 position = segment.localPosition;
        position.x = (start + end) * .5f;
        segment.localPosition = position;
        Vector3 size = segment.localScale;
        size.x = end - start;
        segment.localScale = size;
    }

    static Material LiftLamp(string name, Color color, float glow)
    {
        var material = FitMat(name, color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * glow);
        EditorUtility.SetDirty(material);
        return material;
    }

    static JEILiftController.Landing BuildLiftLanding(Transform root, Transform floor, int number, Material door,
        Material trim, Material wood, Material steel, Material lamp)
    {
        const float x = JEILiftController.ShaftX;
        const float z = JEILiftController.LandingZ;
        var left = LiftDoor(root, "Landing left sliding door", z + .014f, door, trim);
        var right = LiftDoor(root, "Landing right sliding door", z + .014f, door, trim);
        foreach (int side in new[] { -1, 1 })
            D(root, "Elevator casing jamb", new Vector3(x + side * .573f, 1.2f, z + .105f), new Vector3(.046f, 2.4f, .052f), trim);
        D(root, "Elevator casing header", new Vector3(x, 2.435f, z + .105f), new Vector3(1.19f, .09f, .052f), trim);
        D(root, "Grooved threshold sill", new Vector3(x, .008f, z), new Vector3(1.10f, .016f, .2f), steel);
        for (int i = 0; i < 3; i++)
            D(root, "Threshold groove", new Vector3(x, .017f, z - .052f + i * .052f), new Vector3(1.09f, .003f, .008f), trim);

        D(root, "Floor indicator panel", new Vector3(x, 2.61f, z + .115f), new Vector3(.27f, .16f, .036f), trim);
        var digit = LiftDigit(root, number, new Vector3(x - .023f, 2.61f, z + .137f), .095f, steel, false);
        digit.name = "Landing floor number";
        var indicator = D(root, "Arrival indicator", new Vector3(x + .075f, 2.61f, z + .138f), new Vector3(.037f, .024f, .012f), lamp);
        D(root, "Oak call station", new Vector3(x - .77f, 1.11f, z + .123f), new Vector3(.145f, .22f, .048f), wood);
        D(root, "Call button trim", new Vector3(x - .77f, 1.11f, z + .15f), new Vector3(.081f, .084f, .025f), trim);
        var button = D(root, "Call button lamp", new Vector3(x - .77f, 1.11f, z + .164f), new Vector3(.043f, .043f, .012f), lamp);

        return new JEILiftController.Landing
        {
            floor = floor, leftDoor = left, rightDoor = right,
            button = button.GetComponent<MeshRenderer>(), indicator = indicator.GetComponent<MeshRenderer>()
        };
    }

    static Transform LiftDoor(Transform root, string name, float z, Material door, Material trim)
    {
        var leaf = Group(root, name);
        leaf.localPosition = new Vector3(JEILiftController.ShaftX, 0f, z);
        D(leaf, "Door panel", new Vector3(0f, 1.207f, 0f), new Vector3(.555f, 2.383f, .045f), door, true);
        D(leaf, "Recessed door edge", new Vector3(0f, 1.207f, .024f), new Vector3(.48f, 2.27f, .004f), trim);
        D(leaf, "Satin door panel inset", new Vector3(0f, 1.207f, .026f), new Vector3(.446f, 2.235f, .004f), door);
        return leaf;
    }

    static void BuildLiftCabin(Transform cabin, int floorCount, Material shell, Material trim, Material wood, Material steel,
        Material floor, Material lamp)
    {
        const float x = JEILiftController.ShaftX;
        const float centerZ = 1.72f;
        D(cabin, "Cabin floor", new Vector3(x, -.039f, centerZ), new Vector3(2.53f, .10f, 2.944f), floor, true);
        D(cabin, "Cabin ceiling", new Vector3(x, 2.454f, centerZ), new Vector3(2.53f, .11f, 2.944f), shell, true);
        D(cabin, "Rear cabin wall", new Vector3(x, 1.24f, .277f), new Vector3(2.53f, 2.46f, .075f), shell, true);
        D(cabin, "Rear wood panel", new Vector3(x, 1.27f, .325f), new Vector3(2.42f, 1.86f, .035f), wood);
        D(cabin, "Rear handrail", new Vector3(x, 1.07f, .408f), new Vector3(2.12f, .058f, .065f), steel);
        foreach (int side in new[] { -1, 1 })
        {
            D(cabin, "Sage cabin side wall", new Vector3(x + side * 1.235f, 1.24f, centerZ), new Vector3(.064f, 2.46f, 2.944f), shell, true);
            D(cabin, "Cabin side handrail", new Vector3(x + side * 1.161f, 1.07f, 1.6f), new Vector3(.061f, .058f, 2.16f), steel);
            D(cabin, "Cabin skirting", new Vector3(x + side * 1.194f, .09f, centerZ), new Vector3(.018f, .14f, 2.87f), trim);
            D(cabin, "Entrance return", new Vector3(x + side * .902f, 1.207f, JEILiftController.CabinFrontZ - .024f),
                new Vector3(.706f, 2.383f, .057f), trim, true);
        }
        var station = Group(cabin, "Floor selector");
        station.localPosition = new Vector3(x - .956f, 1.3f, 3.098f);
        const float buttonSpacing = .132f;
        D(station, "Selector panel", Vector3.zero, new Vector3(.20f, .084f + buttonSpacing * floorCount, .034f), steel);
        for (int number = 1; number <= floorCount; number++)
        {
            float y = (number - (floorCount + 1) * .5f) * buttonSpacing;
            D(station, "Floor " + number + " button", new Vector3(0f, y, -.025f), new Vector3(.081f, .088f, .024f), trim);
            LiftDigit(station, number, new Vector3(0f, y, -.041f), .063f, lamp, true);
        }

        D(cabin, "Warm overhead panel", new Vector3(x, 2.387f, 1.65f), new Vector3(1.56f, .026f, .68f), lamp);
        var light = Group(cabin, "Cabin downlight");
        light.localPosition = new Vector3(x, 2.36f, 1.68f);
        light.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var downlight = light.gameObject.AddComponent<Light>();
        downlight.type = LightType.Spot;
        downlight.intensity = 5.5f;
        downlight.color = new Color(1f, .94f, .82f);
        downlight.range = 3.5f;
        downlight.spotAngle = 140f;
        downlight.innerSpotAngle = 106f;
    }

    static Transform LiftDigit(Transform parent, int number, Vector3 point, float height, Material material, bool fromShaft)
    {
        string[] digits = { "bc", "abged", "abgcd", "fbgc", "afgcd", "afgecd" };
        var glyph = Group(parent, "Embossed floor " + number);
        glyph.localPosition = point;
        if (!fromShaft) glyph.localRotation = Quaternion.Euler(0, 180f, 0);
        float width = height * .52f;
        float stroke = height * .12f;
        foreach (char segment in digits[number - 1])
        {
            bool horizontal = segment == 'a' || segment == 'd' || segment == 'g';
            float x = horizontal ? 0f : segment == 'e' || segment == 'f' ? -width * .5f : width * .5f;
            float y = segment == 'a' ? height * .5f : segment == 'd' ? -height * .5f :
                segment == 'g' ? 0f : segment == 'b' || segment == 'f' ? height * .25f : -height * .25f;
            D(glyph, "Digit stroke", new Vector3(x, y, 0f),
                horizontal ? new Vector3(width, stroke, .006f) : new Vector3(stroke, height * .5f, .006f), material);
        }
        return glyph;
    }

    static void CaptureTourLift(JEIStructureView view, string path)
    {
        var lift = FindTourLift(view);
        int floor = lift.CurrentFloor;
        bool open = lift.IsOpen;
        var cameraObject = new GameObject("Temporary elevator preview camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camera.nearClipPlane = .04f;
        camera.farClipPlane = 60f;
        camera.fieldOfView = 57f;
        try
        {
            lift.SetEditorLanding(1, true);
            camera.transform.position = new Vector3(10.38f, 1.7f, 5.59f);
            camera.transform.LookAt(new Vector3(10.85f, 1.21f, 3.24f));
            SaveRender(camera, path, 1500, 1000);
        }
        finally
        {
            lift.SetEditorLanding(floor, open);
            Object.DestroyImmediate(cameraObject);
        }
    }

    static JEILiftController FindTourLift(JEIStructureView view)
    {
        var buildingRoot = view.floors[0].parent;
        var root = buildingRoot.Find(TourLiftRoot) ?? buildingRoot.Find(LegacyTourLiftRoot);
        var lift = root ? root.GetComponent<JEILiftController>() : null;
        if (!lift) throw new InvalidOperationException("Missing tour elevator.");
        return lift;
    }
}
