using UnityEngine;

/// <summary>Articulated gait driven by actual motion, with walk/run, airborne and landing blends.</summary>
public class JEIAvatarMotion : MonoBehaviour
{
    public Transform hips, chest, head;
    public Transform leftThigh, rightThigh, leftShin, rightShin, leftFoot, rightFoot;
    public Transform leftArm, rightArm, leftForearm, rightForearm;
    float phase, amount, runBlend, airBlend, landing, idleTime;
    bool wasGrounded = true;

    public void Animate(Vector3 velocity, bool running, bool grounded, float verticalSpeed, float dt)
    {
        if (hips == null || dt <= 0) return;
        float speed = velocity.magnitude;
        float blend = 1 - Mathf.Exp(-12 * dt);
        amount = Mathf.Lerp(amount, Mathf.Clamp01(speed / 2.8f), blend);
        runBlend = Mathf.Lerp(runBlend, running && speed > .2f ? 1 : 0, blend);
        airBlend = Mathf.Lerp(airBlend, grounded ? 0 : 1, 1 - Mathf.Exp(-18 * dt));
        if (grounded && !wasGrounded) landing = .055f;
        wasGrounded = grounded;
        landing = Mathf.MoveTowards(landing, 0, dt * .32f);
        idleTime += dt;
        // Travel distance controls cadence, so walking into a wall stops the gait.
        phase += speed * dt * Mathf.Lerp(3.9f, 2.9f, runBlend);
        if (speed > .15f)
        {
            Vector3 local = transform.parent.InverseTransformDirection(velocity);
            Quaternion facing = Quaternion.LookRotation(new Vector3(local.x, 0, local.z));
            transform.localRotation = Quaternion.Slerp(transform.localRotation, facing, 1 - Mathf.Exp(-14 * dt));
        }
        float gait = amount * (1 - airBlend);
        float stride = Mathf.Lerp(27, 44, runBlend) * gait;
        float left = Mathf.Sin(phase), right = -left;
        hips.localPosition = new Vector3(Mathf.Cos(phase) * .012f * gait,
            .94f + Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(.018f, .045f, runBlend) * gait - landing
            + Mathf.Sin(idleTime * 2) * .003f * (1 - amount), 0);
        hips.localRotation = Quaternion.Euler(0, Mathf.Cos(phase) * 4 * gait, Mathf.Cos(phase) * 2 * gait);
        chest.localRotation = Quaternion.Euler(runBlend * gait * 9 + airBlend * 6,
            -Mathf.Cos(phase) * 7 * gait, -Mathf.Cos(phase) * 2 * gait);
        head.localRotation = Quaternion.Euler(-runBlend * gait * 4, Mathf.Cos(phase) * 2 * gait, 0);
        PoseLeg(leftThigh, leftShin, leftFoot, left, stride, gait, airBlend, verticalSpeed, runBlend);
        PoseLeg(rightThigh, rightShin, rightFoot, right, stride, gait, airBlend, verticalSpeed, runBlend);
        leftArm.localRotation = Quaternion.Euler(-left * stride * .85f - airBlend * 18, 0, -6 - airBlend * 8);
        rightArm.localRotation = Quaternion.Euler(-right * stride * .85f - airBlend * 18, 0, 6 + airBlend * 8);
        leftForearm.localRotation = Quaternion.Euler(-12 - runBlend * 55 - Mathf.Max(0, right) * 14 * gait - airBlend * 15, 0, 0);
        rightForearm.localRotation = Quaternion.Euler(-12 - runBlend * 55 - Mathf.Max(0, left) * 14 * gait - airBlend * 15, 0, 0);
    }

    static void PoseLeg(Transform thigh, Transform shin, Transform foot, float wave, float stride,
        float gait, float air, float verticalSpeed, float run)
    {
        float tuck = verticalSpeed > 0 ? 32 : 16;
        thigh.localRotation = Quaternion.Euler(wave * stride - tuck * air, 0, 0);
        shin.localRotation = Quaternion.Euler(Mathf.Max(0, -wave) * Mathf.Lerp(38, 68, run) * gait + air * 48, 0, 0);
        foot.localRotation = Quaternion.Euler(-wave * 12 * gait - air * 12, 0, 0);
    }
}
