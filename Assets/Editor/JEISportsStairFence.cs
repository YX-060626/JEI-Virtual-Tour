using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string FenceMeshPath = "Assets/JEI_GeneratedMeshes/Sports_Stair_Chain_Link.asset";
    const string FenceName = "Sports stair chain-link enclosure";
    const float FenceHeight = SportsClearHeight - RoofDeckThickness;
    const float FenceInnerX = 14.25f;
    const float FenceWidth = 3.30f;
    const float NorthLandingEnd = 3.15f;
    const float SouthLandingEnd = -3.15f;
    const float FlightEnd = 1.65f;
    const float HalfLanding = RoofStairStorey * .5f;
    const float Riser = .15f;
    const float OpeningGuardHeight = 1.10f;

    // The frame follows the tread nosings and landings. On the flight and north
    // opening, the wire runs through that frame all the way to the sports-floor slab.
    // The two south panels start higher so people can enter under the landing.
    static readonly (Vector3 railFrom, Vector3 railTo, float meshBottom)[] FencePanels =
    {
        (new Vector3(0, RoofStairStorey, -FlightEnd), new Vector3(0, HalfLanding + Riser, FlightEnd), 0f),
        (new Vector3(0, HalfLanding, FlightEnd), new Vector3(0, HalfLanding, NorthLandingEnd), 0f),
        (new Vector3(0, HalfLanding, NorthLandingEnd), new Vector3(FenceWidth, HalfLanding, NorthLandingEnd), 0f),
        (new Vector3(0, RoofStairStorey, SouthLandingEnd), new Vector3(0, RoofStairStorey, -FlightEnd), RoofStairStorey),
        (new Vector3(0, RoofStairStorey, SouthLandingEnd), new Vector3(FenceWidth, RoofStairStorey, SouthLandingEnd), RoofStairStorey)
    };

    public static void InstallSportsStairFences(Transform floor5)
    {
        var arrival = floor5.Find("04_Stairs__arrival_only");
        var lowerRun = floor5.Find(RoofTerraceStairsName + "/Sports hall to intermediate landing");
        if (!arrival || !lowerRun)
            throw new InvalidOperationException("The sports hall stair cores are missing.");

        var mesh = LoadSportsFenceMesh();
        var posts = LoadSportsAccessMaterial("Assets/JEI_Fitout/Gallery_Deep_Green.mat");
        var wire = LoadSportsAccessMaterial("Assets/JEI_UserInteriors/JEI_Furnishings/Materials/C04_Sage_Graphite_Metal.mat");
        foreach (int side in new[] { -1, 1 })
        {
            var guards = arrival.Find(side < 0 ? "West opening guards" : "East opening guards");
            var landing = lowerRun.Find(side < 0 ? "STAIR_A__WEST" : "STAIR_B__EAST");
            if (!guards || !landing) throw new InvalidOperationException("Missing sports stair opening guards.");

            // The mesh and simple panel colliders replace the solid guards on these edges.
            foreach (string name in new[] { "Inner stair opening guard", "North stair opening guard" })
            {
                var old = guards.Find(name);
                if (old) Object.DestroyImmediate(old.gameObject);
            }
            for (int i = landing.childCount - 1; i >= 0; i--)
            {
                var guard = landing.GetChild(i);
                bool innerEdge = guard.name == "Intermediate landing side guard" &&
                    Mathf.Abs(guard.localPosition.x) < 16f;
                if (innerEdge || guard.name == "Intermediate landing south guard")
                    Object.DestroyImmediate(guard.gameObject);
            }

            var fence = guards.Find(FenceName);
            if (!fence) fence = Group(guards, FenceName);
            fence.localPosition = Vector3.right * (side * FenceInnerX);
            const string netName = "Posts, tension rails and diamond wire";
            var net = fence.Find(netName);
            if (!net) net = Group(fence, netName);
            net.localScale = new Vector3(side, 1, 1);
            if (!net.TryGetComponent<MeshFilter>(out var filter)) filter = net.gameObject.AddComponent<MeshFilter>();
            if (!net.TryGetComponent<MeshRenderer>(out var renderer)) renderer = net.gameObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = new[] { posts, wire };

            // Each continuous wire panel has a matching collider, on a positive-scale anchor.
            var previous = fence.GetComponents<BoxCollider>();
            for (int i = 0; i < FencePanels.Length; i++)
            {
                var (from, to, bottom) = FencePanels[i];
                var collider = i < previous.Length ? previous[i] : fence.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(side * (from.x + to.x) * .5f, (bottom + FenceHeight) * .5f,
                    (from.z + to.z) * .5f);
                collider.size = new Vector3(Mathf.Max(.075f, Mathf.Abs(to.x - from.x)), FenceHeight - bottom,
                    Mathf.Max(.075f, Mathf.Abs(to.z - from.z)));
            }
            for (int i = FencePanels.Length; i < previous.Length; i++) Object.DestroyImmediate(previous[i]);
        }
    }

    static Mesh LoadSportsFenceMesh()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(FenceMeshPath);

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var frameTriangles = new List<int>();
        var wireTriangles = new List<int>();
        var postBases = new Dictionary<Vector2, float>();

        foreach (var (from, to, bottom) in FencePanels)
        {
            Vector3 planarEdge = new Vector3(to.x - from.x, 0, to.z - from.z);
            float length = planarEdge.magnitude;
            Vector3 axis = planarEdge / length;
            int bays = Mathf.CeilToInt(length / 2.4f);
            for (int i = 0; i <= bays; i++)
            {
                Vector3 point = i == bays ? to : Vector3.Lerp(from, to, (float)i / bays);
                var key = new Vector2(point.x, point.z);
                if (!postBases.TryGetValue(key, out float previousBase) || bottom < previousBase)
                    postBases[key] = bottom;
            }
            Tube(from + Vector3.up * .025f, to + Vector3.up * .025f, .025f, 8, frameTriangles);
            void LevelRail(float y, float radius = .025f)
            {
                Tube(new Vector3(from.x, y, from.z), new Vector3(to.x, y, to.z), radius, 8, frameTriangles);
            }
            LevelRail(FenceHeight - .025f);
            LevelRail((RoofStairStorey + FenceHeight) * .5f);
            if (bottom == 0f)
            {
                LevelRail(OpeningGuardHeight - .025f);
                // The floor slab is set back 50 mm from the landing edge.
                LevelRail(.025f, .065f);
            }

            // Use the wall coordinate so the diamonds continue across adjacent
            // coplanar bays as well as through the rail at stair height.
            const float pitch = .17f;
            foreach (float slope in new[] { -1.25f, 1.25f })
            {
                Vector3 depth = Vector3.Cross(axis, Vector3.up) * (Mathf.Sign(slope) * .004f);
                float phase = slope * Vector3.Dot(from, axis);
                int first = Mathf.CeilToInt((bottom - Mathf.Max(0, slope * length) - phase) / pitch);
                int last = Mathf.FloorToInt((FenceHeight - Mathf.Min(0, slope * length) - phase) / pitch);
                for (int i = first; i <= last; i++)
                {
                    float intercept = i * pitch + phase;
                    float atBase = (bottom - intercept) / slope;
                    float atTop = (FenceHeight - intercept) / slope;
                    float start = Mathf.Max(0, Mathf.Min(atBase, atTop));
                    float end = Mathf.Min(length, Mathf.Max(atBase, atTop));
                    if (end - start < .002f) continue;
                    Vector3 StrandPoint(float x) => new Vector3(from.x, intercept + slope * x, from.z) + axis * x + depth;
                    Tube(StrandPoint(start), StrandPoint(end), .0055f, 5, wireTriangles);
                }
            }
        }
        foreach (var post in postBases)
        {
            var key = post.Key;
            Tube(new Vector3(key.x, post.Value, key.y), new Vector3(key.x, FenceHeight, key.y), .041f, 10, frameTriangles);
        }

        // Update the mesh in place so existing renderers and saved scenes retain their asset reference.
        var mesh = existing ? existing : new Mesh { name = "Sports stair chain-link enclosure" };
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(frameTriangles, 0);
        mesh.SetTriangles(wireTriangles, 1);
        mesh.RecalculateBounds();
        if (existing) EditorUtility.SetDirty(mesh);
        else AssetDatabase.CreateAsset(mesh, FenceMeshPath);
        return mesh;

        void Tube(Vector3 from, Vector3 to, float radius, int sides, List<int> triangles)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 reference = Mathf.Abs(axis.y) > .95f ? Vector3.forward : Vector3.up;
            Vector3 u = Vector3.Cross(axis, reference).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            int first = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = 2 * Mathf.PI * i / sides;
                Vector3 normal = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
                vertices.Add(from + normal * radius);
                vertices.Add(to + normal * radius);
                normals.Add(normal);
                normals.Add(normal);
                int a = first + 2 * i;
                int b = first + 2 * ((i + 1) % sides);
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(b); triangles.Add(b + 1); triangles.Add(a + 1);
            }
        }
    }
}
