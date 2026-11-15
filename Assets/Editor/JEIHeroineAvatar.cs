using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string HeroineName = "Tour Avatar__articulated_heroine";
    const string HeroineFolder = CharacterFolder + "/Heroine";

    static readonly Vector4[] HeroineHeadProfile =
    {
        new Vector4(-.147f, .028f, .031f, .040f), new Vector4(-.126f, .064f, .068f, .024f),
        new Vector4(-.095f, .090f, .090f, .015f), new Vector4(-.060f, .111f, .109f, .009f),
        new Vector4(-.018f, .127f, .117f, .005f), new Vector4(.031f, .134f, .120f, -.001f),
        new Vector4(.075f, .132f, .119f, -.004f), new Vector4(.119f, .119f, .109f, -.008f),
        new Vector4(.156f, .092f, .086f, -.013f), new Vector4(.180f, .053f, .049f, -.018f),
        new Vector4(.188f, .009f, .010f, -.020f)
    };

    static readonly Vector4[] HeroineJacketProfile =
    {
        new Vector4(.008f, .159f, .106f, 0), new Vector4(.040f, .163f, .111f, 0),
        new Vector4(.109f, .146f, .102f, 0), new Vector4(.186f, .145f, .102f, .003f),
        new Vector4(.268f, .169f, .121f, .007f), new Vector4(.345f, .185f, .117f, .004f),
        new Vector4(.391f, .191f, .105f, 0), new Vector4(.425f, .154f, .089f, 0),
        new Vector4(.462f, .086f, .071f, .002f)
    };

    sealed class HeroinePalette
    {
        public readonly Material skin = HeroineMaterial("Refined_Warm_Skin", new Color(.88f, .66f, .56f));
        public readonly Material lips = HeroineMaterial("Refined_Rose_Lips", new Color(.50f, .23f, .23f));
        public readonly Material lowerLip = HeroineMaterial("Refined_Rose_Tint", new Color(.72f, .41f, .39f));
        public readonly Material hair = HeroineMaterial("Refined_Chestnut_Hair", new Color(.235f, .112f, .077f), .23f);
        public readonly Material hairLight = HeroineMaterial("Refined_Chestnut_Waves", new Color(.31f, .157f, .105f), .24f);
        public readonly Material brow = HeroineMaterial("Refined_Espresso_Brow", new Color(.18f, .087f, .067f));
        public readonly Material iris = HeroineMaterial("Refined_Hazel_Iris", new Color(.32f, .43f, .26f), .25f);
        public readonly Material ink = HeroineMaterial("Refined_Eye_Contour", new Color(.09f, .054f, .050f));
        public readonly Material ivory = HeroineMaterial("Refined_Ivory_Jersey", new Color(.95f, .91f, .80f));
        public readonly Material whites = HeroineMaterial("Refined_Eye_White", new Color(.95f, .92f, .87f), .26f);
        public readonly Material jacket = HeroineMaterial("Refined_Deep_Teal_Twill", new Color(.20f, .43f, .40f));
        public readonly Material trim = HeroineMaterial("Refined_Teal_Seams", new Color(.115f, .28f, .25f));
        public readonly Material lapel = HeroineMaterial("Refined_Teal_Lapels", new Color(.26f, .49f, .44f));
        public readonly Material brass = HeroineMaterial("Refined_Brass_Accents", new Color(.86f, .67f, .37f), .38f);
        public readonly Material trousers = HeroineMaterial("Refined_Indigo_Denim", new Color(.22f, .29f, .40f));
        public readonly Material boot = HeroineMaterial("Refined_Cognac_Boots", new Color(.52f, .28f, .18f), .22f);
        public readonly Material sole = HeroineMaterial("Refined_Boot_Outsole", new Color(.27f, .15f, .105f));
    }

    static void BuildHeroineAvatar(JEIStructureView view)
    {
        if (!view || !view.walkPlayer) throw new InvalidOperationException("The tour player is missing.");
        var tour = view.walkPlayer.GetComponent<JEITourController>();
        if (!tour) throw new InvalidOperationException("The tour player has no walking controller.");
        if (!AssetDatabase.IsValidFolder(CharacterFolder)) AssetDatabase.CreateFolder("Assets", "JEI_Character");
        if (!AssetDatabase.IsValidFolder(HeroineFolder)) AssetDatabase.CreateFolder(CharacterFolder, "Heroine");

        var palette = new HeroinePalette();
        var avatar = Group(tour.transform, HeroineName);
        avatar.gameObject.AddComponent<JEIGeneratedObject>();
        try
        {
            var motion = avatar.gameObject.AddComponent<JEIAvatarMotion>();
            motion.hips = Joint(avatar, "Hips", new Vector3(0, .94f, 0));
            motion.chest = Joint(motion.hips, "Chest", Vector3.zero);
            HeroineMesh(motion.hips, "High waist denim", "Refined_Pelvis", palette.trousers, HeroineProfileMesh(new[]
            {
                new Vector4(-.105f, .096f, .083f, 0), new Vector4(-.068f, .161f, .111f, 0),
                new Vector4(-.008f, .168f, .115f, 0), new Vector4(.051f, .151f, .102f, 0)
            }));
            BuildRefinedJacket(motion.chest, palette);
            motion.head = Joint(motion.chest, "Head", new Vector3(0, .628f, .007f));
            BuildRefinedFace(motion.head, palette);
            BuildRefinedHair(motion.head, palette);
            BuildRefinedLimbs(motion, -1, palette);
            BuildRefinedLimbs(motion, 1, palette);
            motion.Animate(Vector3.zero, false, true, 0, 1);
        }
        catch
        {
            Object.DestroyImmediate(avatar.gameObject);
            throw;
        }

        var old = tour.avatar;
        tour.avatar = avatar;
        if (old && old.name == HeroineName && old.GetComponent<JEIGeneratedObject>())
            Object.DestroyImmediate(old.gameObject);
        else if (old)
            old.gameObject.SetActive(false);
    }

    static void BuildRefinedJacket(Transform chest, HeroinePalette p)
    {
        HeroineMesh(chest, "Tailored teal jacket", "Refined_Jacket", p.jacket, HeroineProfileMesh(HeroineJacketProfile));
        HeroineMesh(chest, "Neck", "Refined_Neck", p.skin, HeroineProfileMesh(new[]
        {
            new Vector4(.429f, .063f, .060f, .001f), new Vector4(.473f, .053f, .053f, .003f),
            new Vector4(.533f, .048f, .054f, .013f), new Vector4(.562f, .064f, .058f, .011f)
        }));

        Vector2[] shirt = { new Vector2(.008f,.045f), new Vector2(.100f,.047f), new Vector2(.206f,.053f),
            new Vector2(.295f,.063f), new Vector2(.380f,.074f), new Vector2(.440f,.068f), new Vector2(.456f,.055f) };
        HeroineContourPanel(chest, "Ivory knit shirt", "Refined_Shirt", p.ivory, shirt, 0);
        foreach (int side in new[] { -1, 1 })
        {
            HeroineContourPanel(chest, "Turned jacket lapel", "Refined_Lapel_" + side, p.lapel, shirt, side);
            HeroinePoint(chest, "Waist pocket seam", new Vector3(side * .116f, .114f, .081f),
                new Vector3(.064f, .010f, .011f), p.trim, new Vector3(0, side * 37, side * 24));
            HeroinePoint(chest, "Brass jacket snap", new Vector3(side * .067f, .161f, .101f),
                new Vector3(.012f, .013f, .010f), p.brass);
        }
        HeroinePoint(chest, "Campus enamel pin", new Vector3(-.12f, .302f, .099f), new Vector3(.025f, .025f, .015f),
            p.brass, new Vector3(0, -38f, 0));
    }

    static void HeroineContourPanel(Transform chest, string name, string assetName, Material material, Vector2[] profile, int side)
    {
        const int columns = 10;
        const int subdivisions = 6;
        int rows = (profile.Length - 1) * subdivisions;
        var points = new List<Vector3>();
        var triangles = new List<int>();
        for (int row = 0; row <= rows; row++)
        {
            int lower = Mathf.Min(row / subdivisions, profile.Length - 2);
            Vector2 section = Vector2.Lerp(profile[lower], profile[lower + 1], (row - lower * subdivisions) / (float)subdivisions);
            float y = section.x;
            float width = section.y;
            float collar = Mathf.InverseLerp(.20f, .425f, y);
            float lapelWidth = Mathf.Lerp(.011f, .040f, collar) * (y > .44f ? .65f : 1f);
            float left = side < 0 ? -width - lapelWidth : side > 0 ? width - .002f : -width;
            float right = side < 0 ? -width + .002f : side > 0 ? width + lapelWidth : width;
            for (int column = 0; column <= columns; column++)
            {
                float x = Mathf.Lerp(left, right, (float)column / columns);
                float z = HeroineSurfaceDepth(HeroineJacketProfile, x, y) + (side == 0 ? .0065f : .0085f);
                points.Add(new Vector3(x,y,z));
            }
        }
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                int k = row * (columns + 1) + column;
                triangles.AddRange(new[] { k,k+1,k+columns+1,k+1,k+columns+2,k+columns+1 });
            }
        HeroineMesh(chest, name, assetName, material, HeroineNewMesh(points, triangles));
    }

    static void BuildRefinedFace(Transform head, HeroinePalette p)
    {
        HeroineMesh(head, "Sculpted head", "Refined_Face", p.skin, HeroineProfileMesh(HeroineHeadProfile, HeroineNose));
        foreach (int side in new[] { -1, 1 })
        {
            HeroinePoint(head, "Ear", new Vector3(side * .126f, -.025f, .007f), new Vector3(.040f, .067f, .043f), p.skin,
                new Vector3(0, side * 20f, side * -10f));
            HeroinePoint(head, "Small gold stud", new Vector3(side * .140f, -.051f, .023f), new Vector3(.013f, .013f, .013f), p.brass);
            BuildRefinedEye(head, side, p);
        }
        var lipCurve = new List<Vector3>();
        var lowerLip = new List<Vector3>();
        for (int i = 0; i <= 16; i++)
        {
            float t = i / 8f - 1f;
            float x = t * .044f;
            float y = -.086f + .012f * t * t + t * .002f;
            lipCurve.Add(new Vector3(x,y,HeroineFaceDepth(x,y) + .0025f));
            y -= .005f;
            lowerLip.Add(new Vector3(x * .85f,y,HeroineFaceDepth(x * .85f,y) + .0025f));
        }
        HeroineLock(head, "Subtle smile", "Refined_Smile", p.lips, lipCurve, .0033f, .0030f);
        HeroineLock(head, "Warm lower lip", "Refined_Lower_Lip", p.lowerLip, lowerLip, .0035f, .0033f);
    }

    static float HeroineNose(float x, float y)
    {
        float bridge = Mathf.Exp(-Mathf.Pow(x / .018f, 2f) - Mathf.Pow((y + .018f) / .045f, 2f));
        float tip = Mathf.Exp(-Mathf.Pow(x / .022f, 2f) - Mathf.Pow((y + .042f) / .018f, 2f));
        float wing = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - .019f) / .011f, 2f) - Mathf.Pow((y + .047f) / .013f, 2f));
        return .013f * bridge + .025f * tip + .007f * wing;
    }

    static float HeroineFaceDepth(float x, float y) => HeroineSurfaceDepth(HeroineHeadProfile, x, y) + HeroineNose(x,y);

    static void BuildRefinedEye(Transform head, int side, HeroinePalette p)
    {
        const float center = .059f;
        const float width = .036f;
        const int segments = 24;
        float EyeLift(float x) => .031f + (x * side - center) * .13f;
        Vector3 EyePoint(float angle, float radius)
        {
            float x = side * (center + Mathf.Cos(angle) * width * radius);
            float amplitude = Mathf.Sin(angle) >= 0 ? .022f : .014f;
            float y = EyeLift(x) + Mathf.Sin(angle) * amplitude * radius;
            float bulge = .0025f + .009f * (1 - radius * radius);
            return new Vector3(x, y, HeroineFaceDepth(x,y) + bulge);
        }
        var vertices = new List<Vector3> { EyePoint(0,0) };
        var triangles = new List<int>();
        for (int ring = 1; ring <= 4; ring++)
            for (int i = 0; i < segments; i++) vertices.Add(EyePoint(i * Mathf.PI * 2 / segments, ring / 4f));
        for (int i = 0; i < segments; i++)
        {
            int next = (i+1)%segments;
            HeroineTriangle(triangles, side, 0, 1+i, 1+next);
            for (int ring = 0; ring < 3; ring++)
            {
                int start = 1 + ring * segments;
                HeroineTriangle(triangles, side, start+i, start+segments+i, start+next);
                HeroineTriangle(triangles, side, start+next, start+segments+i, start+segments+next);
            }
        }
        HeroineMesh(head, "Inset almond eye", "Refined_Eye_" + side, p.whites, HeroineNewMesh(vertices,triangles));

        float ix = side * (center - .003f);
        const float iy = .032f;
        float iz = HeroineFaceDepth(ix,iy) + .009f;
        var iris = Joint(head, "Hazel eye", new Vector3(ix,iy,iz));
        iris.localRotation = Quaternion.Euler(0, side * 22f, 0);
        HeroinePoint(iris, "Iris outline", Vector3.zero, new Vector3(.031f,.035f,.009f), p.ink);
        HeroinePoint(iris, "Green iris", new Vector3(0,0,.0035f), new Vector3(.027f,.032f,.009f), p.iris);
        HeroinePoint(iris, "Pupil", new Vector3(0,0,.0075f), new Vector3(.015f,.021f,.005f), p.ink);
        HeroinePoint(iris, "Catchlight", new Vector3(-.005f,.008f,.010f), new Vector3(.006f,.007f,.0035f), p.whites);

        var eyeLine = new List<Vector3>();
        var browLine = new List<Vector3>();
        for (int i = 0; i <= 16; i++)
        {
            float t = i / 16f;
            var lid = EyePoint(Mathf.Lerp(0f,Mathf.PI,t),1f);
            lid.z += .002f;
            eyeLine.Add(lid);
            float x = side * Mathf.Lerp(.026f,.098f,t);
            float y = .062f + Mathf.Sin(t * Mathf.PI * .95f) * .015f + t * .003f;
            browLine.Add(new Vector3(x,y,HeroineFaceDepth(x,y) + .0013f));
        }
        HeroineLock(head, "Upper eyelid contour", "Refined_Lid_" + side, p.ink, eyeLine, .0026f, .0024f);
        HeroineLock(head, "Tapered eyebrow", "Refined_Brow_" + side, p.brow, browLine, .0048f, .0025f);
    }

    static void BuildRefinedHair(Transform head, HeroinePalette p)
    {
        const int segments = 64;
        const int rings = 23;
        var points = new List<Vector3>();
        var faces = new List<int>();
        for (int ring = 0; ring <= rings; ring++)
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                float front = Mathf.Clamp01(Mathf.Cos(a));
                float hairline = Mathf.Lerp(-.128f, .138f + Mathf.Sin(a) * .013f, Mathf.Pow(front, .85f));
                float y = Mathf.Lerp(.215f, hairline, ring / (float)rings);
                float xRadius, zRadius, zCenter;
                if (y > .176f)
                {
                    float fromCrown = Mathf.InverseLerp(.215f, .176f, y);
                    float dome = Mathf.Sqrt(fromCrown);
                    xRadius = Mathf.Lerp(.001f, .087f, dome);
                    zRadius = Mathf.Lerp(.001f, .076f, dome);
                    zCenter = -.017f;
                }
                else
                {
                    Vector4 section = HeroineProfileAt(HeroineHeadProfile, y);
                    float layer = .024f + .004f * Mathf.Sin(a * 5f + ring * .15f);
                    xRadius = section.y + layer;
                    zRadius = section.z + layer;
                    zCenter = section.w;
                }
                points.Add(new Vector3(xRadius * Mathf.Sin(a), y, zCenter + zRadius * Mathf.Cos(a)));
            }
        for (int ring = 0; ring < rings; ring++)
            for (int i = 0; i < segments; i++)
            {
                int a = ring*segments+i;
                int b = ring*segments+(i+1)%segments;
                faces.AddRange(new[] { a,a+segments,b,b,a+segments,b+segments });
            }
        HeroineMesh(head, "Sculpted side part crown", "Refined_Hair_Cap", p.hair, HeroineNewMesh(points,faces));
        HeroineWave(head,"Long swept fringe","Refined_Fringe",p.hairLight,.032f,.024f,
            new Vector3(.106f,.161f,.052f),new Vector3(.049f,.173f,.121f),new Vector3(-.068f,.139f,.131f),new Vector3(-.132f,.080f,.091f));
        HeroineWave(head,"Face framing outer wave","Refined_Wave_Left",p.hair,.042f,.049f,
            new Vector3(-.116f,.161f,.018f),new Vector3(-.205f,.087f,.027f),new Vector3(-.176f,-.095f,.028f),new Vector3(-.145f,-.218f,.040f));
        HeroineWave(head,"Face framing inner wave","Refined_Wave_Left_Highlight",p.hairLight,.026f,.025f,
            new Vector3(-.133f,.132f,.060f),new Vector3(-.195f,.042f,.061f),new Vector3(-.173f,-.107f,.048f),new Vector3(-.141f,-.202f,.053f));
        HeroineWave(head,"Tucked shorter side","Refined_Wave_Right",p.hair,.058f,.045f,
            new Vector3(.108f,.131f,.022f),new Vector3(.148f,.041f,-.048f),new Vector3(.167f,-.052f,-.057f),new Vector3(.111f,-.190f,-.023f));
        HeroineWave(head,"Soft back wave left","Refined_Wave_Back_Left",p.hair,.072f,.049f,
            new Vector3(-.067f,.111f,-.118f),new Vector3(-.164f,.030f,-.143f),new Vector3(-.104f,-.147f,-.111f),new Vector3(-.074f,-.250f,-.064f));
        HeroineWave(head,"Soft back wave right","Refined_Wave_Back_Right",p.hairLight,.076f,.046f,
            new Vector3(.036f,.119f,-.107f),new Vector3(.099f,.027f,-.158f),new Vector3(.068f,-.133f,-.145f),new Vector3(.103f,-.229f,-.098f));
        HeroineWave(head,"Long nape wave","Refined_Wave_Back_Center",p.hair,.075f,.045f,
            new Vector3(-.006f,.119f,-.129f),new Vector3(-.060f,.003f,-.163f),new Vector3(.049f,-.095f,-.152f),new Vector3(.010f,-.237f,-.113f));
    }

    static void HeroineWave(Transform parent, string name, string assetName, Material material, float width, float depth,
        Vector3 start, Vector3 handleA, Vector3 handleB, Vector3 end)
    {
        var path = new List<Vector3>();
        for (int i = 0; i <= 22;i++)
        {
            float t = i/22f;
            float r = 1-t;
            path.Add(r*r*r*start+3*r*r*t*handleA+3*r*t*t*handleB+t*t*t*end);
        }
        HeroineLock(parent,name,assetName,material,path,width,depth);
    }

    static void HeroineLock(Transform parent, string name, string assetName, Material material, List<Vector3> path, float width, float depth)
    {
        const int sides = 12;
        var points = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0; i < path.Count; i++)
        {
            float t = i/(float)(path.Count-1);
            float taper = Mathf.Max(.10f,Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t*Mathf.PI)),.58f));
            Vector3 tangent = (path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(i-1,0)]).normalized;
            Vector3 cross = Vector3.Cross(tangent,Vector3.forward).normalized;
            Vector3 normal = Vector3.Cross(cross,tangent).normalized;
            for (int j = 0;j<sides;j++)
            {
                float a = j*Mathf.PI*2/sides;
                points.Add(path[i]+taper*(cross*Mathf.Cos(a)*width+normal*Mathf.Sin(a)*depth));
            }
        }
        for (int i = 0; i < path.Count-1; i++)
            for (int side = 0; side < sides; side++)
            {
                int a = i*sides+side;
                int b = i*sides+(side+1)%sides;
                triangles.AddRange(new[] { a,a+sides,b,b,a+sides,b+sides });
            }
        HeroineMesh(parent,name,assetName,material,HeroineNewMesh(points,triangles));
    }

    static void BuildRefinedLimbs(JEIAvatarMotion motion, int side, HeroinePalette p)
    {
        string mark = side < 0 ? "Left" : "Right";
        var thigh = Joint(motion.hips, mark + " thigh",new Vector3(side*.089f,-.028f,0));
        var shin = Joint(thigh,"Knee",new Vector3(0,-.376f,0));
        var foot = Joint(shin,"Ankle",new Vector3(0,-.372f,0));
        HeroineSkinnedLimb(thigh,shin,"Tapered denim leg","Refined_Denim_"+mark,p.trousers,.095f,new[]
        {
            new Vector4(-.723f,.064f,.071f,.003f), new Vector4(-.662f,.066f,.072f,-.006f),
            new Vector4(-.580f,.071f,.077f,-.012f), new Vector4(-.480f,.072f,.079f,-.010f),
            new Vector4(-.382f,.071f,.074f,.003f), new Vector4(-.329f,.073f,.081f,.002f),
            new Vector4(-.231f,.077f,.087f,0f), new Vector4(-.125f,.089f,.098f,0f),
            new Vector4(-.050f,.092f,.101f,0f), new Vector4(.012f,.084f,.095f,0f)
        });
        HeroineMesh(foot,"Suede ankle boot","Refined_Boot_"+mark,p.boot,HeroineProfileMesh(new[]
        {
            new Vector4(-.145f,.074f,.130f,.048f),new Vector4(-.116f,.075f,.130f,.048f),
            new Vector4(-.087f,.073f,.114f,.042f),new Vector4(-.066f,.066f,.091f,.018f),
            new Vector4(-.022f,.061f,.068f,-.002f),new Vector4(.044f,.066f,.067f,-.003f)
        }));
        HeroineMesh(foot,"Dark boot sole","Refined_Boot_Sole_"+mark,p.sole,HeroineProfileMesh(new[]
        {
            new Vector4(-.163f,.071f,.128f,.049f),new Vector4(-.151f,.077f,.134f,.049f),
            new Vector4(-.137f,.074f,.132f,.048f)
        }));

        var arm = Joint(motion.chest,mark+" shoulder",new Vector3(side*.166f,.378f,.002f));
        var forearm = Joint(arm,"Elbow",new Vector3(0,-.263f,0));
        HeroineSkinnedLimb(arm,forearm,"Seamless twill sleeve","Refined_Sleeve_"+mark,p.jacket,.079f,new[]
        {
            new Vector4(-.472f,.047f,.047f,.007f),new Vector4(-.430f,.048f,.051f,.003f),
            new Vector4(-.377f,.052f,.056f,0f),new Vector4(-.303f,.055f,.059f,0f),
            new Vector4(-.247f,.056f,.061f,0f),new Vector4(-.192f,.061f,.063f,0f),
            new Vector4(-.120f,.065f,.071f,0f),new Vector4(-.037f,.074f,.079f,0f),
            new Vector4(.010f,.066f,.065f,0f),new Vector4(.042f,.036f,.036f,0f)
        });
        HeroineMesh(forearm,"Structured sleeve cuff","Refined_Cuff_"+mark,p.trim,HeroineProfileMesh(new[]
        {
            new Vector4(-.219f,.045f,.047f,.006f),new Vector4(-.205f,.049f,.051f,.006f),
            new Vector4(-.174f,.049f,.051f,.003f)
        }));
        HeroinePoint(forearm,"Hand",new Vector3(0,-.257f,.009f),new Vector3(.068f,.096f,.061f),p.skin,new Vector3(8,side*14,0));
        HeroinePoint(forearm,"Thumb",new Vector3(-side*.027f,-.244f,.023f),new Vector3(.021f,.054f,.024f),p.skin,
            new Vector3(12,0,-side*17));

        if (side<0)
        {
            motion.leftThigh=thigh; motion.leftShin=shin; motion.leftFoot=foot;
            motion.leftArm=arm; motion.leftForearm=forearm;
        }
        else
        {
            motion.rightThigh=thigh; motion.rightShin=shin; motion.rightFoot=foot;
            motion.rightArm=arm; motion.rightForearm=forearm;
        }
    }

    static void HeroineSkinnedLimb(Transform upper, Transform lower, string name, string assetName, Material material,
        float blendSpan, Vector4[] sections)
    {
        var part = Joint(upper.parent,name,upper.localPosition);
        var mesh = HeroineProfileMesh(sections);
        var weights = new BoneWeight[mesh.vertexCount];
        var vertices = mesh.vertices;
        for (int i = 0;i<weights.Length;i++)
        {
            float t = Mathf.InverseLerp(lower.localPosition.y + blendSpan, lower.localPosition.y - blendSpan, vertices[i].y);
            float blend = Mathf.SmoothStep(0,1,t);
            weights[i] = blend <= .5f
                ? new BoneWeight { boneIndex0 = 0, boneIndex1 = 1, weight0 = 1 - blend, weight1 = blend }
                : new BoneWeight { boneIndex0 = 1, boneIndex1 = 0, weight0 = blend, weight1 = 1 - blend };
        }
        mesh.boneWeights = weights;
        mesh.bindposes = new[] { upper.worldToLocalMatrix*part.localToWorldMatrix,lower.worldToLocalMatrix*part.localToWorldMatrix };
        var renderer = part.gameObject.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = SaveHeroineMesh(assetName,mesh);
        renderer.bones = new[] { upper,lower };
        renderer.rootBone = upper;
        renderer.localBounds = new Bounds(Vector3.down * .29f,new Vector3(1.2f,1.7f,1.7f));
        renderer.sharedMaterial = material;
        renderer.enabled = false;
    }

    static void HeroinePoint(Transform parent, string name, Vector3 position, Vector3 size, Material material, Vector3 rotation = default)
    {
        var piece = Part(parent,name,PrimitiveType.Sphere,position,size,material);
        piece.transform.localRotation=Quaternion.Euler(rotation);
    }

    static Material HeroineMaterial(string name, Color color, float smoothness = .14f)
    {
        var path = HeroineFolder+"/"+name+".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=name,color=color };
        material.SetFloat("_Smoothness",smoothness);
        AssetDatabase.CreateAsset(material,path);
        return material;
    }

    // Sections contain elevation, half-width, half-depth and fore-aft center.
    static Vector4 HeroineProfileAt(Vector4[] sections, float y)
    {
        int index=0;
        while (index<sections.Length-2 && y>sections[index+1].x) index++;
        return Vector4.Lerp(sections[index],sections[index+1],Mathf.InverseLerp(sections[index].x,sections[index+1].x,y));
    }

    static float HeroineSurfaceDepth(Vector4[] sections, float x, float y)
    {
        Vector4 profile=HeroineProfileAt(sections,y);
        float nx=Mathf.Clamp(x/profile.y,-1,1);
        return profile.w+profile.z*Mathf.Sqrt(1-nx*nx);
    }

    static Mesh HeroineProfileMesh(Vector4[] sections, Func<float,float,float> sculpture = null)
    {
        int sides=sculpture==null ? 28 : 64;
        int bands=(sections.Length-1)*4;
        var points=new List<Vector3>();
        var triangles=new List<int>();
        for (int band=0;band<=bands;band++)
        {
            int low=Mathf.Min(band/4,sections.Length-2);
            Vector4 section=Vector4.Lerp(sections[low],sections[low+1],(band-low*4)/4f);
            for (int side=0;side<sides;side++)
            {
                float angle=side*Mathf.PI*2/sides;
                float x=section.y*Mathf.Sin(angle);
                float z=section.w+section.z*Mathf.Cos(angle);
                if (sculpture!=null) z+=sculpture(x,section.x)*Mathf.Clamp01(Mathf.Cos(angle));
                points.Add(new Vector3(x,section.x,z));
            }
        }
        for (int band=0;band<bands;band++)
            for (int side=0;side<sides;side++)
            {
                int a=band*sides+side;
                int b=band*sides+(side+1)%sides;
                triangles.AddRange(new[] { a,b,a+sides,b,b+sides,a+sides });
            }
        int bottom=points.Count;
        int top=bottom+1;
        Vector4 last=sections[sections.Length-1];
        points.Add(new Vector3(0,sections[0].x,sections[0].w));
        points.Add(new Vector3(0,last.x,last.w));
        for (int side=0;side<sides;side++)
        {
            int next=(side+1)%sides;
            triangles.AddRange(new[] { bottom,next,side,top,bands*sides+side,bands*sides+next });
        }
        return HeroineNewMesh(points,triangles);
    }

    static void HeroineTriangle(List<int> triangles, int side, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(side>0 ? b : c);
        triangles.Add(side>0 ? c : b);
    }

    static Mesh HeroineNewMesh(List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void HeroineMesh(Transform parent, string name, string assetName, Material material, Mesh mesh)
    {
        var node=Joint(parent,name,Vector3.zero).gameObject;
        node.AddComponent<MeshFilter>().sharedMesh=SaveHeroineMesh(assetName,mesh);
        var renderer=node.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;
        renderer.enabled=false;
    }

    static Mesh SaveHeroineMesh(string assetName, Mesh mesh)
    {
        string path=HeroineFolder+"/"+assetName+".asset";
        mesh.name=assetName;
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!existing)
        {
            AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
        EditorUtility.CopySerialized(mesh,existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }
}
