using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string GalleryLibraryName = "06_Library__Collections_and_Reading";
    const string GalleryExhibitName = "Campus projects and journals exhibit";
    const string GalleryNookName = "Atrium-facing relaxed reading seats";
    const string GalleryWindowBenchName = "Southeast window reading bench";

    public static bool InstallGalleryLibrary(Transform gallery)
    {
        if (!gallery || gallery.name != SportsGalleryName)
            throw new InvalidOperationException("The fourth-floor atrium gallery is required.");
        var existingFitout = gallery.Find("03_Gallery_Seating_and_Wayfinding");
        if (!existingFitout) throw new InvalidOperationException("The gallery furnishings are missing.");

        var library = gallery.Find(GalleryLibraryName);
        if (library && library.Find(GalleryNookName) && library.Find(GalleryWindowBenchName) &&
            library.Find(GallerySouthShelvingName + "/" + GallerySouthShelfExtensionName) && !library.Find(GalleryExhibitName))
        {
            bool aligned = AlignGallerySouthReading(library);
            if (aligned) EditorSceneManager.MarkSceneDirty(gallery.gameObject.scene);
            return RemoveGalleryBenchParts(existingFitout) || aligned;
        }

        var oak = FitMat("Oak_Furniture", new Color(.64f, .43f, .25f));
        var dark = FitMat("Gallery_Library_Dark", new Color(.16f, .13f, .11f));
        var green = FitMat("Gallery_Library_Green", new Color(.14f, .31f, .25f));
        var cream = FitMat("Gallery_Library_Cream", new Color(.90f, .84f, .70f));
        var covers = new[]
        {
            FitMat("Gallery_Library_Indigo", new Color(.21f, .30f, .47f)),
            FitMat("Gallery_Library_Rust", new Color(.57f, .27f, .18f)),
            FitMat("Gallery_Library_Ochre", new Color(.66f, .48f, .24f)),
            green,
        };
        if (library)
        {
            if (!library.Find(GallerySouthShelvingName))
                UpdateGallerySouthReading(library, oak, dark, green, cream, covers);
            else
                AlignGallerySouthReading(library);
            AddGallerySouthShelfExtension(library, oak, dark, cream, covers);
            AddGalleryNook(library, oak, dark, green, cream);
            AddGalleryWindowBench(library, oak, dark, green, cream);
            RemoveGalleryBenchParts(existingFitout);
            EditorSceneManager.MarkSceneDirty(gallery.gameObject.scene);
            return true;
        }

        var foliage = FitMat("Foliage", new Color(.18f, .42f, .22f));
        library = Group(gallery, GalleryLibraryName);
        try
        {
            BuildGalleryLibraryShelves(library, oak, dark, cream, covers);
            BuildGalleryLibrarySeating(library, oak, dark, green, cream, foliage);
            BuildGallerySouthShelves(library, oak, dark, cream, covers);
            AddGallerySouthShelfExtension(library, oak, dark, cream, covers);
            AddGalleryNook(library, oak, dark, green, cream);
            AddGalleryWindowBench(library, oak, dark, green, cream);
            D(library, "Collection wall sign", new Vector3(-16f, 2.41f, 3.32f),
                new Vector3(2.60f, .33f, .035f), dark);
            Label(library, "LIBRARY COLLECTION", new Vector3(-16f, 2.41f, 3.35f), .068f,
                new Color(.97f, .91f, .78f), 180f);
        }
        catch
        {
            Object.DestroyImmediate(library.gameObject);
            throw;
        }

        RemoveGalleryBenchParts(existingFitout);
        foreach (var sign in existingFitout.GetComponentsInChildren<TextMesh>())
        {
            if (sign.text == "04  ATRIUM GALLERY   |   05  SPORTS")
                sign.text = "04  READING GALLERY   |   05  SPORTS";
        }
        EditorSceneManager.MarkSceneDirty(gallery.gameObject.scene);
        return true;
    }

    static void AddGalleryWindowBench(Transform library, Material oak, Material dark, Material green, Material cream)
    {
        if (library.Find(GalleryWindowBenchName)) return;

        var bench = Group(library, GalleryWindowBenchName);
        try
        {
            BuildGalleryLibraryWindowBench(bench, oak, dark, green, cream);
        }
        catch
        {
            Object.DestroyImmediate(bench.gameObject);
            throw;
        }
    }

    static bool RemoveGalleryBenchParts(Transform existingFitout)
    {
        bool removed = false;
        for (int index = existingFitout.childCount - 1; index >= 0; index--)
        {
            var item = existingFitout.GetChild(index);
            if (item.name != "Oak gallery bench seat" && item.name != "Gallery bench base") continue;
            Object.DestroyImmediate(item.gameObject);
            removed = true;
        }
        if (removed) EditorSceneManager.MarkSceneDirty(existingFitout.gameObject.scene);
        return removed;
    }

    static void AddGalleryNook(Transform library, Material oak, Material dark, Material green, Material cream)
    {
        var previous = library.Find(GalleryExhibitName);
        if (!library.Find(GalleryNookName))
        {
            var nook = Group(library, GalleryNookName);
            nook.localPosition = previous ? previous.localPosition : new Vector3(-9.10f, 0f, 1.60f);
            try
            {
                BuildGalleryLibraryNook(nook, oak, dark, green, cream);
            }
            catch
            {
                Object.DestroyImmediate(nook.gameObject);
                throw;
            }
        }
        if (previous) Object.DestroyImmediate(previous.gameObject);
    }
}
