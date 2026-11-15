using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string RoomOpeningsRootName = "10_Room_Openings";
    const float DoorLeafBottom = .049f;
    const float DoorLeafHeight = 2.267f;
    const float DoorLeafThickness = .044f;
    const float DoorSwingDegrees = 95;

    static void BuildRoomOpenings(JEIStructureView view)
    {
        var paint = new OpeningMaterials();
        for (int level = 1; level <= view.floors.Length; level++)
        {
            var floor = view.floors[level - 1];
            var previous = floor.Find(RoomOpeningsRootName);
            if (previous)
            {
                if (!previous.GetComponent<JEIGeneratedObject>())
                    throw new InvalidOperationException("Existing opening group was not generated: " + floor.name);
                Object.DestroyImmediate(previous.gameObject);
            }

            var openings = Group(floor, RoomOpeningsRootName);
            openings.gameObject.AddComponent<JEIGeneratedObject>();
            var plan = UpperInteriorPlan(level);
            foreach (var wall in plan.walls)
            {
                foreach (var opening in wall.openings)
                {
                    float thickness = wall.external ? plan.externalWall : plan.internalWall;
                    if (opening.kind == "window")
                    {
                        BuildCampusWindow(openings, wall, opening, thickness, paint);
                        continue;
                    }

                    if (opening.kind != "door" || !TryGetRoomAtDoor(plan, wall, opening, out var room, out float inward))
                        continue;

                    BuildCampusDoor(openings, wall, opening, room, inward, thickness, paint);
                    if (wall.external) BuildEntranceThreshold(openings, wall, opening, paint.Frame);
                }
            }
        }
        Physics.SyncTransforms();
    }

    static void BuildEntranceThreshold(Transform parent, Wall wall, Opening opening, Material material)
    {
        // Smooth the existing step from the exterior site up to the interior slab.
        Vector3 low = new Vector3(opening.at, -.30f, wall.at - 1.49f);
        Vector3 high = new Vector3(opening.at, 0f, wall.at - .12f);
        Vector3 slope = high - low;
        Quaternion tilt = Quaternion.LookRotation(slope);
        const float thickness = .09f;
        var ramp = Box(parent, "Main entrance sloped threshold", (low + high) / 2 - tilt * Vector3.up * (thickness / 2),
            new Vector3(opening.width, thickness, slope.magnitude), material);
        ramp.transform.localRotation = tilt;
    }

    static bool TryGetRoomAtDoor(Plan plan, Wall wall, Opening opening, out Room room, out float inward)
    {
        room = null;
        inward = 0;
        // Side exits and stair doors preserve the clear stair/corridor system.
        if (wall.axis != "H") return false;
        foreach (var candidate in plan.rooms)
        {
            if (candidate.kind == "stair" || candidate.kind == "courtyard" || candidate.name == "LIFT")
                continue;
            if (opening.at <= candidate.x0 || opening.at >= candidate.x1) continue;
            if (Mathf.Abs(candidate.z0 - wall.at) < .01f)
                inward = 1;
            else if (Mathf.Abs(candidate.z1 - wall.at) < .01f)
                inward = -1;
            else
                continue;
            room = candidate;
            return true;
        }
        return false;
    }

    sealed class OpeningMaterials
    {
        public readonly Material Timber;
        public readonly Material Stile;
        public readonly Material Frame;
        public readonly Material Metal;
        public readonly Material Frosted;
        public readonly Material Glass;
        public readonly Material SafetyBand;

        public OpeningMaterials()
        {
            Timber = FitMat("Room_Door_Warm_Oak", new Color(.58f, .42f, .25f));
            Stile = FitMat("Room_Door_Oak_Trim", new Color(.37f, .25f, .15f));
            Frame = FitMat("Room_Window_Teal_Frame", new Color(.135f, .214f, .21f));
            Metal = FitMat("Room_Door_Satin_Brass", new Color(.68f, .55f, .33f));
            Frosted = FitMat("Room_Privacy_Glass", new Color(.69f, .77f, .76f));
            Frosted.SetFloat("_Smoothness", .58f);
            SafetyBand = FitMat("Room_Glazing_Safety_Band", new Color(.79f, .87f, .85f));
            Glass = FitMat("Room_Clear_Glazing", new Color(.63f, .83f, .88f, .14f));
            Glass.color = new Color(.63f, .83f, .88f, .14f);
            Glass.SetOverrideTag("RenderType", "Transparent");
            Glass.SetFloat("_Surface", 1);
            Glass.SetFloat("_Blend", 0);
            Glass.SetFloat("_BlendModePreserveSpecular", 0);
            Glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            Glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            Glass.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            Glass.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            Glass.SetFloat("_ZWrite", 0);
            Glass.SetFloat("_Smoothness", .70f);
            Glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            Glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            Glass.SetShaderPassEnabled("DepthOnly", false);
            Glass.SetShaderPassEnabled("ShadowCaster", false);
            Glass.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(Glass);
            EditorUtility.SetDirty(Frosted);
        }
    }

    static void BuildCampusWindow(Transform parent, Wall wall, Opening opening, float thickness, OpeningMaterials paint)
    {
        var window = Group(parent, $"Fixed window {wall.axis} {wall.at:0.##} / {opening.at:0.##}");
        window.localPosition = wall.axis == "H" ? new Vector3(opening.at, 0, wall.at) : new Vector3(wall.at, 0, opening.at);
        window.localRotation = Quaternion.Euler(0, wall.axis == "H" ? 0 : 90, 0);
        const float mid = 1.85f;
        float half = opening.width / 2;
        foreach (int side in new[] { -1, 1 })
        {
            FramePart(window, "Inset jamb", side * (half - .025f), mid, 0, .05f, 1.89f, thickness, paint.Frame);
            FramePart(window, "Head and sill", 0, mid + side * .919f, 0, opening.width, .062f, thickness, paint.Frame);
        }

        int divisions = Mathf.CeilToInt(opening.width / 1.55f);
        for (int i = 1; i < divisions; i++)
        {
            float x = -half + opening.width * i / divisions;
            FramePart(window, "Slim mullion", x, mid, 0, .045f, 1.85f, .108f, paint.Frame);
        }
        FramePart(window, "Glazed transom divider", 0, 2.35f, 0, opening.width - .08f, .045f, .108f, paint.Frame);
        FramePart(window, "Sill reveal", 0, .928f, 0, opening.width - .10f, .035f, thickness + .10f, paint.Stile);
        AddOpeningGlass(window, "Clear fixed safety glazing", new Vector3(0, mid, 0),
            new Vector3(opening.width, 1.90f, .026f), paint.Glass, true);
        FramePart(window, "Slim glass visibility band", 0, 1.42f, 0, opening.width - .06f, .026f, .032f, paint.SafetyBand);
    }

    static void BuildCampusDoor(Transform parent, Wall wall, Opening opening, Room room,
        float inward, float thickness, OpeningMaterials paint)
    {
        string role = wall.external ? "Main entrance" : room.name;
        var portal = Group(parent, $"{room.id} {role} door");
        portal.localPosition = new Vector3(opening.at, 0, wall.at);
        portal.localRotation = Quaternion.Euler(0, inward > 0 ? 0 : 180, 0);
        float halfWidth = opening.width / 2;
        foreach (int side in new[] { -1, 1 })
            FramePart(portal, "Door jamb lining", side * (halfWidth - .014f), 1.174f, 0, .028f, 2.348f, thickness + .05f, paint.Stile);
        FramePart(portal, "Door head lining", 0, 2.345f, 0, opening.width, .046f, thickness + .05f, paint.Stile);

        bool doubleDoor = opening.width > 2f;
        float hingedZ = thickness / 2 + .044f;
        if (doubleDoor)
        {
            // Each wide reception door has two independent glazed leaves.
            foreach (int side in new[] { -1, 1 })
                BuildVerifiedDoorLeaf(portal, halfWidth - .076f, halfWidth - .052f, hingedZ, true, false, paint, side);
            return;
        }

        BuildVerifiedDoorLeaf(portal, opening.width - .103f, halfWidth - .052f, hingedZ,
            false, room.kind == "service", paint);
    }

    static void BuildVerifiedDoorLeaf(Transform portal, float width, float hingeOffset, float depth,
        bool glazed, bool privatePane, OpeningMaterials paint, int pairedSide = 0)
    {
        int[] handing = pairedSide == 0 ? new[] { -1, 1 } : new[] { pairedSide };
        foreach (int side in handing)
        {
            var door = BuildCampusLeaf(portal, side, width, hingeOffset, depth, glazed, privatePane, paint);
            if (DoorMovementIsClear(door)) return;
            Object.DestroyImmediate(door.gameObject);
        }

        // A close-running side-mounted rail frees the entire opening where
        // both jambs border furniture that would intersect a swinging leaf.
        // A slim corridor-side track remains inside the frame reveal.
        foreach (float trackDepth in new[] { depth, -depth + .020f })
        {
            foreach (int side in handing)
            {
                var door = BuildCampusLeaf(portal, side, width, hingeOffset, trackDepth, glazed, privatePane, paint, true);
                if (DoorMovementIsClear(door)) return;
                Object.DestroyImmediate(door.gameObject);
            }
        }

        throw new InvalidOperationException("No clear door motion at " + portal.name + " / " + portal.position);
    }

    static bool DoorMovementIsClear(JEIRoomDoor door)
    {
        Physics.SyncTransforms();
        bool opened = door.TryToggle();
        for (int frame = 0; opened && !door.IsOpen && !door.IsBlocked && frame < 40; frame++)
            door.AdvanceMotion(.05f);
        bool clear = opened && door.IsOpen;
        door.SetOpenImmediate(false);
        Physics.SyncTransforms();
        return clear;
    }

    static JEIRoomDoor BuildCampusLeaf(Transform portal, int hingeSide, float width, float hingeOffset, float hingeDepth,
        bool fullyGlazed, bool privatePane, OpeningMaterials paint, bool sliding = false)
    {
        var mount = Group(portal, sliding ? "Sliding door actuator" : hingeSide < 0 ? "Left door actuator" : "Right door actuator");
        var hinge = Group(mount, "Door leaf hinge");
        hinge.localPosition = new Vector3(hingeSide * hingeOffset, 0, hingeDepth);
        float middleX = -hingeSide * width / 2;
        float middleY = DoorLeafBottom + DoorLeafHeight / 2;
        float top = DoorLeafBottom + DoorLeafHeight;
        float handleX = -hingeSide * (width - .13f);
        float thickness = sliding ? .028f : DoorLeafThickness;
        const float handleY = 1.05f;

        var collision = hinge.gameObject.AddComponent<BoxCollider>();
        collision.center = new Vector3(middleX, middleY, 0);
        collision.size = new Vector3(width, DoorLeafHeight, thickness);
        foreach (int stile in new[] { 0, 1 })
        {
            float x = -hingeSide * (stile == 0 ? .051f : width - .051f);
            FramePart(hinge, "Door stile", x, middleY, 0, .102f, DoorLeafHeight, thickness, paint.Stile);
        }
        FramePart(hinge, "Door top rail", middleX, top - .067f, 0, width - .188f, .134f, thickness, paint.Timber);
        float glassFloor = fullyGlazed ? .29f : privatePane ? 1.79f : 1.31f;
        float glassCeiling = top - .134f;
        FramePart(hinge, "Door lower wood panel", middleX, (glassFloor + DoorLeafBottom) / 2, 0,
            width - .188f, glassFloor - DoorLeafBottom, thickness, paint.Timber);
        AddOpeningGlass(hinge, privatePane ? "High frosted privacy light" : "Door vision panel",
            new Vector3(middleX, (glassFloor + glassCeiling) / 2, 0), new Vector3(width - .192f, glassCeiling - glassFloor, .020f),
            privatePane ? paint.Frosted : paint.Glass, false);
        FramePart(hinge, "Glazing sill", middleX, glassFloor + .027f, 0, width - .188f, .054f, thickness, paint.Stile);
        if (fullyGlazed)
            FramePart(hinge, "Reception glazing safety strip", middleX, 1.13f, 0, width - .188f, .054f, .028f, paint.SafetyBand);

        foreach (int side in new[] { -1, 1 })
        {
            FramePart(hinge, "Lever plate", handleX, handleY, side * .027f, .054f, .14f, .012f, paint.Metal);
            FramePart(hinge, "Lever stem", handleX, handleY, side * .048f, .029f, .032f, .041f, paint.Metal);
            FramePart(hinge, "Door lever", handleX + hingeSide * .05f, handleY, side * .067f, .125f, .03f, .023f, paint.Metal);
        }
        var controller = mount.gameObject.AddComponent<JEIRoomDoor>();
        Vector3 handle = hinge.localPosition + new Vector3(handleX, handleY, 0);
        if (sliding)
        {
            float travel = hingeSide * (width + .062f);
            controller.ConfigureSliding(hinge, new Collider[] { collision }, handle, Vector3.right * travel);
            FramePart(mount, "Covered door rail", hingeSide * hingeOffset, top + .038f, hingeDepth,
                width + Mathf.Abs(travel) + .05f, .072f, .075f, paint.Stile);
        }
        else
        {
            controller.Configure(hinge, new Collider[] { collision }, handle, hingeSide * DoorSwingDegrees);
        }
        return controller;
    }

    static void FramePart(Transform parent, string name, float x, float y, float z, float width, float height, float depth, Material material)
    {
        D(parent, name, new Vector3(x, y, z), new Vector3(width, height, depth), material);
    }

    static void AddOpeningGlass(Transform parent, string name, Vector3 center, Vector3 size, Material material, bool solid)
    {
        var pane = D(parent, name, center, size, material, solid);
        var renderer = pane.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
