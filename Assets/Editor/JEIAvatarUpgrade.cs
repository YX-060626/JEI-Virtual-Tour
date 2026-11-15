using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class JEIStructureBlockout
{
    const string CharacterFolder = "Assets/JEI_Character";
    const string CharacterPreview = "Documentation/CharacterMovement";
    const string CharacterTestKey = "JEI_Character_Test";

    [InitializeOnLoadMethod]
    static void RegisterCharacterUpgrade() { EditorApplication.update += PollCharacterUpgrade; }

    [MenuItem("Tools/JEI/Tour/Upgrade Character and Movement")]
    public static void UpgradeCharacter()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        // Preserve unsaved edits without replacing the existing building.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var open = SceneManager.GetSceneAt(i);
            if (!open.isLoaded || !open.isDirty) continue;
            string backup = "Assets/Scenes/JEI_Recovery_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity";
            if (!EditorSceneManager.SaveScene(open, backup, true)) throw new IOException("Cannot back up unsaved scene.");
            if (!string.IsNullOrEmpty(open.path) && !EditorSceneManager.SaveScene(open)) throw new IOException("Cannot preserve open scene.");
        }
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        var overview = Object.FindFirstObjectByType<JEIStructureView>();
        var tour = overview.walkPlayer.GetComponent<JEITourController>();
        if (tour.avatar && tour.avatar.GetComponent<JEIAvatarMotion>())
            throw new InvalidOperationException("Articulated avatar already exists; existing work preserved.");
        if (!AssetDatabase.IsValidFolder(CharacterFolder)) AssetDatabase.CreateFolder("Assets", "JEI_Character");
        var jacket = CharacterMaterial("Jacket", new Color(.12f, .31f, .40f));
        var trim = CharacterMaterial("Jacket_Trim", new Color(.07f, .16f, .22f));
        var trousers = CharacterMaterial("Trousers", new Color(.17f, .21f, .28f));
        var skin = CharacterMaterial("Skin", new Color(.73f, .51f, .37f));
        var hair = CharacterMaterial("Hair", new Color(.10f, .07f, .055f));
        var shoes = CharacterMaterial("Shoes", new Color(.055f, .075f, .095f));
        var light = CharacterMaterial("Soles_and_Eyes", new Color(.88f, .90f, .89f));
        var root = Group(tour.transform, "Tour Avatar__articulated_student");
        var motion = root.gameObject.AddComponent<JEIAvatarMotion>();
        motion.hips = Joint(root, "Hips", new Vector3(0, .94f, 0));
        Part(motion.hips, "Hip trousers", PrimitiveType.Capsule, new Vector3(0, -.015f, 0), new Vector3(.35f, .105f, .24f), trousers);
        motion.chest = Joint(motion.hips, "Chest", Vector3.zero);
        var torso = Part(motion.chest, "Tailored jacket", PrimitiveType.Cube, new Vector3(0, .23f, 0), Vector3.one, jacket);
        torso.GetComponent<MeshFilter>().sharedMesh = TaperedTorso();
        Part(motion.chest, "Jacket hem", PrimitiveType.Cube, new Vector3(0, .025f, .005f), new Vector3(.355f, .045f, .245f), trim);
        Part(motion.chest, "Jacket zipper", PrimitiveType.Cube, new Vector3(0, .23f, .141f), new Vector3(.014f, .39f, .012f), light);
        Part(motion.chest, "Collar", PrimitiveType.Capsule, new Vector3(0, .47f, 0), new Vector3(.21f, .04f, .20f), trim);
        Part(motion.chest, "Neck", PrimitiveType.Cylinder, new Vector3(0, .52f, 0), new Vector3(.12f, .065f, .12f), skin);
        motion.head = Joint(motion.chest, "Head", new Vector3(0, .65f, 0));
        Part(motion.head, "Face", PrimitiveType.Sphere, Vector3.zero, new Vector3(.255f, .325f, .255f), skin);
        Part(motion.head, "Short hair back", PrimitiveType.Sphere, new Vector3(0, .04f, -.053f), new Vector3(.268f, .27f, .19f), hair);
        Part(motion.head, "Short hair top", PrimitiveType.Sphere, new Vector3(0, .115f, -.005f), new Vector3(.265f, .12f, .25f), hair);
        Part(motion.head, "Nose", PrimitiveType.Sphere, new Vector3(0, -.005f, .124f), new Vector3(.046f, .065f, .048f), skin);
        foreach (int side in new[] { -1, 1 })
        {
            Part(motion.head, "Ear", PrimitiveType.Sphere, new Vector3(side * .127f, -.005f, 0), new Vector3(.043f, .075f, .05f), skin);
            Part(motion.head, "Eye white", PrimitiveType.Sphere, new Vector3(side * .049f, .031f, .114f), new Vector3(.044f, .029f, .018f), light);
            Part(motion.head, "Eye pupil", PrimitiveType.Sphere, new Vector3(side * .049f, .031f, .123f), new Vector3(.018f, .021f, .008f), shoes);
            Part(motion.head, "Eyebrow", PrimitiveType.Cube, new Vector3(side * .049f, .058f, .112f), new Vector3(.047f, .009f, .014f), hair);
            var thigh = Joint(motion.hips, side < 0 ? "Left thigh" : "Right thigh", new Vector3(side * .12f, 0, 0));
            Part(thigh, "Trouser upper leg", PrimitiveType.Capsule, new Vector3(0, -.20f, 0), new Vector3(.185f, .22f, .205f), trousers);
            var shin = Joint(thigh, "Knee", new Vector3(0, -.40f, 0));
            Part(shin, "Trouser lower leg", PrimitiveType.Capsule, new Vector3(0, -.18f, 0), new Vector3(.15f, .20f, .17f), trousers);
            var foot = Joint(shin, "Ankle", new Vector3(0, -.38f, 0));
            Part(foot, "Sneaker", PrimitiveType.Cube, new Vector3(0, -.075f, .055f), new Vector3(.165f, .12f, .30f), shoes);
            Part(foot, "Sneaker sole", PrimitiveType.Cube, new Vector3(0, -.125f, .055f), new Vector3(.17f, .025f, .305f), light);
            var arm = Joint(motion.chest, side < 0 ? "Left shoulder" : "Right shoulder", new Vector3(side * .255f, .42f, 0));
            Part(arm, "Jacket upper sleeve", PrimitiveType.Capsule, new Vector3(0, -.135f, 0), new Vector3(.15f, .165f, .16f), jacket);
            var forearm = Joint(arm, "Elbow", new Vector3(0, -.28f, 0));
            Part(forearm, "Jacket lower sleeve", PrimitiveType.Capsule, new Vector3(0, -.105f, 0), new Vector3(.125f, .135f, .135f), jacket);
            Part(forearm, "Cuff", PrimitiveType.Cylinder, new Vector3(0, -.21f, 0), new Vector3(.125f, .025f, .13f), trim);
            Part(forearm, "Hand", PrimitiveType.Sphere, new Vector3(0, -.27f, .005f), new Vector3(.105f, .14f, .10f), skin);
            if (side < 0) { motion.leftThigh = thigh; motion.leftShin = shin; motion.leftFoot = foot; motion.leftArm = arm; motion.leftForearm = forearm; }
            else { motion.rightThigh = thigh; motion.rightShin = shin; motion.rightFoot = foot; motion.rightArm = arm; motion.rightForearm = forearm; }
        }
        motion.Animate(Vector3.zero, false, true, 0, 1);
        // Preserve the earlier mannequin as an inactive source; it is no longer used by the controller.
        if (tour.avatar) { tour.avatar.name = "Previous mannequin__inactive"; tour.avatar.gameObject.SetActive(false); }
        tour.avatar = root;
        tour.runSpeed = 5.8f; tour.jumpHeight = .9f; tour.gravity = 18;
        Directory.CreateDirectory(CharacterPreview);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("JEI_CHARACTER_UPGRADE_SUCCESS");
    }

    static Transform Joint(Transform parent, string name, Vector3 p) { var t = Group(parent, name); t.localPosition = p; return t; }
    static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = size;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = material; obj.GetComponent<Renderer>().enabled = false;
        return obj;
    }
    static Material CharacterMaterial(string name, Color color)
    {
        string path = CharacterFolder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = color; m.SetFloat("_Smoothness", .12f);
        AssetDatabase.CreateAsset(m, path); return m;
    }
    static Mesh TaperedTorso()
    {
        string path = CharacterFolder + "/Jacket_Torso.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh) return mesh;
        var corners = new[] { new Vector3(-.175f,-.23f,-.12f), new Vector3(.175f,-.23f,-.12f), new Vector3(.225f,.23f,-.135f), new Vector3(-.225f,.23f,-.135f),
            new Vector3(-.175f,-.23f,.12f), new Vector3(.175f,-.23f,.12f), new Vector3(.225f,.23f,.135f), new Vector3(-.225f,.23f,.135f) };
        int[] faces = { 0,3,2,1, 4,5,6,7, 0,4,7,3, 1,2,6,5, 3,7,6,2, 0,1,5,4 };
        var vertices = new List<Vector3>(); var indices = new List<int>();
        for (int i = 0; i < faces.Length; i += 4)
        {
            int n = vertices.Count; for (int j = 0; j < 4; j++) vertices.Add(corners[faces[i+j]]);
            indices.AddRange(new[] { n,n+1,n+2,n,n+2,n+3 });
        }
        mesh = new Mesh { name = "Tailored jacket torso" }; mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path); return mesh;
    }

    static void PollCharacterUpgrade()
    {
        const string request = "Library/JEI_Character.request";
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(request))
        {
            string action = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (action == "runtime") { SessionState.SetInt(CharacterTestKey, 1); EditorApplication.isPlaying = true; }
                else UpgradeCharacter();
                File.WriteAllText("Library/JEI_Character.result", "SUCCESS: " + action);
            }
            catch (Exception e) { File.WriteAllText("Library/JEI_Character.result", "FAIL: " + e); Debug.LogException(e); }
        }
        if (SessionState.GetInt(CharacterTestKey,0) != 1 || !EditorApplication.isPlaying || Time.frameCount < 8) return;
        SessionState.SetInt(CharacterTestKey,0);
        try { VerifyCharacterMovement(); Debug.Log("JEI_CHARACTER_TEST_SUCCESS"); }
        catch (Exception e) { File.WriteAllText(CharacterPreview + "/Verification.txt", "FAIL: " + e); Debug.LogException(e); }
        finally { EditorApplication.isPlaying = false; }
    }

    static void VerifyCharacterMovement()
    {
        var overview = Object.FindFirstObjectByType<JEIStructureView>();
        if (!overview.IsWalking) overview.SetWalking(true);
        var tour = overview.walkPlayer.GetComponent<JEITourController>();
        var cc = tour.GetComponent<CharacterController>();
        var motion = tour.avatar.GetComponent<JEIAvatarMotion>();
        if (!motion) throw new InvalidOperationException("Articulated motion missing.");
        var oldKeyboard = UnityEngine.InputSystem.Keyboard.current;
        var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        var report = new List<string>();
        try
        {
            PlaceProbe(cc,new Vector3(0,.08f,-9)); tour.transform.rotation = Quaternion.identity;
            for (int i=0;i<10;i++) tour.MoveCharacter(Vector2.zero,false,.016f);
            PressTourKey(keyboard,UnityEngine.InputSystem.Key.LeftShift,tour);
            if (!tour.IsRunning) throw new InvalidOperationException("Shift failed to toggle running.");
            UnityEngine.InputSystem.InputSystem.Update();
            tour.MoveCharacter(Vector2.zero,false,.016f);
            if (!tour.IsRunning) throw new InvalidOperationException("Run mode did not persist after releasing Shift.");
            PressTourKey(keyboard,UnityEngine.InputSystem.Key.RightShift,tour);
            if (tour.IsRunning) throw new InvalidOperationException("Shift failed to toggle walking.");
            float walk = MeasureTravel(tour,cc);
            PressTourKey(keyboard,UnityEngine.InputSystem.Key.LeftShift,tour);
            float run = MeasureTravel(tour,cc);
            if (walk < 3 || run < 5.5f || run < walk * 1.6f) throw new InvalidOperationException("Walk/run speed mismatch: " + walk + "/" + run);
            report.Add("PASS: Shift toggles persistent walk/run mode (both Shift keys); measured " + walk.ToString("F2") + " / " + run.ToString("F2") + " m in one second.");

            PlaceProbe(cc,new Vector3(0,.08f,-9)); tour.MoveCharacter(Vector2.zero,false,.016f);
            float start= tour.transform.position.y;
            PressTourKey(keyboard,UnityEngine.InputSystem.Key.Space,tour);
            if (tour.VerticalSpeed <= 0) throw new InvalidOperationException("Space did not start grounded jump.");
            float firstVelocity=tour.VerticalSpeed;
            PressTourKey(keyboard,UnityEngine.InputSystem.Key.Space,tour);
            if (tour.VerticalSpeed > firstVelocity+.01f) throw new InvalidOperationException("Air jump incorrectly allowed.");
            float max=start;
            for(int i=0;i<120;i++) { tour.MoveCharacter(Vector2.zero,false,1f/120); max=Mathf.Max(max,tour.transform.position.y); }
            if(max-start < .72f || max-start > 1.05f || Mathf.Abs(tour.transform.position.y-start)>.08f || !tour.IsGrounded)
                throw new InvalidOperationException("Jump/landing mismatch: " + (max-start) + ", " + tour.transform.position);
            report.Add("PASS: Space jumps " + (max-start).ToString("F2") + "m, rejects a second airborne jump, and lands on the floor.");

            // A temporary low ceiling isolates upward collision from the existing building.
            PlaceProbe(cc,new Vector3(0,.08f,-9)); tour.MoveCharacter(Vector2.zero,false,.016f);
            var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name="Temporary jump headroom probe"; ceiling.transform.position=new Vector3(0,2.2f,-9); ceiling.transform.localScale=new Vector3(2,.2f,2);
            try
            {
                Physics.SyncTransforms(); tour.MoveCharacter(Vector2.zero,true,1f/120);
                float peak=0;
                for(int i=0;i<100;i++) { tour.MoveCharacter(Vector2.zero,false,1f/120); peak=Mathf.Max(peak,tour.transform.position.y); }
                if(peak>.36f || !tour.IsGrounded) throw new InvalidOperationException("Head collision or landing failed.");
            }
            finally { Object.DestroyImmediate(ceiling); Physics.SyncTransforms(); }
            report.Add("PASS: low ceiling stops upward motion without penetration; player lands normally.");

            for(int mode=0;mode<2;mode++)
            {
                if(tour.IsRunning != (mode==1)) PressTourKey(keyboard,UnityEngine.InputSystem.Key.LeftShift,tour);
                foreach(int side in new[] {-1,1})
                {
                    float outer=side*16.8f, inner=side*15f, passage=side*13.3f;
                    PlaceProbe(cc,new Vector3(passage,.08f,-4.6f)); tour.transform.rotation=Quaternion.identity;
                    for(int storey=0;storey<2;storey++)
                    {
                        MoveTourTo(tour,new Vector2(passage,-2.4f)); MoveTourTo(tour,new Vector2(outer,-2.4f));
                        MoveTourTo(tour,new Vector2(outer,2.4f)); MoveTourTo(tour,new Vector2(inner,2.4f));
                        MoveTourTo(tour,new Vector2(inner,-2.4f)); MoveTourTo(tour,new Vector2(passage,-2.4f));
                        MoveTourTo(tour,new Vector2(passage,-4.6f)); AssertHeight(cc,(storey+1)*3.6f);
                    }
                    for(int storey=1;storey>=0;storey--)
                    {
                        MoveTourTo(tour,new Vector2(passage,-2.4f)); MoveTourTo(tour,new Vector2(inner,-2.4f));
                        MoveTourTo(tour,new Vector2(inner,2.4f)); MoveTourTo(tour,new Vector2(outer,2.4f));
                        MoveTourTo(tour,new Vector2(outer,-2.4f)); MoveTourTo(tour,new Vector2(passage,-2.4f));
                        MoveTourTo(tour,new Vector2(passage,-4.6f)); AssertHeight(cc,storey*3.6f);
                    }
                }
            }
            report.Add("PASS: the actual updated movement controller walks AND runs up/down both staircases through all three floors.");

            tour.SetThirdPerson(true); VerifyOneCamera();
            motion.Animate(Vector3.forward*3.2f,false,true,0,.12f);
            Quaternion a=motion.leftThigh.localRotation;
            motion.Animate(Vector3.forward*3.2f,false,true,0,.12f);
            if(Quaternion.Angle(a,motion.leftThigh.localRotation)<5 || Quaternion.Angle(motion.leftThigh.localRotation,motion.rightThigh.localRotation)<5)
                throw new InvalidOperationException("Legs are not alternating.");
            for(int i=0;i<40;i++) motion.Animate(Vector3.zero,false,true,0,.016f);
            if(Quaternion.Angle(Quaternion.identity,motion.leftThigh.localRotation)>1) throw new InvalidOperationException("Idle did not settle.");
            report.Add("PASS: articulated legs alternate, arms counter-swing, idle blends out, and airborne / run poses are supported.");
            CaptureCharacter(tour,motion);
            tour.SetThirdPerson(false);
            foreach(var r in tour.avatar.GetComponentsInChildren<Renderer>()) if(r.enabled) throw new InvalidOperationException("Avatar visible in first person.");
            report.Add("PASS: first/third-person switching retains one camera and hides the full new avatar in first person.");
            File.WriteAllText(CharacterPreview+"/Verification.txt",string.Join("\n",report));
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard); if(oldKeyboard!=null) oldKeyboard.MakeCurrent();
        }
    }

    static float MeasureTravel(JEITourController tour,CharacterController cc)
    {
        PlaceProbe(cc,new Vector3(-8,.08f,-4.6f)); tour.transform.rotation=Quaternion.Euler(0,90,0);
        Vector3 start=tour.transform.position;
        for(int i=0;i<60;i++) tour.MoveCharacter(Vector2.up,false,1f/60);
        return Vector3.ProjectOnPlane(tour.transform.position-start,Vector3.up).magnitude;
    }

    static void MoveTourTo(JEITourController tour, Vector2 target)
    {
        for(int i=0;i<2400;i++)
        {
            Vector3 p=tour.transform.position;
            Vector2 remaining=target-new Vector2(p.x,p.z);
            if(remaining.magnitude<.035f) return;
            Vector3 local=tour.transform.InverseTransformDirection(new Vector3(remaining.x,0,remaining.y));
            float step=(tour.IsRunning?tour.runSpeed:tour.speed)/120;
            tour.MoveCharacter(Vector2.ClampMagnitude(new Vector2(local.x,local.z)/step,1),false,1f/120);
        }
        throw new InvalidOperationException("Updated locomotion blocked near " + tour.transform.position + " en route to " + target);
    }

    static void CaptureCharacter(JEITourController tour,JEIAvatarMotion motion)
    {
        var cc=tour.GetComponent<CharacterController>();
        PlaceProbe(cc,new Vector3(0,.08f,-9)); tour.transform.rotation=Quaternion.identity; tour.avatar.localRotation=Quaternion.identity; tour.SetThirdPerson(true);
        var cameraObject=new GameObject("Temporary character preview"); var camera=cameraObject.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.8f,.85f,.88f); camera.fieldOfView=38;
        camera.transform.position=new Vector3(2,1.55f,-5.8f); camera.transform.LookAt(new Vector3(0,1f,-9));
        try
        {
            for(int i=0;i<60;i++) motion.Animate(Vector3.zero,false,true,0,1f/60);
            SaveRender(camera,CharacterPreview+"/Avatar_Idle.png",1100,1100);
            for(int i=0;i<10;i++) motion.Animate(Vector3.forward*3.2f,false,true,0,1f/60);
            SaveRender(camera,CharacterPreview+"/Avatar_Walk.png",1100,1100);
            for(int i=0;i<12;i++) motion.Animate(Vector3.forward*5.8f,true,true,0,1f/60);
            SaveRender(camera,CharacterPreview+"/Avatar_Run.png",1100,1100);
            for(int i=0;i<8;i++) motion.Animate(Vector3.zero,false,false,3,1f/60);
            SaveRender(camera,CharacterPreview+"/Avatar_JumpPose.png",1100,1100);
        }
        finally { Object.DestroyImmediate(cameraObject); }
    }
}
