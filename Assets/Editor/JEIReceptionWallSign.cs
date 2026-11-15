using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string AcademySignFace = "Academy sign printed oak face";

    static void UpdateAcademyWallSign(Transform firstFloor)
    {
        var reception = firstFloor.Find("06_Approved_Interiors/S02_Minimal_Reception");
        var backing = reception ? reception.Find("Academy identity backing") : null;
        if (!backing) throw new InvalidOperationException("The original JEI ACADEMY wall sign is missing.");

        var art = AssetDatabase.LoadAssetAtPath<Texture2D>(FitoutAssets + "/Academy_Wall_Sign.png");
        if (!art) throw new InvalidOperationException("The oak academy sign artwork is missing.");
        var print = RoomPlaqueMaterial("Academy_Wall_Sign_Print");
        print.SetTexture("_BaseMap", art);
        print.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(print);
        backing.GetComponent<MeshRenderer>().sharedMaterial = FitMat("Academy_Oak_Edge", new Color(.52f, .32f, .16f));

        foreach (var label in reception.GetComponentsInChildren<TextMesh>(true).Where(IsOldAcademySignText))
            label.gameObject.SetActive(false);

        var existingFace = reception.Find(AcademySignFace);
        if (existingFace) Object.DestroyImmediate(existingFace.gameObject);
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = AcademySignFace;
        face.transform.SetParent(reception, false);
        face.transform.localPosition = backing.localPosition + Vector3.right * (backing.localScale.x * .5f + .001f);
        face.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        face.transform.localScale = new Vector3(backing.localScale.z, backing.localScale.y, 1f);
        Object.DestroyImmediate(face.GetComponent<Collider>());
        var renderer = face.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = print;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static bool IsOldAcademySignText(TextMesh label) =>
        label.text == "JEI  ACADEMY" || label.text == "LEARN  /  CREATE  /  CONNECT";

    static void VerifyEnglishTourText(JEIStructureView view)
    {
        var root = view.floors[0].parent;
        foreach (var label in root.GetComponentsInChildren<TextMesh>())
            if (label.text.Any(character => character >= '\u3400' && character <= '\u9fff'))
                throw new InvalidOperationException("Chinese text remains visible in the tour: " + label.name);

        var reception = view.floors[0].Find("06_Approved_Interiors/S02_Minimal_Reception");
        var face = reception ? reception.Find(AcademySignFace) : null;
        if (!face || !face.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BaseMap") ||
            reception.GetComponentsInChildren<TextMesh>().Any(IsOldAcademySignText))
            throw new InvalidOperationException("The academy sign must use its applied oak print without duplicate text.");
    }
}
