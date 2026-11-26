using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string CourtyardCafeRoot = "C04_Cafe_Fitout";

    [MenuItem("Tools/JEI/Interior/Build Courtyard Cafe")]
    public static void BuildCourtyardCafe()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before changing the courtyard.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty)
            throw new InvalidOperationException("Save the open scene before changing the courtyard.");
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath);

        BuildCourtyardCafe(Object.FindFirstObjectByType<JEIStructureView>());
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save the courtyard cafe.");
        AssetDatabase.SaveAssets();
        Debug.Log("JEI_COURTYARD_CAFE_SUCCESS");
    }

    static void BuildCourtyardCafe(JEIStructureView view)
    {
        var commons = view && view.floors.Length > 0 && view.floors[0]
            ? view.floors[0].Find("07_Courtyard_Commons/C04__Courtyard_Commons")
            : null;
        if (!commons || !commons.Find("C04_Stone_paving"))
            throw new InvalidOperationException("The furnished first-floor courtyard is missing.");

        // Keep the imported paving, glazing and entrance. All furniture sits on the existing stone surface.
        var priorCafe = commons.Find(CourtyardCafeRoot);
        if (priorCafe) Object.DestroyImmediate(priorCafe.gameObject);
        foreach (string name in new[] { "C04_Low_planting_island", "C04_Curved_bench_01", "C04_Curved_bench_02",
                     "C04_Art_and_Corner_Planting", "C04_Warm_Lighting" })
        {
            var retired = commons.Find(name);
            if (retired) Object.DestroyImmediate(retired.gameObject);
        }

        var cafe = Group(commons, CourtyardCafeRoot);
        cafe.localPosition = Vector3.up * .075f;
        cafe.gameObject.AddComponent<JEIGeneratedObject>();
        new CourtyardCafeFurniture(cafe).Build();
        CombineRoomDecoration(cafe);
        VerifyCourtyardCafe();
    }

    static void VerifyCourtyardCafe()
    {
        using var opened = new OpenRoomDoors();
        var probe = new GameObject("Temporary courtyard cafe access probe");
        var controller = probe.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = Vector3.up * .9f;
        controller.radius = .3f;
        controller.skinWidth = .035f;
        controller.stepOffset = .25f;
        try
        {
            WalkCourtyardCafe(controller);
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    static void WalkCourtyardCafe(CharacterController controller)
    {
        ImportedRoute(controller, new[]
        {
            new Vector2(0, -4.6f), new Vector2(0, -2.45f), new Vector2(0, -.6f),
            new Vector2(1.85f, .55f), new Vector2(.65f, .55f), new Vector2(0, 1.82f),
            new Vector2(-1.75f, 1.82f), new Vector2(0, 1.82f), new Vector2(0, -.82f),
            new Vector2(1.25f, -1.38f), new Vector2(0, -1.38f), new Vector2(0, -4.6f)
        });
    }

    sealed class CourtyardCafeFurniture
    {
        readonly Transform root;
        readonly Material walnut = FitMat("Cafe_Walnut", new Color32(112, 69, 47, 255));
        readonly Material maple = FitMat("Cafe_Maple", new Color32(190, 134, 90, 255));
        readonly Material brass = FitMat("Cafe_Brass", new Color32(179, 128, 62, 255));
        readonly Material dark = FitMat("Graphite_Metal", new Color(.09f, .13f, .17f));
        readonly Material green = FitMat("Cafe_Forest_Upholstery", new Color32(37, 80, 70, 255));
        readonly Material clay = FitMat("Cafe_Clay_Upholstery", new Color32(170, 88, 63, 255));
        readonly Material cream = FitMat("Porcelain", new Color(.91f, .92f, .90f));
        readonly Material stone = FitMat("Cafe_Cream_Quartz", new Color32(223, 198, 162, 255));
        readonly Material coffee = FitMat("Cafe_Espresso", new Color32(60, 34, 26, 255));
        readonly Material rug = FitMat("Cafe_Woven_Rug", new Color32(144, 91, 65, 255));
        readonly Material diffuser = FitMat("Cafe_Lamp_Diffuser", new Color32(255, 224, 178, 255));

        public CourtyardCafeFurniture(Transform root)
        {
            this.root = root;
        }

        public void Build()
        {
            diffuser.EnableKeyword("_EMISSION");
            diffuser.SetColor("_EmissionColor", new Color(1.6f, 1.07f, .58f));
            EditorUtility.SetDirty(diffuser);

            BuildCoffeeBar();
            BuildBanquette();
            for (int index = 0; index < 2; index++)
            {
                float z = index == 0 ? -1.12f : 1.04f;
                var table = CafeGroup(root, "Bistro table " + (index + 1), -2.27f, z);
                PedestalTable(table, .77f, .74f);
                Cup(table, new Vector3(-.12f, .745f, -.12f), cream);
                Cup(table, new Vector3(.14f, .745f, .125f), clay);
                BuildChair(-1.31f, z, 90, false);
                BuildWallLamp(-1, z, maple);
            }

            var lounge = CafeGroup(root, "Lounge seating", 2.46f, -2.20f);
            D(lounge, "Woven lounge rug", new Vector3(0, .007f, 0), new Vector3(2.62f, .012f, 1.4f), rug);
            BuildChair(1.53f, -2.23f, 92, true);
            BuildChair(3.40f, -2.18f, -91, true);
            PedestalTable(lounge, .68f, .44f);
            Cup(lounge, new Vector3(-.12f, .445f, .08f), cream);
            D(lounge, "Book on coffee table", new Vector3(.13f, .452f, -.055f), new Vector3(.19f, .026f, .22f), green);
            BuildWallLamp(1, .67f, dark);
            BuildWallLamp(1, -2.10f, dark);
        }

        void BuildCoffeeBar()
        {
            var bar = CafeGroup(root, "Coffee bar", 2.99f, .82f, 90);
            D(bar, "Solid cabinet", new Vector3(0, .515f, 0), new Vector3(2.64f, 1.03f, .83f), walnut, true);
            D(bar, "Recessed toe kick", new Vector3(0, .075f, -.426f), new Vector3(2.61f, .15f, .026f), dark);
            D(bar, "Quartz countertop", new Vector3(0, 1.075f, 0), new Vector3(2.80f, .09f, .98f), stone, true);
            for (int slat = -20; slat <= 20; slat++)
            {
                D(bar, "Fluted oak facing", new Vector3(slat * .061f, .578f, -.429f),
                    new Vector3(.043f, .833f, .035f), maple);
            }

            D(bar, "Brass sign backing", new Vector3(0, .67f, -.455f), new Vector3(1.01f, .29f, .026f), brass);
            D(bar, "Green brand plaque", new Vector3(0, .67f, -.472f), new Vector3(.98f, .26f, .02f), green);
            Label(bar, "JEI COFFEE", new Vector3(0, .67f, -.486f), .033f, Color.white);

            var machine = CafeGroup(bar, "Espresso machine", -.62f, .04f);
            D(machine, "Machine body", new Vector3(0, 1.39f, .082f), new Vector3(.72f, .50f, .30f), dark);
            D(machine, "Stainless front", new Vector3(0, 1.47f, -.105f), new Vector3(.72f, .23f, .12f), brass);
            D(machine, "Cup warming shelf", new Vector3(0, 1.648f, .025f), new Vector3(.76f, .04f, .43f), dark);
            D(machine, "Drip tray", new Vector3(0, 1.15f, -.11f), new Vector3(.70f, .045f, .42f), dark);
            foreach (float x in new[] { -.18f, .18f })
            {
                CafeRound(machine, "Group head", new Vector3(x, 1.345f, -.131f), .12f, .055f, dark);
                D(machine, "Portafilter handle", new Vector3(x, 1.326f, -.241f), new Vector3(.038f, .032f, .19f), walnut);
                Cup(machine, new Vector3(x, 1.18f, -.158f), cream);
                Cup(machine, new Vector3(x, 1.675f, .035f), cream);
            }

            D(bar, "Grinder base", new Vector3(-1.12f, 1.25f, .091f), new Vector3(.235f, .26f, .28f), dark);
            CafeRound(bar, "Bean hopper", new Vector3(-1.12f, 1.49f, .091f), .21f, .22f, coffee);
            CafeRound(bar, "Hopper cap", new Vector3(-1.12f, 1.61f, .091f), .23f, .025f, dark);

            D(bar, "Menu stand", new Vector3(.59f, 1.17f, .23f), new Vector3(.55f, .10f, .24f), walnut);
            D(bar, "Menu frame", new Vector3(.59f, 1.48f, .24f), new Vector3(.69f, .61f, .07f), walnut);
            D(bar, "Menu panel", new Vector3(.59f, 1.48f, .198f), new Vector3(.62f, .54f, .024f), dark);
            Label(bar, "MENU", new Vector3(.59f, 1.65f, .181f), .028f, new Color32(255, 217, 154, 255));
            Label(bar, "ESPRESSO\nFLAT WHITE\nFILTER COFFEE", new Vector3(.59f, 1.405f, .181f), .012f, Color.white);
            Cup(bar, new Vector3(.44f, 1.125f, -.21f), clay);
            Cup(bar, new Vector3(.63f, 1.125f, -.20f), green);
        }

        void BuildBanquette()
        {
            var booth = CafeGroup(root, "Window banquette", -3.36f, -.02f);
            D(booth, "Walnut base", new Vector3(0, .20f, 0), new Vector3(.78f, .40f, 3.92f), walnut, true);
            D(booth, "Plinth shadow gap", new Vector3(.395f, .07f, 0), new Vector3(.017f, .10f, 3.87f), dark);
            D(booth, "Continuous back", new Vector3(-.32f, .66f, 0), new Vector3(.18f, .62f, 3.92f), green);
            for (int index = 0; index < 4; index++)
            {
                float z = .97f * (index - 1.5f);
                D(booth, "Upholstered cushion", new Vector3(.055f, .456f, z), new Vector3(.68f, .11f, .95f), green);
                D(booth, "Upholstered back panel", new Vector3(-.218f, .715f, z), new Vector3(.075f, .44f, .945f), green);
                D(booth, "Contrast cushion", new Vector3(-.118f, .613f, z + .22f), new Vector3(.14f, .226f, .31f), index % 2 == 0 ? clay : stone);
            }
        }

        void BuildChair(float x, float z, float yaw, bool lounge)
        {
            float width = lounge ? .66f : .46f;
            float height = lounge ? .42f : .46f;
            float depth = lounge ? .68f : .48f;
            var chair = CafeGroup(root, lounge ? "Upholstered armchair" : "Cafe chair", x, z, yaw);
            var fabric = lounge ? clay : green;
            D(chair, "Seat frame", new Vector3(0, height - .071f, 0), new Vector3(width, .062f, depth), walnut);
            D(chair, "Fabric seat", new Vector3(0, height, 0), new Vector3(width - .014f, .092f, depth - .022f), fabric);
            D(chair, "Padded backrest", new Vector3(0, height + .20f, depth / 2f - .058f), new Vector3(width, .43f, .116f), fabric);
            foreach (float sideX in new[] { -1f, 1f })
            {
                foreach (float sideZ in new[] { -1f, 1f })
                    D(chair, "Tapered-look timber leg", new Vector3(sideX * (width * .5f - .06f), height / 2f - .025f,
                        sideZ * (depth * .5f - .062f)), new Vector3(.049f, height - .05f, .049f), walnut);
                if (lounge)
                    D(chair, "Padded arm", new Vector3(sideX * (width * .5f - .042f), height + .125f, .012f),
                        new Vector3(.09f, .13f, depth * .88f), fabric);
            }
            Solid(chair, new Vector3(0, .425f, 0), new Vector3(width, .85f, depth));
        }

        void PedestalTable(Transform table, float diameter, float height)
        {
            CafeRound(table, "Tabletop", new Vector3(0, height - .025f, 0), diameter, .05f, maple);
            CafeRound(table, "Brass pedestal", new Vector3(0, height / 2f, 0), .08f, height - .06f, brass);
            CafeRound(table, "Weighted steel base", new Vector3(0, .033f, 0), diameter * .63f, .063f, dark);
            Solid(table, new Vector3(0, height / 2f, 0), new Vector3(diameter, height, diameter));
        }

        void BuildWallLamp(float side, float z, Material arm)
        {
            var fixture = CafeGroup(root, "Warm cafe wall lamp", side * 3.81f, z);
            const float height = 2.45f;
            const float reach = .85f;
            D(fixture, "Wall mounting plate", new Vector3(0, height, 0), new Vector3(.075f, .24f, .13f), arm);
            D(fixture, "Horizontal swing arm", new Vector3(-side * reach * .50f, height, 0),
                new Vector3(reach, .044f, .048f), arm);
            D(fixture, "Downrod", new Vector3(-side * reach, height - .095f, 0),
                new Vector3(.036f, .19f, .036f), brass);
            Shape(fixture, "Spun metal lamp shade", PrimitiveType.Sphere,
                new Vector3(-side * reach, height - .22f, 0), new Vector3(.34f, .23f, .34f), green);
            CafeRound(fixture, "Warm lamp diffuser", new Vector3(-side * reach, height - .296f, 0), .245f, .026f, diffuser);
            var lightRoot = Group(fixture, "Local warm illumination");
            lightRoot.localPosition = new Vector3(-side * reach, height - .43f, 0);
            var light = lightRoot.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 3.6f;
            light.color = new Color(1f, .83f, .66f);
            light.shadows = LightShadows.None;
        }

        void Cup(Transform parent, Vector3 center, Material ceramic)
        {
            var cup = Group(parent, "Coffee and saucer");
            cup.localPosition = center;
            CafeRound(cup, "Saucer", new Vector3(0, .009f, 0), .143f, .017f, ceramic);
            CafeRound(cup, "Ceramic cup", new Vector3(0, .056f, 0), .088f, .082f, ceramic);
            CafeRound(cup, "Espresso surface", new Vector3(0, .097f, 0), .075f, .002f, coffee);
            D(cup, "Handle outer", new Vector3(.060f, .058f, 0), new Vector3(.010f, .046f, .014f), ceramic);
            foreach (float height in new[] { .039f, .077f })
                D(cup, "Handle join", new Vector3(.049f, height, 0), new Vector3(.031f, .011f, .014f), ceramic);
        }
    }

    static Transform CafeGroup(Transform parent, string name, float x, float z, float yaw = 0)
    {
        var group = Group(parent, name);
        group.localPosition = new Vector3(x, 0, z);
        group.localRotation = Quaternion.Euler(0, yaw, 0);
        return group;
    }

    static void CafeRound(Transform parent, string name, Vector3 center, float diameter, float height, Material material)
    {
        Shape(parent, name, PrimitiveType.Cylinder, center, new Vector3(diameter, height / 2f, diameter), material);
    }
}
