using UnityEngine;

/// <summary>
/// SnapToTerrain – A utility to keep objects grounded on the procedural island.
/// Works in the Editor [ExecuteInEditMode] for a "drag and drop" feel.
/// </summary>
[ExecuteInEditMode]
public class SnapToTerrain : MonoBehaviour
{
    public float VerticalOffset = 0f;
    public bool ContinuousSnap = true;

    void Update()
    {
        if (ContinuousSnap || !Application.isPlaying)
        {
            DoSnap();
        }
    }

    [ContextMenu("Snap Now")]
    public void DoSnap()
    {
        // Raycast down from high above to find the terrain
        Vector3 origin = transform.position;
        origin.y = 100f; // well above the max height

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f))
        {
            // Only snap to things tagged "Terrain" or with a MeshCollider (which the island has)
            // For now, we assume if it's a MeshCollider it's the island.
            if (hit.collider is MeshCollider)
            {
                Vector3 newPos = transform.position;
                newPos.y = hit.point.y + VerticalOffset;
                transform.position = newPos;
                
                // Optional: align to surface normal
                // transform.up = hit.normal; 
            }
        }
    }
}
