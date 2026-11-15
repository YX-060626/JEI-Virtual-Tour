using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class JEIStructureBlockout
{
    enum ReceptionOffice { Faculty, Program, Advisor }

    static void FurnishReceptionOffice(UpperRoomLayout room, ReceptionOffice use)
    {
        bool loungeOnLeft = use == ReceptionOffice.Faculty;
        float side = loungeOnLeft ? 1 : -1;
        float loungeWall = loungeOnLeft ? room.Left : room.Right;
        float workWall = loungeOnLeft ? room.Right : room.Left;
        float loungeYaw = loungeOnLeft ? 270 : 90;
        float workWallYaw = loungeOnLeft ? 90 : 270;
        float loungeDepth = use == ReceptionOffice.Program ? 3.22f : 2.70f;
        float workDepth = use == ReceptionOffice.Faculty ? 5.22f : 5.05f;
        var walnut = FitMat("Office_Warm_Walnut", new Color(.31f, .19f, .12f));
        var walnutEdge = FitMat("Office_Walnut_Edge", new Color(.43f, .28f, .17f));
        var brass = FitMat("Office_Champagne_Brass", new Color(.74f, .57f, .33f));
        var sand = UpperFurnitureMaterial("S01_Faculty_Oatmeal_Fabric", white);
        var fabric = use == ReceptionOffice.Faculty ? sand :
            use == ReceptionOffice.Program ? purple : UpperFurnitureMaterial("Fabric_Teal", teal);
        var pillow = use == ReceptionOffice.Faculty ? blue : sand;
        var carpet = use == ReceptionOffice.Program ?
            UpperFurnitureMaterial("Quiet_Office_Carpet", tile) : FitMat("Office_Woven_Sand", new Color(.63f, .59f, .52f));

        // The two furniture zones flank a continuous route from the door to the window.
        float deskX = workWall - side * 1.39f;
        float deskWidth = use == ReceptionOffice.Program ? 1.88f : 1.70f;
        BuildOfficeWorkstation(deskX, room.Z(workDepth), room.TowardDoor, deskWidth, walnut, walnutEdge, brass);
        UpperUpholsteredSeat(deskX, room.Z(workDepth + .80f), room.TowardWindows, .61f, metal, walnut, null);
        UpperUpholsteredSeat(deskX, room.Z(workDepth - .95f), room.TowardDoor, .61f, sand, walnut, null);
        BuildOfficePrintCabinet(workWall - side * .33f, room.Z(use == ReceptionOffice.Advisor ? 1.62f : 2.30f),
            workWallYaw, walnut, walnutEdge, brass);

        UpperRug(loungeWall + side * 1.23f, room.Z(loungeDepth + .10f), carpet, sand);
        UpperUpholsteredSeat(loungeWall + side * .47f, room.Z(loungeDepth), loungeYaw, 1.93f, fabric, walnut, pillow);
        float guestDepth = use == ReceptionOffice.Program ? loungeDepth - 1.45f : loungeDepth + 1.40f;
        float guestYaw = use == ReceptionOffice.Program ? room.TowardDoor : room.TowardWindows;
        UpperUpholsteredSeat(loungeWall + side * 1.34f, room.Z(guestDepth), guestYaw, .65f, pillow, walnut, null);
        UpperReceptionTable(loungeWall + side * 1.38f, room.Z(loungeDepth - .04f), walnut, brass, use != ReceptionOffice.Program);
        UpperOfficeBookcase(loungeWall + side * .27f, room.Z(5.75f), loungeYaw, walnut, brass);

        float lampDepth = use == ReceptionOffice.Program ? loungeDepth + 1.35f : loungeDepth - 1.30f;
        UpperFloorLamp(loungeWall + side * .49f, room.Z(lampDepth), brass, sand);
        bool sideWindow = use == ReceptionOffice.Advisor;
        float artworkX = sideWindow ? workWall - side * .047f : loungeWall + side * .047f;
        float artworkYaw = sideWindow ? workWallYaw : loungeYaw;
        UpperOfficeArtwork(artworkX, room.Z(loungeDepth), artworkYaw, walnut, fabric, brass);
        UpperPlant(workWall - side * .41f, room.Z(room.Depth - .47f));
    }

    static void BuildOfficeWorkstation(float x, float z, float yaw, float width, Material walnut, Material edge, Material brass)
    {
        var desk = Item("Office computer workstation", x, z, yaw);
        D(desk, "Walnut desktop", new Vector3(0, .765f, 0), new Vector3(width, .075f, .83f), walnut);
        D(desk, "Desktop edge", new Vector3(0, .778f, .422f), new Vector3(width, .035f, .015f), edge);
        D(desk, "Left wood support", new Vector3(-width / 2 + .10f, .36f, 0), new Vector3(.095f, .72f, .70f), walnut);
        D(desk, "Drawer pedestal", new Vector3(width / 2 - .24f, .36f, 0), new Vector3(.38f, .72f, .69f), walnut);
        for (int i = 0; i < 3; i++)
        {
            float y = .19f + i * .21f;
            D(desk, "Walnut drawer", new Vector3(width / 2 - .24f, y, -.352f), new Vector3(.34f, .19f, .02f), edge);
            D(desk, "Drawer pull", new Vector3(width / 2 - .24f, y + .05f, -.371f), new Vector3(.11f, .018f, .02f), brass);
        }
        D(desk, "Walnut modesty panel", new Vector3(0, .44f, .335f), new Vector3(width - .21f, .52f, .055f), walnut);
        Solid(desk, new Vector3(0, .40f, 0), new Vector3(width, .80f, .83f));

        D(desk, "Monitor shell", new Vector3(-.16f, 1.16f, .18f), new Vector3(.57f, .36f, .043f), metal);
        D(desk, "Monitor display", new Vector3(-.16f, 1.16f, .155f), new Vector3(.525f, .31f, .009f), screen);
        D(desk, "Monitor stand", new Vector3(-.16f, .912f, .18f), new Vector3(.05f, .21f, .055f), metal);
        D(desk, "Monitor base", new Vector3(-.16f, .817f, .18f), new Vector3(.23f, .025f, .21f), metal);
        D(desk, "Keyboard", new Vector3(-.16f, .816f, -.145f), new Vector3(.41f, .025f, .12f), metal);
        Shape(desk, "Mouse", PrimitiveType.Sphere, new Vector3(.15f, .817f, -.145f), new Vector3(.075f, .032f, .11f), metal);
        D(desk, "Office notebook", new Vector3(.49f, .822f, -.12f), new Vector3(.26f, .029f, .30f), teal);
        D(desk, "Pen", new Vector3(.48f, .844f, -.12f), new Vector3(.015f, .015f, .16f), brass);
    }

    static void BuildOfficePrintCabinet(float x, float z, float yaw, Material walnut, Material edge, Material brass)
    {
        var cabinet = Item("Printer and document cabinet", x, z, yaw);
        D(cabinet, "Walnut cabinet body", new Vector3(0, .40f, 0), new Vector3(1.51f, .76f, .51f), walnut, true);
        D(cabinet, "Cabinet top", new Vector3(0, .795f, 0), new Vector3(1.57f, .05f, .55f), edge);
        foreach (int side in new[] { -1, 1 })
        {
            D(cabinet, "Cabinet door", new Vector3(side * .365f, .41f, -.264f), new Vector3(.70f, .65f, .025f), walnut);
            D(cabinet, "Cabinet handle", new Vector3(side * .14f, .58f, -.285f), new Vector3(.13f, .018f, .018f), brass);
        }
        D(cabinet, "Printer lower body", new Vector3(-.33f, .935f, 0), new Vector3(.48f, .23f, .43f), metal);
        D(cabinet, "Printer lid", new Vector3(-.33f, 1.068f, .03f), new Vector3(.48f, .046f, .35f), white);
        D(cabinet, "Paper output slot", new Vector3(-.33f, .939f, -.217f), new Vector3(.31f, .056f, .015f), white);
        D(cabinet, "Output paper", new Vector3(-.33f, .92f, -.25f), new Vector3(.23f, .008f, .16f), paper);
        D(cabinet, "Printer display", new Vector3(-.14f, 1.059f, -.184f), new Vector3(.069f, .012f, .049f), screen);
        D(cabinet, "Document tray", new Vector3(.39f, .858f, 0), new Vector3(.37f, .075f, .32f), edge);
        D(cabinet, "Paper stack", new Vector3(.39f, .903f, 0), new Vector3(.29f, .025f, .24f), paper);
    }

    // Seat backs sit on the local +Z edge. A wide seat is a two-cushion sofa.
    static void UpperUpholsteredSeat(float x, float z, float yaw, float width, Material fabric, Material wood, Material pillow)
    {
        bool sofa = width > 1f;
        var seat = Item(sofa ? "Upholstered reception sofa" : "Upholstered office chair", x, z, yaw);
        float depth = sofa ? .79f : .68f;
        float cushionWidth = sofa ? (width - .24f) / 2 : width - .15f;
        OfficePadded(seat, "Upholstered seat base", new Vector3(0, .32f, 0), new Vector3(width, .21f, depth), fabric);
        int cushions = sofa ? 2 : 1;
        for (int i = 0; i < cushions; i++)
        {
            float dx = (i - (cushions - 1) / 2f) * (cushionWidth + .015f);
            OfficePadded(seat, "Seat cushion", new Vector3(dx, .455f, -.055f), new Vector3(cushionWidth - .015f, .115f, depth - .19f), fabric);
            OfficePadded(seat, "Back cushion", new Vector3(dx, .68f, depth / 2 - .086f),
                new Vector3(cushionWidth, .56f, .17f), fabric);
        }
        foreach (int side in new[] { -1, 1 })
        {
            float sideX = side * (width / 2 - .058f);
            OfficePadded(seat, "Padded arm", new Vector3(sideX, .531f, -.022f), new Vector3(.115f, .34f, depth - .04f), fabric);
            foreach (int end in new[] { -1, 1 })
                D(seat, "Walnut foot", new Vector3(sideX, .133f, end * (depth / 2 - .085f)), new Vector3(.054f, .18f, .055f), wood);
        }
        if (pillow)
        {
            OfficePadded(seat, "Throw pillow", new Vector3(-width * .30f, .61f, .18f), new Vector3(.30f, .32f, .11f), pillow).localRotation = Quaternion.Euler(0, 0, 14);
            OfficePadded(seat, "Accent pillow", new Vector3(width * .29f, .60f, .18f), new Vector3(.27f, .29f, .12f), pillow).localRotation = Quaternion.Euler(0, 0, -12);
        }
        Solid(seat, new Vector3(0, .49f, 0), new Vector3(width, .98f, depth));
    }

    static void UpperRug(float x, float z, Material fabric, Material border)
    {
        var rug = Item("Woven reception rug", x, z);
        D(rug, "Woven field", new Vector3(0, .032f, 0), new Vector3(2.14f, .013f, 2.82f), fabric);
        foreach (int side in new[] { -1, 1 })
        {
            D(rug, "Rug woven border", new Vector3(side * .997f, .039f, 0), new Vector3(.035f, .003f, 2.66f), border);
            D(rug, "Rug woven border", new Vector3(0, .039f, side * 1.33f), new Vector3(2.03f, .003f, .035f), border);
        }
    }

    static void UpperReceptionTable(float x, float z, Material walnut, Material brass, bool round)
    {
        var table = Item("Walnut reception table", x, z);
        if (round)
            Shape(table, "Round walnut tabletop", PrimitiveType.Cylinder, new Vector3(0, .48f, 0), new Vector3(.68f, .035f, .68f), walnut);
        else
            OfficePadded(table, "Rectangular walnut tabletop", new Vector3(0, .48f, 0), new Vector3(.65f, .07f, .87f), walnut);
        Shape(table, "Brass pedestal", PrimitiveType.Cylinder, new Vector3(0, .24f, 0), new Vector3(.075f, .22f, .075f), brass);
        Shape(table, "Table foot", PrimitiveType.Cylinder, new Vector3(0, .05f, 0), new Vector3(.41f, .025f, .41f), metal);
        D(table, "Reading book", new Vector3(-.09f, .532f, -.035f), new Vector3(.23f, .035f, .29f), teal);
        Shape(table, "Ceramic cup", PrimitiveType.Cylinder, new Vector3(.14f, .565f, .12f), new Vector3(.08f, .053f, .08f), white);
        Solid(table, new Vector3(0, .25f, 0), new Vector3(.68f, .50f, round ? .68f : .87f));
    }

    static void UpperOfficeBookcase(float x, float z, float yaw, Material walnut, Material brass)
    {
        var bookcase = Item("Walnut bookcase and archive", x, z, yaw);
        const float width = 1.44f;
        D(bookcase, "Walnut back", new Vector3(0, 1.04f, .191f), new Vector3(width, 2.08f, .055f), walnut);
        foreach (int side in new[] { -1, 1 })
            D(bookcase, "Walnut upright", new Vector3(side * .70f, 1.04f, 0), new Vector3(.045f, 2.08f, .42f), walnut);
        foreach (float y in new[] { .12f, .59f, 1.08f, 1.58f, 2.06f })
            D(bookcase, "Walnut shelf", new Vector3(0, y, 0), new Vector3(width, .045f, .42f), walnut);
        D(bookcase, "Archive door", new Vector3(0, .35f, -.218f), new Vector3(1.35f, .40f, .035f), walnut);
        D(bookcase, "Archive handle", new Vector3(0, .46f, -.239f), new Vector3(.19f, .019f, .022f), brass);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 7 - row; i++)
            {
                float h = .20f + ((row + i) % 4) * .038f;
                var color = (i % 3 == 0) ? teal : (i % 3 == 1) ? paper : blue;
                D(bookcase, "Book spine", new Vector3(-.58f + i * .13f, .614f + row * .495f + h / 2, -.068f),
                    new Vector3(.10f, h, .23f), color);
            }
        Solid(bookcase, new Vector3(0, 1.04f, 0), new Vector3(width, 2.08f, .44f));
    }

    static void UpperFloorLamp(float x, float z, Material brass, Material shade)
    {
        var lamp = Item("Warm reading lamp", x, z);
        Shape(lamp, "Lamp base", PrimitiveType.Cylinder, new Vector3(0, .059f, 0), new Vector3(.27f, .034f, .27f), metal);
        Shape(lamp, "Lamp stem", PrimitiveType.Cylinder, new Vector3(0, .755f, 0), new Vector3(.026f, .715f, .026f), brass);
        Shape(lamp, "Fabric lampshade", PrimitiveType.Cylinder, new Vector3(0, 1.485f, 0), new Vector3(.36f, .125f, .36f), shade);
        var glow = FitMat("Office_Warm_Lamp_Lining", new Color(1f, .86f, .63f));
        glow.EnableKeyword("_EMISSION");
        glow.SetColor("_EmissionColor", new Color(1f, .79f, .55f) * 1.20f);
        EditorUtility.SetDirty(glow);
        Shape(lamp, "Illuminated shade lining", PrimitiveType.Cylinder, new Vector3(0, 1.362f, 0), new Vector3(.31f, .01f, .31f), glow);
        Solid(lamp, new Vector3(0, .71f, 0), new Vector3(.28f, 1.42f, .28f));

        var lampLight = Group(lamp, "Warm pool of light");
        lampLight.localPosition = new Vector3(0, 1.33f, 0);
        lampLight.localRotation = Quaternion.Euler(90, 0, 0);
        var light = lampLight.gameObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, .83f, .66f);
        light.intensity = .78f;
        light.range = 2.15f;
        light.spotAngle = 113f;
    }

    static void UpperOfficeArtwork(float x, float z, float yaw, Material walnut, Material fabric, Material brass)
    {
        var frame = Item("Framed study print", x, z, yaw);
        D(frame, "Walnut frame", new Vector3(0, 1.90f, 0), new Vector3(1.17f, .79f, .041f), walnut);
        D(frame, "Canvas mat", new Vector3(0, 1.9f, -.025f), new Vector3(1.10f, .72f, .015f), paper);
        D(frame, "Painted color block", new Vector3(-.23f, 1.91f, -.036f), new Vector3(.31f, .45f, .007f), fabric);
        D(frame, "Painted gold block", new Vector3(.12f, 1.97f, -.036f), new Vector3(.26f, .24f, .007f), brass);
        D(frame, "Painted charcoal line", new Vector3(.13f, 1.79f, -.037f), new Vector3(.34f, .028f, .007f), metal);
    }

    static Transform OfficePadded(Transform parent, string name, Vector3 at, Vector3 size, Material material)
    {
        var piece = D(parent, name, at, size, material);
        piece.GetComponent<MeshFilter>().sharedMesh = OfficeRoundedBox();
        return piece.transform;
    }

    static Mesh OfficeRoundedBox()
    {
        string path = FitoutAssets + "/Office_Rounded_Box.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;

        const float center = .386f;
        const float rounding = .5f - center;
        float[] steps = { -.5f, -.464f, -.423f, -center, center, .423f, .464f, .5f };
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        foreach (var normal in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down })
        {
            int offset = positions.Count;
            Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
            Vector3 v = Vector3.Cross(normal, u);
            foreach (float dy in steps)
                foreach (float dx in steps)
                {
                    Vector3 face = normal * .5f + u * dx + v * dy;
                    var inner = new Vector3(
                        Mathf.Clamp(face.x, -center, center),
                        Mathf.Clamp(face.y, -center, center),
                        Mathf.Clamp(face.z, -center, center));
                    Vector3 radial = (face - inner).normalized;
                    positions.Add(inner + radial * rounding);
                    normals.Add(radial);
                }
            for (int y = 0; y < steps.Length - 1; y++)
                for (int x = 0; x < steps.Length - 1; x++)
                {
                    int a = offset + y * steps.Length + x;
                    int b = a + 1;
                    int c = a + steps.Length;
                    int d = c + 1;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
        }
        mesh = new Mesh { name = "Rounded upholstery block", indexFormat = IndexFormat.UInt16 };
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
