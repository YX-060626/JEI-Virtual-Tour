using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string GallerySouthShelvingName = "Southwest library shelving";
    const string GallerySouthShelfExtensionName = "Three additional south collection rows";

    static void BuildGalleryLibraryShelves(Transform parent, Material oak, Material dark, Material cream, Material[] covers)
    {
        var shelving = Group(parent, "Library shelving and displays");
        var books = new GalleryLibraryBooks(shelving, covers, cream, dark, FitoutAssets + "/Gallery_Library_Books.asset");
        float[] rowCenters = { -11.88f, -9.86f, -7.84f, -5.82f, -3.80f };
        for (int i = 0; i < rowCenters.Length; i++)
            BuildGalleryShelfRow(shelving, i + 1, rowCenters[i], 8.90f, oak, dark, books);

        var wallCase = Group(shelving, "North-facing stair wall bookcase");
        wallCase.localPosition = new Vector3(-16.00f, 0f, 3.53f);
        BuildGalleryWallBookcase(wallCase, oak, dark, books);

        var display = Group(shelving, "Face-out featured book display");
        display.localPosition = new Vector3(-12.15f, 0f, 6.05f);
        display.localRotation = Quaternion.Euler(0f, 180f, 0f);
        BuildGalleryFeaturedBooks(display, oak, dark, books);
        books.Save();
    }

    static void BuildGallerySouthShelves(Transform library, Material oak, Material dark, Material cream, Material[] covers)
    {
        BuildGallerySouthShelfGroup(library, GallerySouthShelvingName, "Gallery_Library_South_Books.asset",
            6, -10.90f, oak, dark, cream, covers);
    }

    static void AddGallerySouthShelfExtension(Transform library, Material oak, Material dark, Material cream, Material[] covers)
    {
        var shelving = library.Find(GallerySouthShelvingName);
        if (shelving.Find(GallerySouthShelfExtensionName)) return;
        BuildGallerySouthShelfGroup(shelving, GallerySouthShelfExtensionName, "Gallery_Library_South_Extension_Books.asset",
            9, -4.84f, oak, dark, cream, covers);
    }

    static void BuildGallerySouthShelfGroup(Transform parent, string groupName, string bookFile, int firstRow, float firstX,
        Material oak, Material dark, Material cream, Material[] covers)
    {
        var shelving = Group(parent, groupName);
        try
        {
            var books = new GalleryLibraryBooks(shelving, covers, cream, dark, FitoutAssets + "/" + bookFile);
            for (int i = 0; i < 3; i++)
                BuildGalleryShelfRow(shelving, firstRow + i, firstX + i * 2.02f, -9.10f, oak, dark, books);
            books.Save();
        }
        catch
        {
            Object.DestroyImmediate(shelving.gameObject);
            throw;
        }
    }

    static void BuildGalleryShelfRow(Transform shelving, int number, float x, float z, Material oak, Material dark,
        GalleryLibraryBooks books)
    {
        var row = Group(shelving, $"Double-sided library shelf {number:00}");
        row.localPosition = new Vector3(x, 0f, z);
        row.localRotation = Quaternion.Euler(0f, 90f, 0f);

        const float length = 3.60f;
        const float depth = .70f;
        const float height = 1.85f;
        const float uprightHeight = height - .20f;
        const float uprightCenter = .156f + uprightHeight / 2;
        const float bayWidth = (length - .10f) / 3;
        D(row, "Recessed dark plinth", new Vector3(0, .078f, 0),
            new Vector3(length - .10f, .156f, depth - .10f), dark);
        D(row, "Central timber backing", new Vector3(0, uprightCenter, 0),
            new Vector3(length - .06f, uprightHeight, .045f), dark);
        D(row, "Oak crown", new Vector3(0, height - .022f, 0), new Vector3(length, .044f, depth), oak);
        foreach (int side in new[] { -1, 1 })
        {
            D(row, "Oak end panel", new Vector3(side * (length - .05f) / 2, uprightCenter, 0),
                new Vector3(.05f, uprightHeight, depth), oak);
            D(row, "Oak bay divider", new Vector3(side * bayWidth / 2, uprightCenter, 0),
                new Vector3(.036f, uprightHeight, depth - .036f), oak);
        }

        foreach (float top in new[] { .194f, .596f, .998f, 1.400f })
        {
            D(row, "Horizontal oak shelf", new Vector3(0, top - .020f, 0),
                new Vector3(length - .10f, .04f, depth - .012f), oak);
            foreach (int face in new[] { -1, 1 })
            {
                D(row, "Recessed shelf edge trim", new Vector3(0, top - .023f, face * (depth / 2 - .003f)),
                    new Vector3(length - .10f, .015f, .006f), dark);
                foreach (float bay in new[] { -bayWidth, 0f, bayWidth })
                    books.AddRun(row, bay, bayWidth - .10f, top, depth / 2 - .028f, face);
            }
        }
        Solid(row, Vector3.up * (height / 2), new Vector3(length, height, depth));
    }

    static void BuildGalleryWallBookcase(Transform bookcase, Material oak, Material dark, GalleryLibraryBooks books)
    {
        const float width = 2.60f;
        const float depth = .42f;
        const float height = 2.12f;
        D(bookcase, "Recessed kickboard", new Vector3(0, .061f, 0), new Vector3(width - .10f, .122f, .34f), dark);
        D(bookcase, "Oak top", new Vector3(0, height - .024f, 0), new Vector3(width, .048f, depth), oak);
        D(bookcase, "Dark bookcase back", new Vector3(0, 1.097f, -.190f),
            new Vector3(width, 1.950f, .04f), dark);
        foreach (float x in new[] { -1.275f, 1.275f })
            D(bookcase, "Oak side", new Vector3(x, 1.097f, 0), new Vector3(.05f, 1.950f, depth), oak);
        foreach (float x in new[] { -.417f, .417f })
            D(bookcase, "Oak bay divider", new Vector3(x, 1.11f, .014f), new Vector3(.032f, 1.92f, .362f), oak);

        foreach (float top in new[] { .548f, .933f, 1.318f, 1.703f })
        {
            D(bookcase, "Horizontal oak shelf", new Vector3(0, top - .019f, .017f),
                new Vector3(width - .10f, .038f, .386f), oak);
            foreach (float x in new[] { -.834f, 0f, .834f })
                books.AddRun(bookcase, x, .75f, top, .187f, 1);
        }

        foreach (float x in new[] { -.834f, 0f, .834f })
        {
            D(bookcase, "Closed oak storage door", new Vector3(x, .313f, .180f), new Vector3(.804f, .382f, .042f), oak);
            D(bookcase, "Dark storage pull", new Vector3(x, .440f, .204f), new Vector3(.112f, .015f, .012f), dark);
        }
        Solid(bookcase, Vector3.up * (height / 2), new Vector3(width, height, depth));
    }

    static void BuildGalleryFeaturedBooks(Transform display, Material oak, Material dark, GalleryLibraryBooks books)
    {
        const float width = 1.36f;
        D(display, "Recessed dark display plinth", new Vector3(0, .054f, 0), new Vector3(1.25f, .108f, .47f), dark);
        D(display, "Oak display base", new Vector3(0, .321f, 0), new Vector3(width, .442f, .56f), oak);
        D(display, "Rear oak riser", new Vector3(0, .658f, -.155f), new Vector3(width, .232f, .25f), oak);
        D(display, "Front dark retaining ledge", new Vector3(0, .555f, .249f),
            new Vector3(1.29f, .036f, .026f), dark);
        D(display, "Rear dark retaining ledge", new Vector3(0, .785f, -.032f),
            new Vector3(1.29f, .033f, .026f), dark);
        foreach (float x in new[] { -.44f, -.145f, .16f, .445f })
        {
            books.AddFaceOut(display, new Vector3(x, .542f, .212f), .174f, .218f);
            books.AddFaceOut(display, new Vector3(x + .018f, .775f, -.069f), .177f, .240f);
        }
        Solid(display, new Vector3(0, .272f, 0), new Vector3(width, .544f, .56f));
        Solid(display, new Vector3(0, .656f, -.155f), new Vector3(width, .224f, .25f));
    }

    sealed class GalleryLibraryBooks
    {
        readonly Transform root;
        readonly Mesh cube;
        readonly Material[] covers;
        readonly Material paper;
        readonly Material dark;
        readonly string meshAssetPath;
        readonly System.Random variety = new System.Random(48116);
        readonly Dictionary<Material, List<CombineInstance>> parts = new Dictionary<Material, List<CombineInstance>>();

        public GalleryLibraryBooks(Transform root, Material[] covers, Material paper, Material dark, string meshAssetPath)
        {
            this.root = root;
            this.covers = covers;
            this.paper = paper;
            this.dark = dark;
            this.meshAssetPath = meshAssetPath;
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
        }

        public void AddRun(Transform shelf, float center, float span, float y, float spineZ, int face)
        {
            int count = variety.Next(4, 6);
            float cursor = center - span / 2 + Range(.015f, .12f);
            bool stackedBooks = variety.Next(5) == 0;
            float spineLimit = center + span / 2 - (stackedBooks ? .265f : .025f);
            var rotation = Quaternion.Euler(0f, face > 0 ? 0f : 180f, 0f);
            for (int i = 0; i < count; i++)
            {
                float width = Range(.052f, .109f);
                if (cursor + width > spineLimit) break;
                float height = Range(.212f, .333f);
                float depth = Range(.162f, .223f);
                var location = new Vector3(cursor + width / 2, y, face * (spineZ - Range(0f, .024f)));
                AddHardback(Pose(shelf, location, rotation), width, height, depth, PickCover());
                cursor += width + Range(.006f, .017f);
            }

            if (!stackedBooks) return;
            const float thickness = .047f;
            for (int i = 0; i < 2; i++)
            {
                float spanX = i == 0 ? .225f : .195f;
                float x = center + span / 2 - .025f - (face > 0 ? 0f : spanX);
                var location = new Vector3(x, y + thickness * (i + .5f), face * (spineZ - .012f - i * .016f));
                AddHardback(Pose(shelf, location, rotation * Quaternion.Euler(0f, 0f, 90f)),
                    thickness, spanX, .157f - i * .012f, PickCover());
            }
        }

        public void AddFaceOut(Transform display, Vector3 location, float width, float height)
        {
            var pose = Pose(display, location, Quaternion.Euler(-12f, 0f, 0f));
            var cover = PickCover();
            AddCube(pose, new Vector3(0, height / 2, -.021f), new Vector3(width - .007f, height - .007f, .026f), paper);
            foreach (float z in new[] { 0f, -.036f })
                AddCube(pose, new Vector3(0, height / 2, z), new Vector3(width, height, .008f), cover);
            AddCube(pose, new Vector3(-width / 2 + .006f, height / 2, -.017f), new Vector3(.012f, height, .038f), cover);
            AddCube(pose, new Vector3(.008f, height * .715f, .0049f), new Vector3(width * .65f, .013f, .002f), paper);
            AddCube(pose, new Vector3(.008f, height * .610f, .0049f), new Vector3(width * .43f, .006f, .002f), paper);
            AddCube(pose, new Vector3(.008f, height * .295f, .0049f), new Vector3(width * .64f, .059f, .002f), dark);
        }

        void AddHardback(Matrix4x4 pose, float thickness, float height, float depth, Material cover)
        {
            AddCube(pose, new Vector3(0, height / 2, -.009f), new Vector3(thickness, height, .018f), cover);
            AddCube(pose, new Vector3(0, height / 2, -.5f * depth - .006f),
                new Vector3(thickness - .007f, height - .009f, depth - .020f), paper);
            foreach (int edge in new[] { -1, 1 })
                AddCube(pose, new Vector3(edge * (thickness / 2 - .002f), height / 2, -.5f * depth - .004f),
                    new Vector3(.004f, height, depth - .008f), cover);
            if (variety.Next(3) != 0)
            {
                AddCube(pose, new Vector3(0, height * .720f, .001f), new Vector3(thickness * .58f, .006f, .002f), paper);
                AddCube(pose, new Vector3(0, height * .205f, .001f), new Vector3(thickness * .72f, .005f, .002f), paper);
            }
        }

        Matrix4x4 Pose(Transform shelf, Vector3 position, Quaternion rotation)
        {
            return root.worldToLocalMatrix * shelf.localToWorldMatrix * Matrix4x4.TRS(position, rotation, Vector3.one);
        }

        void AddCube(Matrix4x4 book, Vector3 center, Vector3 size, Material material)
        {
            if (!parts.TryGetValue(material, out var batch))
            {
                batch = new List<CombineInstance>();
                parts.Add(material, batch);
            }
            batch.Add(new CombineInstance
            {
                mesh = cube,
                transform = book * Matrix4x4.TRS(center, Quaternion.identity, size)
            });
        }

        Material PickCover() => covers[variety.Next(covers.Length)];

        float Range(float min, float max) => Mathf.Lerp(min, max, (float)variety.NextDouble());

        public void Save()
        {
            var materials = new List<Material>(parts.Count);
            var subMeshes = new List<CombineInstance>(parts.Count);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            var baked = new Mesh { name = "Gallery library books", indexFormat = IndexFormat.UInt32 };
            try
            {
                foreach (var batch in parts)
                {
                    var colorMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                    subMeshes.Add(new CombineInstance { mesh = colorMesh, transform = Matrix4x4.identity });
                    colorMesh.CombineMeshes(batch.Value.ToArray(), true, true);
                    materials.Add(batch.Key);
                }
                baked.CombineMeshes(subMeshes.ToArray(), false, false);
                baked.RecalculateBounds();
                if (mesh)
                {
                    EditorUtility.CopySerialized(baked, mesh);
                    EditorUtility.SetDirty(mesh);
                }
                else
                {
                    AssetDatabase.CreateAsset(baked, meshAssetPath);
                    mesh = baked;
                }
            }
            finally
            {
                foreach (var subMesh in subMeshes) Object.DestroyImmediate(subMesh.mesh);
                if (!AssetDatabase.Contains(baked)) Object.DestroyImmediate(baked);
            }

            var collection = Group(root, "Modeled books and page blocks");
            collection.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            collection.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
        }
    }
}
