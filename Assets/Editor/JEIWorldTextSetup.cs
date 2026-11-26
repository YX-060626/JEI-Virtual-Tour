using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class JEIStructureBlockout
{
    const string WorldTextMaterialPath = "Assets/JEI_UserInteriors/JEI_Furnishings/Materials/Depth_Tested_Font.mat";
    static Material worldTextMaterial;

    static Material WorldTextDepthMaterial()
    {
        if (worldTextMaterial) return worldTextMaterial;
        worldTextMaterial = AssetDatabase.LoadAssetAtPath<Material>(WorldTextMaterialPath);
        if (!worldTextMaterial || !worldTextMaterial.mainTexture)
            throw new InvalidOperationException("The tour's depth-tested material or its legacy font atlas is missing.");
        return worldTextMaterial;
    }

    static void AssignWorldTextDepth(TextMesh text)
    {
        text.GetComponent<MeshRenderer>().sharedMaterial = WorldTextDepthMaterial();
    }

    static void ApplyWorldTextDepth(Scene scene)
    {
        // Keep custom materials and the selected fonts on imported signs.
        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Material overlayMaterial = legacyFont.material;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (TextMesh text in root.GetComponentsInChildren<TextMesh>(true))
            {
                if (text.font == legacyFont && text.GetComponent<MeshRenderer>().sharedMaterial == overlayMaterial)
                    AssignWorldTextDepth(text);
            }
    }
}
