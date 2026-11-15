using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string PlaqueGroupName = "07_Room_Number_Plaques";
    const string PlaquePrintMaterialName = "Room_Plaque_Print";
    const string PlaquePrintTexturePath = FitoutAssets + "/Room_Plaque_Print.png";
    const float PlaqueHeight = .25f;
    const float PlaqueProjection = .215f;
    const float PlaqueMaxWidth = .52f;
    const int PlaqueColumns = 8;
    const int PlaqueRows = 8;

    sealed class PlaqueMount
    {
        public string RoomId, RoomName, Number;
        public int Floor, AtlasIndex;
        public float Width;
        public Vector3 Position;
        public Quaternion Rotation;
        public string RestroomLabel;
        public string DisplayName => RestroomLabel ?? RoomName;
        public string DisplayNumber => RestroomLabel == null ? Number : string.Empty;
        public string ObjectName => (RestroomLabel == null ? Number : RoomId) + "__" + DisplayName;
    }

    static void RebuildRoomPlaques(JEIStructureView view)
    {
        var mounts = ReadPlaqueMounts();
        var print = RoomPlaqueMaterial(PlaquePrintMaterialName);
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaquePrintTexturePath);
        if (!atlas) throw new InvalidOperationException("Missing room plaque artwork: " + PlaquePrintTexturePath);
        print.SetTexture("_BaseMap", atlas);
        print.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(print);

        var frameMaterial = RoomPlaqueMaterial("Room_Plaque_Edge");
        frameMaterial.SetColor("_BaseColor", new Color32(215, 185, 145, 255));
        EditorUtility.SetDirty(frameMaterial);

        for (int floorNumber = 1; floorNumber <= 3; floorNumber++)
        {
            var floor = view.floors[floorNumber - 1];
            var oldGroup = floor.Find(PlaqueGroupName);
            if (oldGroup) Object.DestroyImmediate(oldGroup.gameObject);

            var group = Group(floor, PlaqueGroupName);
            foreach (var mount in mounts.Where(m => m.Floor == floorNumber))
            {
                var plaque = Group(group, mount.ObjectName);
                plaque.localPosition = mount.Position;
                plaque.localRotation = mount.Rotation;
                plaque.gameObject.AddComponent<MeshFilter>().sharedMesh = SavePlaqueMesh(mount);
                var renderer = plaque.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { print, frameMaterial };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            UpdateRestroomSignage(floor);
        }

        HideImportedLintelPlaques(view.floors[0]);

        Directory.CreateDirectory(FitoutDocs);
        var entries = mounts.Select(m => $"{m.Floor},{m.DisplayNumber},{m.RoomId},{m.DisplayName}");
        File.WriteAllLines(FitoutDocs + "/Room_Numbers.csv", new[] { "楼层,编号,原始ID,名称" }.Concat(entries), System.Text.Encoding.UTF8);
    }

    static void HideImportedLintelPlaques(Transform firstFloor)
    {
        var interiors = firstFloor.Find("06_Approved_Interiors");
        if (!interiors) return;

        // Each imported lintel plaque has a separate text object in front of the backing.
        // Match its sibling and mounting position to preserve signs inside the rooms.
        foreach (var backing in interiors.GetComponentsInChildren<Transform>(true)
                     .Where(item => item.name == "Room identification plaque"))
        {
            backing.gameObject.SetActive(false);
            foreach (Transform sibling in backing.parent)
            {
                if (sibling.name.StartsWith("Sign - ", StringComparison.Ordinal) &&
                    sibling.GetComponent<TextMesh>() &&
                    Vector3.Distance(sibling.localPosition, backing.localPosition) < .035f)
                    sibling.gameObject.SetActive(false);
            }
        }
    }

    static void UpdateRestroomSignage(Transform floor)
    {
        // The old flat corridor plate overlaps the new numbered plaque on the women's wall.
        // Keep the matching interior plates, fittings, and pictograms in each restroom.
        foreach (var item in floor.GetComponentsInChildren<Transform>(true))
        {
            if (item.name == "C02_Corridor_room_identification" || item.name == "C03_Corridor_room_identification")
                item.gameObject.SetActive(false);
        }

        foreach (var label in floor.GetComponentsInChildren<TextMesh>(true).Where(IsCubicleNumber))
            label.gameObject.SetActive(false);
    }

    static bool IsCubicleNumber(TextMesh label)
    {
        if (label.text != "01" && label.text != "02" && label.text != "03") return false;
        var front = label.transform.parent;
        var cubicle = front ? front.parent : null;
        return cubicle && front.name == "Front_posts_and_header" &&
            (cubicle.name.StartsWith("C02_Cubicle_", StringComparison.Ordinal) ||
             cubicle.name.StartsWith("C03_Cubicle_", StringComparison.Ordinal));
    }

    static Material RoomPlaqueMaterial(string name)
    {
        var path = FitoutAssets + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;

        // The print is baked into the opaque facade. It shares the facade's depth test.
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) throw new InvalidOperationException("URP Unlit shader is required for the room plaques.");
        var material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static List<PlaqueMount> ReadPlaqueMounts()
    {
        var mounts = new List<PlaqueMount>();
        for (int floor = 1; floor <= 3; floor++)
        {
            var layout = JsonUtility.FromJson<Plan>(File.ReadAllText($"Assets/JEI_Blockout/Floor0{floor}_Plan.json"));
            var rooms = layout.rooms.OrderBy(r => r.id.Contains("N") ? 0 : r.id.Contains("S") ? 1 : 2).ThenBy(r => r.id).ToArray();
            if (rooms.Length != 17) throw new InvalidOperationException("The plaque artwork requires 17 existing numbers per floor.");

            for (int index = 0; index < rooms.Length; index++)
            {
                var room = rooms[index];
                string id = room.id.Substring(room.id.Length - 3);
                bool northRoom = room.z0 >= 5.9f;
                bool southRoom = room.z1 <= -5.9f;
                bool southApproach = northRoom || (!southRoom && (room.z1 <= 0 || id == "C01" || (id == "C04" && floor == 1)));
                float wallZ = northRoom ? 5.9f : southRoom ? -5.9f : southApproach ? -3.3f : 3.3f;
                float towardsLeft = southApproach ? -1f : 1f;
                var wall = layout.walls.Single(w => w.axis == "H" && Mathf.Abs(w.at - wallZ) < .01f &&
                    w.start <= room.x0 + .01f && w.end >= room.x1 - .01f);

                var anchor = wall.openings.SingleOrDefault(o => o.kind == "door" && o.at > room.x0 && o.at < room.x1);
                // On upper floors the atrium plaque is beside the viewing window.
                if (anchor == null && id == "C04") anchor = wall.openings.Single(o => o.kind == "window");
                if (anchor == null && id != "C06") throw new InvalidOperationException("No doorway found for plaque " + room.id);

                float edge = anchor == null ? (room.x0 + room.x1) * .5f : anchor.at + towardsLeft * anchor.width * .5f;
                float available = towardsLeft < 0 ? edge - wall.start : wall.end - edge;
                foreach (var neighbor in wall.openings)
                {
                    if (neighbor == anchor) continue;
                    float nearEdge = neighbor.at - towardsLeft * neighbor.width * .5f;
                    float distance = towardsLeft * (nearEdge - edge);
                    if (distance >= 0) available = Mathf.Min(available, distance);
                }

                const float frameGap = .05f;
                float width = Mathf.Min(PlaqueMaxWidth, available - 2 * frameGap);
                if (width < .13f) throw new InvalidOperationException("Left wall pier is too narrow for plaque " + room.id);
                float x = edge + towardsLeft * (frameGap + width * .5f);
                float surfaceZ = wallZ + towardsLeft * (layout.internalWall * .5f + .003f);
                mounts.Add(new PlaqueMount
                {
                    RoomId = room.id, RoomName = room.name, Number = $"{floor}-{index + 1:000}",
                    RestroomLabel = id == "C02" ? "WOMEN" : id == "C03" ? "MEN" : id == "C07" ? "ACCESSIBLE WC" : null,
                    Floor = floor, AtlasIndex = mounts.Count, Width = width,
                    Position = new Vector3(x, 1.65f, surfaceZ),
                    Rotation = Quaternion.Euler(0, southApproach ? 0 : 180, 0)
                });
            }
        }

        return mounts;
    }

    static Mesh SavePlaqueMesh(PlaqueMount mount)
    {
        float x = mount.Width / 2;
        float y = PlaqueHeight / 2;
        var a = new Vector3(-x, -y, 0);
        var b = new Vector3(0, -y, -PlaqueProjection);
        var c = new Vector3(x, -y, 0);
        var top = Vector3.up * PlaqueHeight;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var printFaces = new List<int>();
        var edgeFaces = new List<int>();

        int col = mount.AtlasIndex % PlaqueColumns;
        int row = mount.AtlasIndex / PlaqueColumns;
        // Half-texel offsets prevent fetching the neighboring plate at the border.
        float left = col / (float)PlaqueColumns + .5f / 2048;
        float right = (col + 1) / (float)PlaqueColumns - .5f / 2048;
        float upper = 1 - row / (float)PlaqueRows - .5f / 1024;
        float lower = 1 - (row + 1) / (float)PlaqueRows + .5f / 1024;

        void Side(Vector3 start, Vector3 end, List<int> triangles, bool printed)
        {
            int first = vertices.Count;
            vertices.AddRange(new[] { start, start + top, end + top, end });
            uv.AddRange(printed ? new[] { new Vector2(left, lower), new Vector2(left, upper),
                new Vector2(right, upper), new Vector2(right, lower) } : new Vector2[4]);
            triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
        }

        void Cap(Vector3 firstPoint, Vector3 secondPoint, Vector3 thirdPoint)
        {
            int first = vertices.Count;
            vertices.AddRange(new[] { firstPoint, secondPoint, thirdPoint });
            uv.AddRange(new Vector2[3]);
            edgeFaces.AddRange(new[] { first, first + 1, first + 2 });
        }

        Side(a, b, printFaces, true);
        Side(b, c, printFaces, true);
        Side(c, a, edgeFaces, false);
        Cap(a, b, c);
        Cap(a + top, c + top, b + top);

        var mesh = new Mesh { name = "Triangular_Plaque_" + mount.Number };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(printFaces, 0);
        mesh.SetTriangles(edgeFaces, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string path = FitoutAssets + "/" + mesh.name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!saved)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        EditorUtility.CopySerialized(mesh, saved);
        EditorUtility.SetDirty(saved);
        Object.DestroyImmediate(mesh);
        return saved;
    }

    static void VerifyRoomPlaques(JEIStructureView view)
    {
        var mounts = ReadPlaqueMounts();
        Physics.SyncTransforms();
        for (int floor = 1; floor <= 3; floor++)
        {
            var floorRoot = view.floors[floor - 1];
            var group = floorRoot.Find(PlaqueGroupName);
            if (!group || group.childCount != 17 || group.GetComponentsInChildren<TextMesh>(true).Length != 0)
                throw new InvalidOperationException("Expected 17 printed plaques and no floating text on floor " + floor);
            if (floorRoot.GetComponentsInChildren<TextMesh>().Any(IsCubicleNumber))
                throw new InvalidOperationException("Restroom cubicle numbers are still visible on floor " + floor);
        }

        foreach (var mount in mounts)
        {
            var group = view.floors[mount.Floor - 1].Find(PlaqueGroupName);
            // Some room names contain '/', so use the direct children instead of a hierarchy path.
            var plaque = group.Cast<Transform>().SingleOrDefault(p => p.name == mount.ObjectName);
            if (!plaque || Vector3.Distance(plaque.localPosition, mount.Position) > .002f ||
                Quaternion.Angle(plaque.localRotation, mount.Rotation) > .1f)
                throw new InvalidOperationException("Plaque is not on the planned left wall pier: " + mount.Number);

            var mesh = plaque.GetComponent<MeshFilter>().sharedMesh;
            var print = plaque.GetComponent<MeshRenderer>().sharedMaterial;
            if (!mesh || mesh.subMeshCount != 2 || mesh.GetIndexCount(0) != 12 || mesh.GetIndexCount(1) != 12 ||
                Mathf.Abs(mesh.bounds.size.x - mount.Width) > .001f || Mathf.Abs(mesh.bounds.size.z - PlaqueProjection) > .001f ||
                !print || print.renderQueue > 2500 || print.GetFloat("_ZWrite") != 1 || !print.GetTexture("_BaseMap"))
                throw new InvalidOperationException("Plaque lacks its opaque printed triangular body: " + mount.Number);

            foreach (float offset in new[] { -mount.Width * .48f, mount.Width * .48f })
            {
                var origin = plaque.TransformPoint(new Vector3(offset, 0, -.03f));
                if (!Physics.Raycast(origin, plaque.forward, out var hit, .06f) || hit.collider.name != "Wall")
                    throw new InvalidOperationException("Plaque is not backed by a solid wall: " + mount.Number);
            }
        }
    }
}
