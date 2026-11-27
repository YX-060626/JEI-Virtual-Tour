using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class JEIStairEdgeRepair
{
    const float TreadOverhang = .001f;

    public static int Apply(Scene scene)
    {
        int changedFlights = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform stair in root.GetComponentsInChildren<Transform>(true))
        {
            if (stair.name == "STAIR_A__WEST" || stair.name == "STAIR_B__EAST")
                changedFlights += FitStair(stair);
        }

        if (changedFlights != 0) EditorSceneManager.MarkSceneDirty(scene);
        return changedFlights;
    }

    internal static int FitStair(Transform stair)
    {
        return FitFlight(stair, "Lower flight structural slab", "Lower flight__riser_1")
             + FitFlight(stair, "Upper flight structural slab", "Upper flight__riser_13");
    }

    static int FitFlight(Transform stair, string slabName, string firstTreadName)
    {
        Transform slab = stair.Find(slabName);
        Transform tread = stair.Find(firstTreadName);
        if (!slab || !tread) return 0;

        Vector3 normal = slab.localRotation * Vector3.up;
        Vector3 uphill = slab.localRotation * Vector3.forward;
        Vector3 oldScale = slab.localScale;
        Vector3 soffit = slab.localPosition - normal * (oldScale.y * .5f);

        // The support and the treads share the same pitch. Meeting the first
        // tread's downhill underside closes all the gaps with the soffit fixed.
        Vector3 contact = tread.localPosition;
        contact.y -= tread.localScale.y * .5f;
        contact.z -= Mathf.Sign(uphill.z) * tread.localScale.z * .5f;
        Vector3 scale = oldScale;
        scale.y = Vector3.Dot(contact - soffit, normal);
        scale.x = tread.localScale.x - 2f * TreadOverhang;
        if (Mathf.Approximately(scale.x, oldScale.x) && Mathf.Approximately(scale.y, oldScale.y))
            return 0;

        // A small tread overhang keeps the two visible side faces off the same plane.
        Undo.RecordObject(slab, "Fit stair flight slab");
        slab.localScale = scale;
        slab.localPosition = soffit + normal * (scale.y * .5f);
        return 1;
    }
}
