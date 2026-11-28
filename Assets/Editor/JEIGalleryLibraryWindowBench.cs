using UnityEngine;

public static partial class JEIStructureBlockout
{
    static void BuildGalleryLibraryWindowBench(Transform parent, Material oak, Material dark, Material green, Material cream)
    {
        var upholstery = GalleryNookSurface("Gallery_Library_Nook_Cushion", new Color(.20f, .38f, .30f), .10f);
        BuildGalleryWindowBenchRun(parent, "Long south-window seat", new Vector3(15.25f, 0f, -12.265f),
            180f, 4.83f, 4, oak, dark, green, cream, upholstery);
        BuildGalleryWindowBenchRun(parent, "Short east-window return", new Vector3(17.27f, 0f, -10.788f),
            90f, 2.15f, 2, oak, dark, green, cream, upholstery);
        BuildGalleryWindowBenchTable(parent, 14.11f, -11.09f, true, oak, dark, green, cream);
        BuildGalleryWindowBenchTable(parent, 16.18f, -10.71f, false, oak, dark, green, cream);
    }

    static void BuildGalleryWindowBenchRun(Transform parent, string name, Vector3 position, float yaw,
        float length, int count, Material oak, Material dark, Material green, Material cream, Material upholstery)
    {
        const float depth = .77f;
        const float height = .835f;
        var run = GallerySeatObject(parent, name, position, yaw);
        D(run, "Recessed base shadow line", Vector3.up * .060f,
            new Vector3(length - .10f, .12f, depth - .10f), dark);
        D(run, "Continuous warm oak bench body", Vector3.up * .228f, new Vector3(length, .30f, depth), oak);
        D(run, "Oak top edge", Vector3.up * .384f, new Vector3(length, .045f, depth), oak);

        float span = (length - .12f) / count;
        for (int i = 0; i < count; i++)
        {
            float x = (i + .5f - count / 2f) * span;
            OfficePadded(run, "Soft deep-green seat cushion", new Vector3(x, .462f, -.055f),
                new Vector3(span - .019f, .115f, .635f), upholstery);
            OfficePadded(run, "Low gently reclined window-side back", new Vector3(x, .653f, .278f),
                new Vector3(span - .023f, .346f, .127f), green).localRotation = Quaternion.Euler(10f, 0f, 0f);
        }
        foreach (float x in new[] { -length / 2 + .32f, length / 2 - .32f })
            OfficePadded(run, "Natural linen cushion", new Vector3(x, .596f, .181f),
                new Vector3(.32f, .257f, .118f), cream).localRotation = Quaternion.Euler(9f, 0f, x > 0 ? -12f : 11f);

        Solid(run, Vector3.up * (height / 2f), new Vector3(length, height, depth));
    }

    static void BuildGalleryWindowBenchTable(Transform parent, float x, float z, bool open,
        Material oak, Material dark, Material green, Material cream)
    {
        var table = GallerySeatObject(parent, "Small oak side table", new Vector3(x, 0f, z));
        var top = Shape(table, "Rounded oak top", PrimitiveType.Cylinder, Vector3.up * .477f,
            new Vector3(.53f, .025f, .53f), oak);
        top.AddComponent<MeshCollider>().sharedMesh = top.GetComponent<MeshFilter>().sharedMesh;
        Shape(table, "Dark pedestal", PrimitiveType.Cylinder, Vector3.up * .249f,
            new Vector3(.10f, .204f, .10f), dark);
        Shape(table, "Low circular foot", PrimitiveType.Cylinder, Vector3.up * .051f,
            new Vector3(.34f, .020f, .34f), dark);
        GalleryDeskBook(table, Vector3.up * .502f, open ? -5f : 12f, open, green, cream);
    }
}
