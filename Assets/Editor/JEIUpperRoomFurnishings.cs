using System;
using UnityEditor;
using UnityEngine;

public static partial class JEIStructureBlockout
{
    // The upper rooms keep the first floor's oak, sage and cream palette.
    static void FurnishUpperRoom(Transform group, Room room, float doorX)
    {
        var previous = (decor, oak, white, metal, teal, blue, yellow, purple, paper, screen);
        try
        {
            decor = group;
            oak = UpperFurnitureMaterial("Light_Oak", oak);
            white = UpperFurnitureMaterial("Classroom_Warm_White", white);
            metal = UpperFurnitureMaterial("Graphite_Metal", metal);
            teal = UpperFurnitureMaterial("Classroom_Teal", teal);
            blue = UpperFurnitureMaterial("N01_Chair_Sage", blue);
            yellow = UpperFurnitureMaterial("Classroom_Mustard", yellow);
            purple = UpperFurnitureMaterial("S04_Meeting_Taupe_Fabric", purple);
            paper = UpperFurnitureMaterial("Paper_Cream", paper);
            screen = UpperFurnitureMaterial("Classroom_Display", screen);

            var layout = new UpperRoomLayout(room, doorX);
            switch (room.id)
            {
                case "02-N01": UpperClassroom(layout, false); break;
                case "02-N02": UpperTeachingStudio(layout); break;
                case "02-N03": UpperClassroom(layout, true); break;
                case "02-S01": UpperTeacherOffice(layout); break;
                case "02-S02": FurnishReceptionOffice(layout, ReceptionOffice.Faculty); break;
                case "02-S03": UpperStudyRoom(layout, StudyStyle.Group); break;
                case "02-S04": UpperSeatedTeachingRoom(layout, false, yellow); break;
                case "02-S05": UpperDiscussionRoom(layout, DiscussionStyle.Meeting); break;
                case "03-N01": UpperDiscussionRoom(layout, DiscussionStyle.Seminar); break;
                case "03-N02": UpperSeatedTeachingRoom(layout, false, blue); break;
                case "03-N03": UpperSeatedTeachingRoom(layout, false, teal); break;
                case "03-N04": FurnishReceptionOffice(layout, ReceptionOffice.Program); break;
                case "03-N05": FurnishReceptionOffice(layout, ReceptionOffice.Advisor); break;
                case "03-S01": UpperResearchOffice(layout); break;
                case "03-S02": UpperStudyRoom(layout, StudyStyle.Quiet); break;
                case "03-S03": UpperSeatedTeachingRoom(layout, true, purple); break;
                default: throw new ArgumentException("No furnishings defined for " + room.id);
            }
            UpperRoomLights(layout);
        }
        finally
        {
            (decor, oak, white, metal, teal, blue, yellow, purple, paper, screen) = previous;
        }
    }

