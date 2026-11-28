using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class JEIStructureBlockout
{
    const float GalleryDeskHeight = .76f;
    const string GalleryReadingFurnitureName = "Gallery reading furniture";
    const string GallerySouthStudyName = "South window study desks";
    const string GalleryWindowStudyDeskName = "Window study desk";
    const string GalleryCornerReadingName = "Shared reading table and west window counter";
    const string GalleryCornerTableName = "Corner shared reading table";
    const float GallerySouthStudyZ = -10.05f;
    static readonly float[] GallerySouthStudyCenters = { 3.25f, 9.95f };
    static readonly Vector3 GalleryCornerTablePosition = new Vector3(7.00f, 0f, GallerySouthStudyZ);
    static readonly Quaternion GallerySouthStudyRotation = Quaternion.Euler(0f, 90f, 0f);

    static void UpdateGallerySouthReading(Transform library, Material oak, Material dark,
        Material green, Material cream, Material[] covers)
    {
        var readers = library.Find(GalleryReadingFurnitureName);
        if (!readers) throw new InvalidOperationException("The gallery reading furniture is missing.");
        var corner = readers.Find("Southwest shared table and west window counter");
        if (corner) corner.name = GalleryCornerReadingName;
        else if (!readers.Find(GalleryCornerReadingName)) BuildGalleryCornerReading(readers, oak, dark, green, cream);
        AlignGallerySouthReading(library);
        BuildGallerySouthShelves(library, oak, dark, cream, covers);
    }

    static bool AlignGallerySouthReading(Transform library)
    {
        var readers = library.Find(GalleryReadingFurnitureName);
        var study = readers ? readers.Find(GallerySouthStudyName) : null;
        if (!study) throw new InvalidOperationException("The south window study desks are missing.");
        var desks = study.Cast<Transform>().Where(child => child.name == GalleryWindowStudyDeskName)
            .OrderBy(child => child.localPosition.x).ToArray();
        if (desks.Length != GallerySouthStudyCenters.Length)
            throw new InvalidOperationException("Expected two south window study desks.");

        var shared = readers.Find(GalleryCornerReadingName + "/" + GalleryCornerTableName);
        if (!shared) throw new InvalidOperationException("The shared reading table is missing.");

        bool changed = PlaceGallerySouthTable(shared, GalleryCornerTablePosition);
        for (int index = 0; index < desks.Length; index++)
            changed |= PlaceGallerySouthTable(desks[index], new Vector3(GallerySouthStudyCenters[index], 0f, GallerySouthStudyZ));
        return changed;
    }

    static bool PlaceGallerySouthTable(Transform table, Vector3 position)
    {
        if (table.localPosition == position && table.localRotation == GallerySouthStudyRotation) return false;
        table.SetLocalPositionAndRotation(position, GallerySouthStudyRotation);
        return true;
    }

    static void BuildGalleryLibrarySeating(Transform parent, Material oak, Material dark,
        Material green, Material cream, Material foliage)
    {
        var readers = Group(parent, GalleryReadingFurnitureName);
        var windowStudy = Group(readers, GallerySouthStudyName);
        var diffuser = AssetDatabase.LoadAssetAtPath<Material>(FitoutAssets + "/Office_Warm_Lamp_Lining.mat");
        foreach (float centerX in GallerySouthStudyCenters)
        {
            var desk = GalleryReadingTable(windowStudy, GalleryWindowStudyDeskName, centerX, GallerySouthStudyZ,
                3.60f, .82f, oak, dark);
            desk.localRotation = GallerySouthStudyRotation;
            foreach (float dividerX in new[] { -.60f, .60f })
            {
                OfficePadded(desk, "Low felt study divider", new Vector3(dividerX, .93f, -.05f),
                    new Vector3(.035f, .34f, .60f), green);
            }
            foreach (float x in new[] { -1.2f, 0f, 1.2f })
            {
                GalleryReaderChair(desk, x, .97f, 0, oak, green);
                GalleryReaderLamp(desk, x + .32f, -.20f, dark, green, diffuser ? diffuser : cream);
            }
            GalleryDeskBook(desk, new Vector3(-1.22f, GalleryDeskHeight, .13f), -9, true, green, cream);
            GalleryDeskBook(desk, new Vector3(.02f, GalleryDeskHeight, .11f), 12, false, dark, cream);
        }

        var shared = Group(readers, "North shared reading tables");
        foreach (float centerX in new[] { -1f, 4f })
        {
            var table = GalleryReadingTable(shared, "Four-person reading table", centerX, 8.65f, 2.48f, 1.28f, oak, dark);
            foreach (float x in new[] { -.63f, .63f })
            {
                GalleryReaderChair(table, x, 1.00f, 0, oak, green);
                GalleryReaderChair(table, x, -1.00f, 180, oak, green);
            }
            GalleryDeskBook(table, new Vector3(-.62f, GalleryDeskHeight, .32f), -8, true, green, cream);
            GalleryDeskBook(table, new Vector3(.50f, GalleryDeskHeight, -.32f), 164, false, dark, cream);
        }

        var lounge = Group(readers, "North window reading lounge");
        D(lounge, "Woven reading rug", new Vector3(9.83f, .025f, 10.56f),
            new Vector3(3.65f, .018f, 3.03f), cream);
        foreach (float x in new[] { 8.11f, 11.55f })
            D(lounge, "Woven end stripe", new Vector3(x, .035f, 10.56f), new Vector3(.053f, .002f, 2.98f), green);

        // Reuse the gallery's neighboring office upholstery, with the library fabric and oak palette.
        var previousDecor = decor;
        try
        {
            decor = lounge;
            UpperUpholsteredSeat(9.83f, 11.57f, 0, 1.98f, green, oak, cream);
            UpperUpholsteredSeat(8.63f, 9.82f, -121, .85f, green, oak, null);
            UpperUpholsteredSeat(11.07f, 9.82f, 121, .85f, green, oak, null);
        }
        finally { decor = previousDecor; }

        var coffee = GallerySeatObject(lounge, "Round lounge table", new Vector3(9.83f, 0, 10.43f));
        Shape(coffee, "Oak round top", PrimitiveType.Cylinder, Vector3.up * .444f, new Vector3(.84f, .026f, .84f), oak);
        Shape(coffee, "Dark pedestal", PrimitiveType.Cylinder, Vector3.up * .237f, new Vector3(.11f, .19f, .11f), dark);
        Shape(coffee, "Flared pedestal foot", PrimitiveType.Cylinder, Vector3.up * .049f, new Vector3(.46f, .024f, .46f), dark);
        Solid(coffee, Vector3.up * .247f, new Vector3(.78f, .45f, .78f));
        GalleryDeskBook(coffee, new Vector3(-.05f, .47f, .02f), 23, false, green, cream);

        var plant = GallerySeatObject(lounge, "Window planter", new Vector3(11.50f, 0, 11.75f));
        Shape(plant, "Ceramic planter", PrimitiveType.Cylinder, Vector3.up * .248f, new Vector3(.48f, .222f, .48f), cream);
        Shape(plant, "Soil", PrimitiveType.Cylinder, Vector3.up * .472f, new Vector3(.45f, .010f, .45f), dark);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * 137.5f;
            var rotation = Quaternion.Euler(0, angle, 0) * Quaternion.Euler(29 + 6 * (i % 3), 0, 0);
            var stem = new Vector3(0, .48f, 0) + rotation * Vector3.up * (.34f + .045f * (i % 3));
            var leaf = Shape(plant, "Broad upright leaf", PrimitiveType.Sphere, stem,
                new Vector3(.15f, .74f + .09f * (i % 3), .045f), foliage);
            leaf.transform.localRotation = rotation;
        }
        Solid(plant, Vector3.up * .25f, new Vector3(.48f, .50f, .48f));
        BuildGalleryCornerReading(readers, oak, dark, green, cream);
    }

    static void BuildGalleryCornerReading(Transform readers, Material oak, Material dark,
        Material green, Material cream)
    {
        var corners = Group(readers, GalleryCornerReadingName);
        var shared = GalleryReadingTable(corners, GalleryCornerTableName,
            GalleryCornerTablePosition.x, GalleryCornerTablePosition.z, 2.80f, 1.30f, oak, dark);
        shared.localRotation = GallerySouthStudyRotation;
        foreach (float x in new[] { -.83f, 0f, .83f })
        {
            GalleryReaderChair(shared, x, 1.03f, 0, oak, green);
            GalleryReaderChair(shared, x, -1.03f, 180, oak, green);
        }
        GalleryDeskBook(shared, new Vector3(-.56f, GalleryDeskHeight, .18f), -8f, true, green, cream);
        GalleryDeskBook(shared, new Vector3(.68f, GalleryDeskHeight, -.18f), 13f, false, dark, cream);

        var counter = GalleryReadingTable(corners, "West window reading counter",
            -17.28f, 8.55f, 3.45f, .53f, oak, dark);
        counter.localRotation = Quaternion.Euler(0f, 90f, 0f);
        counter.localScale = new Vector3(1f, 1.42f, 1f);
        foreach (float x in new[] { -1.09f, 0f, 1.09f })
            GalleryReaderChair(counter, x, .69f, 0f, oak, green);
        GalleryDeskBook(counter, new Vector3(-1.02f, GalleryDeskHeight, 0), 7f, false, green, cream);
    }

    static Transform GallerySeatObject(Transform parent, string name, Vector3 position, float yaw = 0)
    {
        var item = Group(parent, name);
        item.localPosition = position;
        item.localRotation = Quaternion.Euler(0, yaw, 0);
        return item;
    }

    static Transform GalleryReadingTable(Transform parent, string name, float x, float z,
        float width, float depth, Material oak, Material dark)
    {
        var table = GallerySeatObject(parent, name, new Vector3(x, 0, z));
        D(table, "Oak writing surface", new Vector3(0, GalleryDeskHeight - .029f, 0), new Vector3(width, .058f, depth), oak);
        D(table, "Recessed edge beneath top", new Vector3(0, .688f, 0), new Vector3(width - .08f, .030f, depth - .06f), dark);
        foreach (float legX in new[] { -width / 2 + .26f, width / 2 - .26f })
        {
            D(table, "Trestle crossbar", new Vector3(legX, .55f, 0),
                new Vector3(.067f, .09f, depth * .62f), oak);
            foreach (int side in new[] { -1, 1 })
            {
                var leg = D(table, "Splayed trestle leg", new Vector3(legX, .344f, side * (depth * .31f)),
                    new Vector3(.074f, .69f, .072f), oak);
                leg.transform.localRotation = Quaternion.Euler(-side * 8f, 0, 0);
            }
        }
        D(table, "Longitudinal trestle stretcher", new Vector3(0, .55f, 0), new Vector3(width - .45f, .094f, .065f), oak);
        Solid(table, new Vector3(0, GalleryDeskHeight / 2, 0), new Vector3(width, GalleryDeskHeight, depth));
        return table;
    }

    static void GalleryReaderChair(Transform table, float x, float z, float yaw, Material oak, Material fabric)
    {
        var chair = GallerySeatObject(table, "Oak reading chair", new Vector3(x, 0, z), yaw);
        OfficePadded(chair, "Seat cushion", Vector3.up * .456f, new Vector3(.48f, .075f, .46f), fabric);
        OfficePadded(chair, "Curved backrest", new Vector3(0, .75f, .202f),
            new Vector3(.47f, .235f, .052f), oak).localRotation = Quaternion.Euler(8, 0, 0);
        foreach (float dx in new[] { -.177f, .177f })
        {
            D(chair, "Front leg", new Vector3(dx, .217f, -.165f), new Vector3(.039f, .434f, .039f), oak);
            D(chair, "Backrest upright", new Vector3(dx, .365f, .178f), new Vector3(.040f, .73f, .040f), oak);
        }
        Solid(chair, Vector3.up * .434f, new Vector3(.48f, .868f, .46f));
    }

    static void GalleryReaderLamp(Transform desk, float x, float z, Material dark, Material green, Material diffuser)
    {
        var lamp = GallerySeatObject(desk, "Library task lamp", new Vector3(x, GalleryDeskHeight, z));
        Shape(lamp, "Oval weighted base", PrimitiveType.Cylinder, Vector3.up * .018f, new Vector3(.19f, .018f, .14f), dark);
        Shape(lamp, "Stem", PrimitiveType.Cylinder, new Vector3(0, .168f, -.030f), new Vector3(.022f, .145f, .022f), dark);
        D(lamp, "Shade arm", new Vector3(0, .301f, .026f), new Vector3(.03f, .025f, .137f), dark);
        OfficePadded(lamp, "Green banker's shade", new Vector3(0, .292f, .104f), new Vector3(.34f, .082f, .135f), green);
        OfficePadded(lamp, "Warm diffuser", new Vector3(0, .251f, .104f), new Vector3(.298f, .012f, .102f), diffuser);
    }

    static void GalleryDeskBook(Transform table, Vector3 position, float yaw, bool open, Material cover, Material paper)
    {
        var book = GallerySeatObject(table, open ? "Open reading book" : "Closed hardback", position, yaw);
        float width = open ? .36f : .182f;
        D(book, "Cloth cover", Vector3.up * .005f, new Vector3(width, .010f, .247f), cover);
        if (open)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var page = D(book, "Open page block", new Vector3(side * .086f, .023f, 0),
                    new Vector3(.170f, .025f, .238f), paper);
                page.transform.localRotation = Quaternion.Euler(0, 0, side * 4);
            }
        }
        else
        {
            D(book, "Page block", Vector3.up * .024f, new Vector3(.173f, .030f, .238f), paper);
            D(book, "Upper cover", Vector3.up * .042f, new Vector3(width, .008f, .247f), cover);
            D(book, "Spine", new Vector3(-.088f, .024f, 0), new Vector3(.010f, .035f, .247f), cover);
        }
    }
}
