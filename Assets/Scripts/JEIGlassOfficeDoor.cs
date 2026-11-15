using UnityEngine;

/// <summary>A quiet-office door that opens inward for the active walking tour.</summary>
public sealed class JEIGlassOfficeDoor : MonoBehaviour
{
    [SerializeField] Vector3 closedLocalEuler = Vector3.zero;
    [SerializeField] Vector3 openLocalEuler = new Vector3(0, -90, 0);
    [SerializeField] float openingDistance = 2f;
    [SerializeField] float closingDistance = 2.5f;
    [SerializeField] float degreesPerSecond = 140f;

    JEITourController player;
    bool wantsOpen;

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        wantsOpen = false;
        SetOpenImmediate(false);
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (!player || !player.isActiveAndEnabled)
            player = FindFirstObjectByType<JEITourController>();

        if (!player || !player.isActiveAndEnabled)
        {
            wantsOpen = false;
        }
        else
        {
            // Measure from the stationary closed-door centre, not the moving
            // leaf, so swinging the door cannot change the proximity threshold.
            Vector3 localCentre = transform.localPosition +
                Quaternion.Euler(closedLocalEuler) * new Vector3(0, 0, .58f);
            Vector3 centre = transform.parent ? transform.parent.TransformPoint(localCentre) : localCentre;
            Vector3 delta = player.transform.position - centre;
            delta.y = 0;
            if (delta.sqrMagnitude < openingDistance * openingDistance) wantsOpen = true;
            else if (delta.sqrMagnitude > closingDistance * closingDistance) wantsOpen = false;
        }

        Quaternion target = Quaternion.Euler(wantsOpen ? openLocalEuler : closedLocalEuler);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target,
            degreesPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// Allows an editor geometry probe to move the assembled leaf without
    /// changing the saved default, runtime proximity state, or door settings.
    /// </summary>
    public void SetOpenImmediate(bool open)
    {
        transform.localRotation = Quaternion.Euler(open ? openLocalEuler : closedLocalEuler);
    }
}
