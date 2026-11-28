using UnityEditor;
using UnityEngine;

public static partial class JEIStructureBlockout
{
    static void BuildGalleryLibraryNook(Transform nook, Material oak, Material dark, Material green, Material cream)
    {
        var cushion = GalleryNookSurface("Gallery_Library_Nook_Cushion", new Color(.20f, .38f, .30f), .10f);
        var rug = GalleryNookSurface("Gallery_Library_Nook_Rug", new Color(.72f, .65f, .55f), .01f);
        var south = GallerySeatObject(nook, "Atrium lounge pair 01", new Vector3(0f, 0f, -2.65f), -90f);
        var north = GallerySeatObject(nook, "Atrium lounge pair 02", new Vector3(0f, 0f, .50f), -90f);
        BuildGalleryNookPair(south, oak, dark, green, cream, cushion, rug, true);
        BuildGalleryNookPair(north, oak, dark, green, cream, cushion, rug, false);
    }

    static Material GalleryNookSurface(string name, Color tint, float smoothness)
    {
        var material = FitMat(name, tint);
        if (!Mathf.Approximately(material.GetFloat("_Smoothness"), smoothness))
        {
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    static void BuildGalleryNookPair(Transform pair, Material oak, Material dark, Material green, Material cream,
        Material cushion, Material rug, bool openBook)
    {
        D(pair, "Small woven rug", Vector3.up * .009f, new Vector3(2.49f, .018f, 2.28f), rug);
        foreach (int side in new[] { -1, 1 })
        {
            D(pair, "Rug side binding", new Vector3(side * 1.18f, .019f, 0f),
                new Vector3(.025f, .002f, 2.17f), cream);
            D(pair, "Rug end binding", new Vector3(0f, .019f, side * 1.08f),
                new Vector3(2.36f, .002f, .025f), cream);
            BuildGalleryNookChair(pair, side, oak, green, cushion);
        }

        var table = GallerySeatObject(pair, "Low round oak table", new Vector3(0f, 0f, -.46f));
        var top = Shape(table, "Round oak top", PrimitiveType.Cylinder, Vector3.up * .401f,
            new Vector3(.59f, .023f, .59f), oak);
        top.AddComponent<MeshCollider>().sharedMesh = top.GetComponent<MeshFilter>().sharedMesh;
        Shape(table, "Dark pedestal", PrimitiveType.Cylinder, Vector3.up * .21f,
            new Vector3(.115f, .173f, .115f), dark);
        Shape(table, "Low circular foot", PrimitiveType.Cylinder, Vector3.up * .033f,
            new Vector3(.33f, .016f, .33f), dark);
        GalleryDeskBook(table, new Vector3(0f, .424f, 0f), 12f, openBook, green, cream);
    }

    static void BuildGalleryNookChair(Transform pair, int side, Material oak, Material green, Material cushion)
    {
        // Chair fronts point down local Z. The pair's turn points them toward the atrium.
        var chair = GallerySeatObject(pair, "Low reclined one-person lounge chair",
            new Vector3(side * .61f, 0f, .48f), side * 13f);
        OfficePadded(chair, "Rounded upholstered base", new Vector3(0f, .273f, 0f),
            new Vector3(.85f, .182f, .75f), green);
        OfficePadded(chair, "Thick pitched seat cushion", new Vector3(0f, .408f, -.068f),
            new Vector3(.655f, .157f, .645f), cushion).localRotation = Quaternion.Euler(6f, 0f, 0f);
        OfficePadded(chair, "Reclined back shell", new Vector3(0f, .658f, .326f),
            new Vector3(.81f, .644f, .186f), green).localRotation = Quaternion.Euler(20f, 0f, 0f);
        OfficePadded(chair, "Soft back cushion", new Vector3(0f, .661f, .238f),
            new Vector3(.676f, .54f, .134f), cushion).localRotation = Quaternion.Euler(20f, 0f, 0f);
        foreach (int edge in new[] { -1, 1 })
        {
            OfficePadded(chair, "Low rounded arm", new Vector3(edge * .368f, .455f, -.03f),
                new Vector3(.133f, .208f, .672f), green).localRotation = Quaternion.Euler(5f, 0f, 0f);
            foreach (int end in new[] { -1, 1 })
            {
                var foot = D(chair, "Splayed oak foot", new Vector3(edge * .293f, .12f, end * .245f),
                    new Vector3(.059f, .20f, .061f), oak);
                foot.transform.localRotation = Quaternion.Euler(-end * 8f, 0f, edge * 8f);
            }
        }
        Solid(chair, new Vector3(0f, .50f, .066f), new Vector3(.876f, 1f, .932f));
    }
}
