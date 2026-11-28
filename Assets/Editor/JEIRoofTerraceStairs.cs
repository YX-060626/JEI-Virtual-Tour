using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string RoofTerraceStairsName = "05_Stairs__to_floor_06";
    const float RoofStairStorey = 3.6f;

    static void BuildRoofTerraceStairs(Transform floor5, Transform floor6, Material railMaterial)
    {
        if (Mathf.Abs(floor6.position.y - floor5.position.y - 2f * RoofStairStorey) > .01f)
            throw new InvalidOperationException("The rooftop stair requires a 7.20 m rise from the sports hall.");

        var oldStairs = floor5.Find(RoofTerraceStairsName);
        if (oldStairs) Object.DestroyImmediate(oldStairs.gameObject);
        var oldArrival = floor6.Find("04_Stairs__arrival_only");
        if (oldArrival) Object.DestroyImmediate(oldArrival.gameObject);
        var fifthArrival = floor5.Find("04_Stairs__arrival_only");
        if (!fifthArrival) throw new InvalidOperationException("The fifth-floor stair landings are missing.");
        foreach (string sideName in new[] { "West opening guards", "East opening guards" })
        {
            var terminal = fifthArrival.Find(sideName + "/Unused south half guard");
            if (terminal) Object.DestroyImmediate(terminal.gameObject);
        }

        var stairs = Group(floor5, RoofTerraceStairsName);
        var firstRun = Group(stairs, "Sports hall to intermediate landing");
        var secondRun = Group(stairs, "Intermediate landing to rooftop");
        secondRun.localPosition = Vector3.up * RoofStairStorey;

        Material previousFrame = frame;
        bool hadStairFinish = finishes.TryGetValue("stair", out var previousStairFinish);
        try
        {
            frame = railMaterial;
            finishes["stair"] = LoadSportsAccessMaterial("Assets/JEI_Blockout/Materials/Stairs.mat");
            foreach (int side in new[] { -1, 1 })
            {
                BuildStair(firstRun, side, true);
                BuildStair(secondRun, side, true);
                FinishRoofStairRun(firstRun, side, railMaterial, false);
                FinishRoofStairRun(secondRun, side, railMaterial, true);
            }
        }
        finally
        {
            frame = previousFrame;
            if (hadStairFinish) finishes["stair"] = previousStairFinish;
            else finishes.Remove("stair");
        }

        BuildRoofStairSupports(floor5, railMaterial);
        BuildSportsArrivalGuards(floor6, railMaterial);
        var arrival = floor6.Find("04_Stairs__arrival_only");
        foreach (int side in new[] { -1, 1 })
        {
            Box(arrival, "Outer stair opening guard", new Vector3(side * 17.65f, .55f, .80f),
                new Vector3(.10f, 1.10f, 4.90f), railMaterial);
        }
        InstallSportsStairFences(floor5);
    }

    static void BuildRoofStairSupports(Transform floor5, Material metal)
    {
        var stairs = floor5.Find(RoofTerraceStairsName);
        if (!stairs) throw new InvalidOperationException("The rooftop stair frame is missing.");

        const string frameName = "Stair landing steel supports";
        var existing = stairs.Find(frameName);
        if (existing) Object.DestroyImmediate(existing.gameObject);
        var supports = Group(stairs, frameName);

        // The stair cores have a narrow strip alongside the glazing. Keep the posts on that strip.
        const float columnX = 17.63f;
        const float insideX = 14.25f;
        const float south = -2.84f;
        const float north = 2.74f;
        float clearHeight = SportsClearHeight - RoofDeckThickness;
        foreach (int side in new[] { -1, 1 })
        {
            float outer = side * columnX;
            foreach (float z in new[] { south, north })
            {
                Box(supports, "Steel stair column", new Vector3(outer, clearHeight * .5f, z),
                    new Vector3(.16f, clearHeight, .18f), metal);
                D(supports, "Stair column base plate", new Vector3(outer, .025f, z),
                    new Vector3(.22f, .05f, .27f), metal);
            }

            foreach (float landingHeight in new[] { RoofStairStorey * .5f, RoofStairStorey, RoofStairStorey * 1.5f })
            {
                float z = landingHeight == RoofStairStorey ? south : north;
                const float beamDepth = .18f;
                float beamY = landingHeight - .15f - beamDepth * .5f + .01f;
                Box(supports, "Landing steel crossbeam",
                    new Vector3(side * (insideX + columnX) * .5f, beamY, z),
                    new Vector3(columnX - insideX, beamDepth, .16f), metal);
                var from = new Vector3(outer, beamY - .50f, z);
                var to = new Vector3(side * (columnX - 1.03f), beamY - .05f, z);
                var brace = Box(supports, "Landing knee brace", (from + to) * .5f,
                    new Vector3(.115f, .13f, Vector3.Distance(from, to)), metal);
                brace.transform.localRotation = Quaternion.LookRotation(to - from);
            }
        }
    }

    static void FinishRoofStairRun(Transform run, int side, Material railMaterial, bool final)
    {
        var stair = run.Find(side < 0 ? "STAIR_A__WEST" : "STAIR_B__EAST");
        foreach (var text in stair.GetComponentsInChildren<TextMesh>())
            text.gameObject.SetActive(false);
        foreach (Transform item in stair)
        {
            if (item.name == "Diagrammatic guardrail")
                item.gameObject.AddComponent<BoxCollider>();
        }

        // The existing model has midline rails. Add matching rails along the exposed outer edges.
        float outside = side * 17.59f;
        float inside = side * 14.21f;
        AddRoofStairRail(stair, new Vector3(outside, 1.10f, -1.65f),
            new Vector3(outside, 2.90f, 1.65f), railMaterial);
        AddRoofStairRail(stair, new Vector3(inside, 2.90f, 1.65f),
            new Vector3(inside, 4.70f, -1.65f), railMaterial);
        foreach (float x in new[] { outside, inside })
            AddRoofStairRail(stair, new Vector3(x, 2.90f, 1.65f),
                new Vector3(x, 2.90f, 3.12f), railMaterial);

        if (final)
        {
            // The sixth-floor slab provides the final landing immediately beyond the last tread.
            var landing = stair.Find("Next floor landing__+3.60m");
            if (landing) Object.DestroyImmediate(landing.gameObject);
            return;
        }

        // Only the two stair landings exist at this height; the sports hall stays clear between them.
        foreach (float x in new[] { outside, inside })
            Box(stair, "Intermediate landing side guard", new Vector3(x, 4.15f, -2.40f),
                new Vector3(.08f, 1.1f, 1.50f), railMaterial);
        Box(stair, "Intermediate landing south guard", new Vector3(side * 15.90f, 4.15f, -3.12f),
            new Vector3(3.36f, 1.10f, .08f), railMaterial);
    }

    static void AddRoofStairRail(Transform stair, Vector3 from, Vector3 to, Material material)
    {
        var rail = Box(stair, "Roof stair handrail", (from + to) * .5f,
            new Vector3(.05f, .05f, Vector3.Distance(from, to)), material);
        rail.transform.localRotation = Quaternion.LookRotation(to - from);
    }

    static void VerifyRoofTerraceStairs(Transform floor5, Transform floor6)
    {
        Physics.SyncTransforms();
        var probe = new GameObject("Temporary rooftop stair walking probe");
        var capsule = probe.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.center = Vector3.up * .90f;
        capsule.radius = .30f;
        capsule.stepOffset = .25f;
        capsule.skinWidth = .035f;
        try
        {
            foreach (int side in new[] { -1, 1 })
            {
                float westOrEast = side * 13.3f;
                float outer = side * 16.80f;
                float inner = side * 15.00f;
                float bottom = floor5.position.y;
                float top = floor6.position.y;
                var landingApproach = new Vector2(westOrEast, -2.40f);
                var deckApproach = new Vector2(westOrEast, -4.60f);
                var lowFlightEnd = new Vector2(outer, -2.40f);
                var lowFlightTurn = new Vector2(outer, 2.40f);
                var highFlightTurn = new Vector2(inner, 2.40f);
                var highFlightEnd = new Vector2(inner, -2.40f);

                PlaceProbe(capsule, new Vector3(deckApproach.x, bottom + .05f, deckApproach.y));
                WalkTo(capsule, landingApproach);
                for (int run = 0; run < 2; run++)
                {
                    WalkTo(capsule, lowFlightEnd);
                    WalkTo(capsule, lowFlightTurn);
                    AssertHeight(capsule, bottom + RoofStairStorey * (run + .5f));
                    WalkTo(capsule, highFlightTurn);
                    WalkTo(capsule, highFlightEnd);
                    AssertHeight(capsule, bottom + RoofStairStorey * (run + 1));
                }
                WalkTo(capsule, landingApproach);
                WalkTo(capsule, deckApproach);
                AssertHeight(capsule, top);

                WalkTo(capsule, landingApproach);
                for (int run = 1; run >= 0; run--)
                {
                    WalkTo(capsule, highFlightEnd);
                    WalkTo(capsule, highFlightTurn);
                    AssertHeight(capsule, bottom + RoofStairStorey * (run + .5f));
                    WalkTo(capsule, lowFlightTurn);
                    WalkTo(capsule, lowFlightEnd);
                    AssertHeight(capsule, bottom + RoofStairStorey * run);
                }
                WalkTo(capsule, landingApproach);
                WalkTo(capsule, deckApproach);
                AssertHeight(capsule, bottom);
            }
        }
        finally { Object.DestroyImmediate(probe); }
    }
}
