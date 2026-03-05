using UnityEngine;

/// <summary>
/// HazardCurrent — Pushes rigidbodies in a specific direction. Used in Waterfall Cliffs and Beach.
/// </summary>
public class HazardCurrent : MonoBehaviour
{
    public Vector3 ForceDirection = Vector3.forward;
    public float ForceMagnitude = 15f;

    void OnTriggerStay(Collider other)
    {
        var rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            // Apply drift force
            rb.AddForce(ForceDirection.normalized * ForceMagnitude * Time.deltaTime, ForceMode.Acceleration);
        }
    }
}
