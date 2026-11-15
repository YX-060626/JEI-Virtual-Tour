using System;
using UnityEngine;

// Movement routes retained from the supplied project to check the imported furniture in this building.
public static partial class JEIStructureBlockout
{
    static void ImportedRoute(CharacterController controller, Vector2[] points)
    {
        controller.enabled = false; controller.transform.position = new Vector3(points[0].x, .08f, points[0].y); controller.enabled = true; Physics.SyncTransforms();
        for (int settle = 0; settle < 10; settle++) controller.Move(Vector3.down * .025f);
        for (int n = 1; n < points.Length; n++)
        {
            bool reached = false;
            for (int i = 0; i < 1500; i++)
            {
                Vector3 p = controller.transform.position; Vector2 delta = points[n] - new Vector2(p.x, p.z);
                if (delta.magnitude < .045f) { reached = true; break; }
                Vector2 step = Vector2.ClampMagnitude(delta, .04f); controller.Move(new Vector3(step.x, -.035f, step.y));
            }
            if (!reached) throw new InvalidOperationException("Furniture blocks walking near " + controller.transform.position + " toward " + points[n]);
        }
    }
    static void ImportedVerifyMeetingRoutes(CharacterController controller)
    {
        ImportedRoute(controller, new[] { new Vector2(13.3f, -4.6f), new Vector2(13.3f, -7.25f), new Vector2(14.25f, -7.25f), new Vector2(13.2f, -7.25f), new Vector2(12.15f, -7.25f) });
        ImportedRoute(controller, new[] { new Vector2(13.3f, -7.25f), new Vector2(11.4f, -7.25f), new Vector2(10.645f, -8.1f), new Vector2(10.645f, -9.25f), new Vector2(10.645f, -10.4f), new Vector2(10.645f, -11.25f), new Vector2(12.15f, -11.25f), new Vector2(13.2f, -11.25f), new Vector2(14.25f, -11.25f) });
        ImportedRoute(controller, new[] { new Vector2(14.25f, -11.25f), new Vector2(15.6f, -11.25f), new Vector2(15.6f, -11.65f), new Vector2(15.6f, -10.85f), new Vector2(15.6f, -10.4f), new Vector2(15.75f, -10.1f), new Vector2(15.75f, -9.25f), new Vector2(15.75f, -7.25f), new Vector2(16, -7.25f) });
    }
    static void ImportedVerifyRegularClassroomRoutes(CharacterController controller)
    {
        ImportedRoute(controller, new[] { new Vector2(-10.7f, 4.6f), new Vector2(-10.7f, 6.35f), new Vector2(-16.835f, 6.35f), new Vector2(-16.835f, 12.35f) });
        // Every pair is entered from its desk ends. The two inner aisles serve seats on both sides.
        foreach (float z in new[] { 6.35f, 8.325f, 10.375f, 12.35f })
            ImportedRoute(controller, new[] { new Vector2(-16.835f, z), new Vector2(-16.2f, z), new Vector2(-14.85f, z), new Vector2(-13.5f, z), new Vector2(-12.15f, z), new Vector2(-11.25f, z) });
        ImportedRoute(controller, new[] { new Vector2(-10.7f, 6.35f), new Vector2(-10.7f, 8.325f), new Vector2(-10.1f, 8.85f), new Vector2(-10.1f, 10.375f) });
        ImportedRoute(controller, new[] { new Vector2(-10.7f, 10.375f), new Vector2(-9.855f, 10.375f), new Vector2(-9.855f, 11.25f) });
    }
    static void ImportedVerifyDigitalClassroomRoutes(CharacterController controller)
    {
        ImportedRoute(controller, new[] { new Vector2(4.5f, 4.6f), new Vector2(4.5f, 6.4f), new Vector2(1.4f, 6.4f), new Vector2(1.4f, 12.3f) });
        foreach (float z in new[] { 6.4f, 9.35f, 12.3f })
            ImportedRoute(controller, new[] { new Vector2(1.4f, z), new Vector2(3.2f, z), new Vector2(5.05f, z), new Vector2(6.9f, z), new Vector2(8.75f, z), new Vector2(9.43f, z) });
        // Enter seats from desk ends; the narrow chair-back gaps are not circulation routes.
        foreach (float x in new[] { 3.2f, 5.05f, 6.9f, 8.75f })
        {
            ImportedRoute(controller, new[] { new Vector2(x, 6.4f), new Vector2(x, 6.65f) });
            ImportedRoute(controller, new[] { new Vector2(x, 9.35f), new Vector2(x, 8.95f) });
            ImportedRoute(controller, new[] { new Vector2(x, 9.35f), new Vector2(x, 9.75f) });
            ImportedRoute(controller, new[] { new Vector2(x, 12.3f), new Vector2(x, 12.05f) });
        }
        ImportedRoute(controller, new[] { new Vector2(9.43f, 6.4f), new Vector2(9.43f, 7.25f), new Vector2(9.43f, 9.35f), new Vector2(9.43f, 11.45f), new Vector2(9.43f, 12.3f) });
        ImportedRoute(controller, new[] { new Vector2(1.4f, 9.35f), new Vector2(.35f, 9.35f), new Vector2(.35f, 8.35f) });
        ImportedRoute(controller, new[] { new Vector2(1.4f, 9.35f), new Vector2(1.4f, 10.15f), new Vector2(-.18f, 10.15f), new Vector2(-.18f, 11.0f) });
    }
    static void ImportedVerifyReadingSuiteRoutes(CharacterController controller)
    {
        // The booth knee space is for seated use; approaches are from its two open ends.
        // The entry route turns east before reaching the south chairs.
        ImportedRoute(controller, new[] { new Vector2(13.3f, 4.6f), new Vector2(13.3f, 6.4f), new Vector2(13.3f, 7.55f), new Vector2(16.35f, 7.55f), new Vector2(16.35f, 8.8f), new Vector2(16.35f, 10.2f), new Vector2(16.35f, 11.65f) });
        foreach (float x in new[] { 12.05f, 14.75f })
            ImportedRoute(controller, new[] { new Vector2(13.3f, 7.55f), new Vector2(x, 7.55f), new Vector2(x, 9.05f) });
        ImportedRoute(controller, new[] { new Vector2(16.35f, 7.55f), new Vector2(16.35f, 9.55f), new Vector2(15.20f, 9.55f) });
        ImportedRoute(controller, new[] { new Vector2(13.3f, 7.55f), new Vector2(11.65f, 7.55f), new Vector2(11.65f, 9.25f) });
        ImportedRoute(controller, new[] { new Vector2(16.35f, 11.65f), new Vector2(14.70f, 11.65f) });
        ImportedRoute(controller, new[] { new Vector2(13.3f, 7.55f), new Vector2(11.10f, 7.55f), new Vector2(11.10f, 8.35f), new Vector2(11.10f, 9.10f) });
        ImportedRoute(controller, new[] { new Vector2(16.35f, 7.55f), new Vector2(16.35f, 8.8f), new Vector2(16.35f, 10.2f), new Vector2(16.35f, 11.4f) });
        foreach (float x in new[] { 14.75f, 15.55f, 16.35f, 11.65f })
            ImportedRoute(controller, new[] { new Vector2(13.3f, 7.55f), new Vector2(x, 7.55f), new Vector2(x, 7.15f) });
    }
    static void ImportedPerimeterRoutes(CharacterController controller)
    {
            ImportedRoute(controller, new[] { new Vector2(0, -12.3f), new Vector2(0, -4.6f) });
            ImportedRoute(controller, new[] { new Vector2(-5, 4.6f), new Vector2(-5, 6.3f), new Vector2(-5, 9.3f), new Vector2(-5, 12.0f) });
            ImportedRoute(controller, new[] { new Vector2(-8.5f, 9.3f), new Vector2(-1.75f, 9.3f) });
            ImportedRoute(controller, new[] { new Vector2(-8, -4.6f), new Vector2(-8, -6.55f), new Vector2(-8, -7.7f), new Vector2(-9.65f, -7.7f), new Vector2(-10.25f, -7.7f), new Vector2(-10.45f, -7.95f), new Vector2(-10.85f, -8.55f), new Vector2(-13.8f, -8.85f) });
            ImportedRoute(controller, new[] { new Vector2(-13.8f, -8.85f), new Vector2(-14.6f, -7.1f), new Vector2(-16.2f, -7.1f) });
            ImportedRoute(controller, new[] { new Vector2(-13.8f, -8.85f), new Vector2(-13, -7.35f), new Vector2(-11, -7.35f) });
            ImportedRoute(controller, new[] { new Vector2(-13.8f, -8.85f), new Vector2(-15.5f, -8.85f), new Vector2(-15.5f, -7.9f), new Vector2(-15.5f, -9.6f) });
            ImportedRoute(controller, new[] { new Vector2(-13.8f, -8.85f), new Vector2(-13.8f, -10.45f), new Vector2(-15.9f, -10.45f), new Vector2(-13.95f, -10.45f), new Vector2(-12, -10.45f), new Vector2(-10.05f, -10.45f) });
            ImportedRoute(controller, new[] { new Vector2(7.3f, -4.6f), new Vector2(7.3f, -7.4f), new Vector2(6.4f, -8.7f), new Vector2(5.875f, -9.05f) });
            ImportedRoute(controller, new[] { new Vector2(7.3f, -7.1f), new Vector2(4.9f, -7.15f) });
            ImportedRoute(controller, new[] { new Vector2(7.3f, -7.4f), new Vector2(8.4f, -7.5f), new Vector2(8.4f, -11.7f), new Vector2(6.6f, -12.05f), new Vector2(5.15f, -12.05f) });
            ImportedRoute(controller, new[] { new Vector2(6.4f, -8.7f), new Vector2(5.7f, -8.15f) });
            ImportedVerifyMeetingRoutes(controller);
            ImportedVerifyRegularClassroomRoutes(controller);
            ImportedVerifyDigitalClassroomRoutes(controller);
            ImportedVerifyReadingSuiteRoutes(controller);
    }
    static void ImportedServiceRoutes(CharacterController controller)
    {
            ImportedRoute(controller,new[]{new Vector2(8,4.6f),new Vector2(8,2.8f),new Vector2(8,1.6f),new Vector2(8.05f,1.1f),new Vector2(8,1.6f),new Vector2(8.40f,1.05f),new Vector2(8,1.6f),new Vector2(7.15f,1.6f),new Vector2(7.25f,2.1f),new Vector2(8.20f,2.1f),new Vector2(8,2.8f),new Vector2(8,4.6f)});
            ImportedRoute(controller,new[]{new Vector2(8,-4.6f),new Vector2(8,-2.8f),new Vector2(7.97f,-1.9f),new Vector2(8.05f,-1.25f),new Vector2(8.05f,-.65f),new Vector2(7.65f,-.65f),new Vector2(7.65f,-.50f),new Vector2(7.65f,-1.25f),new Vector2(7.97f,-1.9f),new Vector2(8,-2.8f),new Vector2(8,-4.6f)});
            ImportedRoute(controller,new[]{new Vector2(10.9f,-4.6f),new Vector2(10.9f,-2.8f),new Vector2(10.9f,-1.6f),new Vector2(10.9f,-.72f),new Vector2(10.3f,-.72f),new Vector2(10.9f,-1.6f),new Vector2(10.9f,-2.8f),new Vector2(10.9f,-4.6f)});
            ImportedRoute(controller,new[]{new Vector2(10.9f,4.6f),new Vector2(10.9f,3.78f),new Vector2(10.9f,4.6f)});
    }
    static void ImportedRestroomRoutes(CharacterController c)
    {
            ImportedRoute(c,new[]{new Vector2(-9.5f,4.6f),new Vector2(-9.5f,2.70f),new Vector2(-10.05f,2.70f),new Vector2(-11.05f,2.70f),new Vector2(-10.05f,2.70f),new Vector2(-10.05f,1.65f),new Vector2(-11.05f,1.65f),new Vector2(-10.05f,1.65f),new Vector2(-10.05f,.60f),new Vector2(-11.05f,.60f),new Vector2(-10.05f,.60f),new Vector2(-9.5f,.60f),new Vector2(-7.65f,.90f),new Vector2(-7.65f,2.20f),new Vector2(-9.5f,2.70f),new Vector2(-9.5f,4.6f)});
            ImportedRoute(c,new[]{new Vector2(-9.5f,-4.6f),new Vector2(-9.5f,-2.65f),new Vector2(-9.5f,-1.10f),new Vector2(-10.05f,-1.65f),new Vector2(-11.05f,-1.65f),new Vector2(-10.05f,-1.65f),new Vector2(-10.05f,-.60f),new Vector2(-11.05f,-.60f),new Vector2(-10.05f,-.60f),new Vector2(-10.05f,-1.10f),new Vector2(-9.30f,-1.10f),new Vector2(-8.25f,-1.10f),new Vector2(-7.65f,-1.10f),new Vector2(-7.65f,-.95f),new Vector2(-7.65f,-2.05f),new Vector2(-7.65f,-1.10f),new Vector2(-9.5f,-1.10f),new Vector2(-9.5f,-2.65f),new Vector2(-9.5f,-4.6f)});
    }
}