    static Material UpperFurnitureMaterial(string name, Material fallback)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ImportedFurnishingMaterials + name + ".mat");
        return material ? material : fallback;
    }

    // All distances are measured from the corridor wall, into the room.
    sealed class UpperRoomLayout
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Door;
        public readonly float Depth;
        readonly float corridorWall;
        readonly bool north;

        public UpperRoomLayout(Room room, float door)
        {
            Left = room.x0 + InsetX(room.x0);
            Right = room.x1 - InsetX(room.x1);
            Door = door;
            north = room.z0 > 0;
            corridorWall = north ? room.z0 : room.z1;
            Depth = room.z1 - room.z0;
        }

        public float Width => Right - Left;
        public float Center => (Left + Right) / 2;
        public float TowardDoor => north ? 180 : 0;
        public float TowardWindows => north ? 0 : 180;
        public float Z(float depth) => corridorWall + (north ? depth : -depth);
        public float Bay(int side) => side < 0 ? (Left + Door - .65f) / 2 : (Door + .65f + Right) / 2;
    }

    static void UpperClassroom(UpperRoomLayout room, bool computers)
    {
        // The two banks face the teaching wall and leave the doorway axis clear.
        float[] rows = computers ? new[] { 2.25f, 4.65f } : new[] { 1.42f, 3.32f, 5.22f };
        float spacing = computers ? 1.34f : 1.22f;
        foreach (int side in new[] { -1, 1 })
        {
            float outer = (side < 0 ? room.Left : room.Right) - side * .82f;
            float inner = room.Door + side * 1.28f;
            int count = Mathf.Max(1, Mathf.FloorToInt(Mathf.Abs(outer - inner) / spacing) + 1);
            for (int column = 0; column < count; column++)
            {
                float x = count == 1 ? (inner + outer) / 2 : Mathf.Lerp(inner, outer, (float)column / (count - 1));
                foreach (float row in rows)
                {
                    Desk(x, room.Z(row), room.TowardDoor, computers, computers ? 1.12f : 1.02f);
                    Chair(x, room.Z(row + .7f), room.TowardDoor, computers ? purple : (column % 3 == 2 ? yellow : blue));
                }
            }
        }
        UpperTeachingBoard(room.Bay(-1), room.Z(.16f), room.TowardDoor, 3.0f, computers ? metal : teal, computers);
        UpperLowCabinet(room.Bay(1), room.Z(.39f), room.TowardDoor, 1.85f);
        UpperPlant(room.Right - .35f, room.Z(room.Depth - .42f));
    }

    static void UpperSeatedTeachingRoom(UpperRoomLayout room, bool lab, Material accent)
    {
        const float centerAisleHalfWidth = .57f;
        const float sideAisleWidth = .64f;
        float deskWidth = lab ? .91f : .76f;
        float deskDepth = lab ? .66f : .46f;
        float[] rows = lab ? new[] { 1.72f, 3.52f, 5.32f } : new[] { 1.36f, 2.88f, 4.40f, 5.92f };
        foreach (int side in new[] { -1, 1 })
        {
            float firstEdge = room.Door + side * centerAisleHalfWidth;
            float outerEdge = side < 0 ? room.Left + sideAisleWidth : room.Right - sideAisleWidth;
            int columns = Mathf.FloorToInt(Mathf.Abs(outerEdge - firstEdge) / deskWidth);
            for (int column = 0; column < columns; column++)
            {
                float x = firstEdge + side * (column + .5f) * deskWidth;
                foreach (float depth in rows)
                {
                    if (lab)
                        Desk(x, room.Z(depth), room.TowardDoor, true, deskWidth - .015f);
                    else
                        UpperStudentWritingDesk(x, room.Z(depth), room.TowardDoor, deskWidth - .015f, deskDepth);
                    UpperCompactClassroomChair(x, room.Z(depth + (lab ? .63f : .47f)), room.TowardDoor, accent);
                }
            }
        }

        float boardWidth = Mathf.Min(lab ? 3.30f : 2.45f, room.Door - room.Left - 1.05f);
        UpperTeachingBoard(room.Bay(-1), room.Z(.16f), room.TowardDoor, boardWidth, teal, lab);
        var podium = Item("Teacher lectern", room.Bay(1), room.Z(.31f), room.TowardDoor);
        D(podium, "Pedestal", new Vector3(0, .51f, 0), new Vector3(.46f, 1.02f, .24f), white, true);
        D(podium, "Oak top", new Vector3(0, 1.055f, 0), new Vector3(.70f, .07f, .38f), oak);
        D(podium, "Sage front inset", new Vector3(0, .78f, .127f), new Vector3(.34f, .21f, .02f), accent);
        D(podium, "Lecture notes", new Vector3(.065f, 1.098f, 0), new Vector3(.28f, .012f, .25f), paper);
        UpperTeachingBoard(room.Bay(1), room.Z(.15f), room.TowardDoor, 1.45f, accent);
    }

    static void UpperStudentWritingDesk(float x, float z, float yaw, float width, float depth)
    {
        var desk = Item("Student writing desk", x, z, yaw);
        D(desk, "Pale oak writing surface", new Vector3(0, .746f, 0), new Vector3(width, .045f, depth), oak);
        foreach (float legX in new[] { -width / 2 + .09f, width / 2 - .09f })
        {
            foreach (float legZ in new[] { -depth / 2 + .06f, depth / 2 - .06f })
                D(desk, "Powder-coated frame", new Vector3(legX, .365f, legZ), new Vector3(.032f, .73f, .032f), metal);
            D(desk, "Desk foot", new Vector3(legX, .026f, 0), new Vector3(.038f, .045f, depth - .07f), metal);
        }
        D(desk, "Under-desk book shelf", new Vector3(0, .56f, 0), new Vector3(width - .14f, .027f, depth - .15f), white);
        D(desk, "Exercise book", new Vector3(-.12f, .777f, -.01f), new Vector3(.24f, .017f, .28f), paper);
        D(desk, "Pencil", new Vector3(.06f, .78f, -.02f), new Vector3(.014f, .014f, .15f), teal);
        Solid(desk, new Vector3(0, .39f, 0), new Vector3(width, .78f, depth));
    }

    static void UpperCompactClassroomChair(float x, float z, float yaw, Material fabric)
    {
        var chair = Item("Student seat", x, z, yaw);
        D(chair, "Upholstered seat shell", new Vector3(0, .445f, 0), new Vector3(.43f, .053f, .35f), fabric);
        D(chair, "Seat back", new Vector3(0, .67f, -.158f), new Vector3(.42f, .36f, .045f), fabric);
        foreach (float legX in new[] { -.17f, .17f })
        {
            foreach (float legZ in new[] { -.13f, .13f })
                D(chair, "Seat frame", new Vector3(legX, .22f, legZ), new Vector3(.031f, .44f, .031f), metal);
        }
        Solid(chair, new Vector3(0, .423f, 0), new Vector3(.44f, .846f, .37f));
    }

    static void UpperTeachingStudio(UpperRoomLayout room)
    {
        foreach (int side in new[] { -1, 1 })
        {
            float x = room.Bay(side);
            UpperTeamTable(room, x, 2.45f, 2.05f, 2, side < 0 ? blue : yellow);
            UpperTeamTable(room, x, 5.12f, 2.05f, 2, side < 0 ? yellow : blue);
        }
        UpperTeachingBoard(room.Bay(1), room.Z(.16f), room.TowardDoor, 2.75f, teal);
        UpperLowCabinet(room.Bay(-1), room.Z(.42f), room.TowardDoor, 2.15f);
        UpperPlant(room.Right - .43f, room.Z(room.Depth - .55f));
    }

    enum DiscussionStyle { Seminar, Meeting }

    static void UpperDiscussionRoom(UpperRoomLayout room, DiscussionStyle style)
    {
        const float depth = 4.4f;
        float width = style == DiscussionStyle.Meeting ? 3.35f : 2.75f;
        var fabric = style == DiscussionStyle.Meeting ? purple : blue;
        UpperTeamTable(room, room.Center, depth, width, style == DiscussionStyle.Meeting ? 3 : 2, fabric);
        foreach (int side in new[] { -1, 1 })
            Chair(room.Center + side * (width / 2 + .47f), room.Z(depth), side < 0 ? 90 : 270, fabric);

        UpperTeachingBoard(room.Right - .065f, room.Z(depth), 90, style == DiscussionStyle.Meeting ? 2.9f : 2.35f, teal, style == DiscussionStyle.Meeting);
        UpperLowCabinet(room.Bay(-1), room.Z(.44f), room.TowardDoor, Mathf.Min(1.55f, room.Width * .22f));
        UpperPlant(room.Left + .45f, room.Z(room.Depth - .53f));
    }

    enum StudyStyle { Group, Quiet }

    static void UpperStudyRoom(UpperRoomLayout room, StudyStyle style)
    {
        float left = room.Bay(-1);
        float right = room.Bay(1);
        if (style == StudyStyle.Quiet)
        {
            for (int i = -1; i <= 1; i++)
            {
                float x = left + i * 1.22f;
                Desk(x, room.Z(5.55f), room.TowardWindows, false, 1.12f);
                Chair(x, room.Z(4.84f), room.TowardWindows, blue);
            }
            UpperTeamTable(room, right, 3.25f, 2.50f, 3, purple);
            Shelf(room.Bay(-1), room.Z(.42f), room.TowardDoor, 2.6f);
        }
        else
        {
            UpperTeamTable(room, left, 2.65f, 1.64f, 2, blue);
            UpperTeamTable(room, right, 4.5f, 1.64f, 2, yellow);
            Shelf(room.Bay(1), room.Z(.44f), room.TowardDoor, 1.48f);
            UpperLowCabinet(room.Left + .37f, room.Z(5.58f), 270, 1.5f);
        }
        UpperPlant(room.Right - .44f, room.Z(room.Depth - .50f));
    }

    static void UpperTeacherOffice(UpperRoomLayout room)
    {
        // Shared preparation room with two window-facing workstations and wall storage.
        foreach (int side in new[] { -1, 1 })
        {
            float x = room.Center + side * 1.20f;
            Desk(x, room.Z(5.45f), room.TowardWindows, true, 1.62f);
            Chair(x, room.Z(4.72f), room.TowardWindows, side < 0 ? blue : purple);
        }

        Shelf(room.Left + .26f, room.Z(2.4f), 270, 1.75f, false);
        UpperLowCabinet(room.Right - .29f, room.Z(2.05f), 90, 1.55f);
        var printer = Item("Shared printer", room.Right - .29f, room.Z(2.05f), 90);
        D(printer, "Printer body", new Vector3(0, .98f, 0), new Vector3(.47f, .28f, .36f), metal);
        D(printer, "Control panel", new Vector3(.16f, 1.12f, -.095f), new Vector3(.11f, .02f, .085f), screen);
        D(printer, "Output tray", new Vector3(-.065f, .955f, -.19f), new Vector3(.25f, .028f, .08f), white);
        UpperPlant(room.Right - .42f, room.Z(room.Depth - .45f));
    }

    static void UpperResearchOffice(UpperRoomLayout room)
    {
        foreach (int side in new[] { -1, 1 })
        {
            float bay = room.Bay(side);
            foreach (float offset in new[] { -.875f, .875f })
            {
                float x = bay + offset;
                Desk(x, room.Z(5.52f), room.TowardWindows, true, 1.45f);
                Chair(x, room.Z(4.79f), room.TowardWindows, purple);
            }
        }

        float lounge = room.Bay(-1);
        Sofa(lounge, room.Z(1.15f), room.TowardDoor, purple, 1.64f);
        CoffeeTable(lounge, room.Z(2.24f));
        UpperTeamTable(room, room.Bay(1), 2.8f, 1.95f, 2, purple);
        Shelf(room.Bay(1), room.Z(.45f), room.TowardDoor, 1.85f, false);
        UpperPlant(room.Right - .45f, room.Z(room.Depth - .43f));
    }

    static void UpperTeamTable(UpperRoomLayout room, float x, float depth, float width, int seatsPerSide, Material fabric)
    {
        var table = Item("Shared oak table", x, room.Z(depth));
        D(table, "Oak tabletop", new Vector3(0, .765f, 0), new Vector3(width, .075f, 1.02f), oak);
        float legOffset = width / 2 - .13f;
        foreach (float dx in new[] { -legOffset, legOffset })
            foreach (float dz in new[] { -.40f, .40f })
                D(table, "Metal table leg", new Vector3(dx, .365f, dz), new Vector3(.055f, .73f, .055f), metal);
        Solid(table, new Vector3(0, .4f, 0), new Vector3(width, .8f, 1.02f));
        D(table, "Shared worksheets", new Vector3(-width * .23f, .812f, -.12f), new Vector3(.27f, .014f, .31f), paper);
        D(table, "Notebook", new Vector3(width * .20f, .823f, .11f), new Vector3(.25f, .036f, .32f), teal);
        for (int i = 0; i < seatsPerSide; i++)
        {
            float seatX = x + width * ((i + .5f) / seatsPerSide - .5f) * .87f;
            Chair(seatX, room.Z(depth - .82f), room.TowardWindows, fabric);
            Chair(seatX, room.Z(depth + .82f), room.TowardDoor, fabric);
        }
    }

    static void UpperTeachingBoard(float x, float z, float yaw, float width, Material trim, bool display = false)
    {
        var board = Item(display ? "Presentation display" : "Teaching whiteboard", x, z, yaw);
        D(board, "Board housing", new Vector3(0, 1.83f, 0), new Vector3(width + .06f, 1.12f, .055f), trim);
        D(board, "Board surface", new Vector3(0, 1.83f, -.031f), new Vector3(width - .03f, 1.04f, .01f), display ? screen : white);
        if (display)
        {
            D(board, "Presentation header", new Vector3(0, 2.17f, -.04f), new Vector3(width * .91f, .19f, .006f), teal);
            for (int i = 0; i < 3; i++)
                D(board, "Presentation graphic", new Vector3(-width * .27f + i * .21f, 1.69f + i * .04f, -.04f), new Vector3(.13f, .24f + .08f * i, .006f), yellow);
        }
        else
        {
            D(board, "Marker ledge", new Vector3(0, 1.25f, -.07f), new Vector3(width, .038f, .085f), oak);
            foreach (int i in new[] { 0, 1, 2 })
                D(board, "Marker", new Vector3(.29f + i * .13f, 1.277f, -.067f), new Vector3(.085f, .015f, .018f), i == 0 ? teal : i == 1 ? yellow : metal);
        }
    }

    static void UpperLowCabinet(float x, float z, float yaw, float width)
    {
        var cabinet = Item("Cream storage credenza", x, z, yaw);
        D(cabinet, "Cabinet body", new Vector3(0, .39f, 0), new Vector3(width, .78f, .44f), white, true);
        D(cabinet, "Oak countertop", new Vector3(0, .81f, 0), new Vector3(width + .05f, .045f, .48f), oak);
        for (int index = -1; index <= 1; index++)
        {
            float offset = index * width / 3;
            D(cabinet, "Recessed panel", new Vector3(offset, .42f, -.225f), new Vector3(width / 3 - .035f, .64f, .022f), paper);
            D(cabinet, "Metal handle", new Vector3(offset, .63f, -.247f), new Vector3(.09f, .018f, .02f), metal);
        }
    }

    static void UpperPlant(float x, float z)
    {
        Plant(x, z, .82f);
    }

    static void UpperRoomLights(UpperRoomLayout room)
    {
        int count = room.Width > 8.5f ? 2 : 1;
        var diffuser = FitMat("Upper_Warm_Ceiling_Diffuser", new Color(.96f, .96f, .9f));
        diffuser.EnableKeyword("_EMISSION");
        diffuser.SetColor("_EmissionColor", new Color(1f, .95f, .87f) * 1.25f);
        EditorUtility.SetDirty(diffuser);
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(room.Left, room.Right, (i + .5f) / count);
            float z = room.Z(room.Depth * .53f);
            D(decor, "Ceiling panel frame", new Vector3(x, 3.23f, z), new Vector3(1.24f, .07f, .56f), metal);
            D(decor, "Warm ceiling diffuser", new Vector3(x, 3.187f, z), new Vector3(1.17f, .02f, .49f), diffuser);
            var source = Group(decor, "Downward room light");
            source.localPosition = new Vector3(x, 3.16f, z);
            source.localRotation = Quaternion.Euler(90, 0, 0);
            var light = source.gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 5.0f;
            light.spotAngle = 112f;
            light.innerSpotAngle = 88f;
            light.color = new Color(1f, .96f, .90f);
            light.intensity = 6.0f;
        }
    }
}
