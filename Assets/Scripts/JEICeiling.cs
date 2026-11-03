using UnityEngine;
// Open roof in Scene view; enclosed interior in Play mode.
public class JEICeiling : MonoBehaviour
{
    void Awake() { GetComponent<Renderer>().enabled=true; }
}
