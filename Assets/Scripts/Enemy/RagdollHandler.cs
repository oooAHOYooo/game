using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RagdollHandler — Manages enabling/disabling child rigidbodies and colliders for ragdoll effects.
/// </summary>
public class RagdollHandler : MonoBehaviour
{
    private struct RagdollPart
    {
        public Rigidbody rb;
        public Collider col;
        public Vector3 localPos;
        public Quaternion localRot;
    }

    private List<RagdollPart> _parts = new List<RagdollPart>();
    private Animator _animator;
    private Rigidbody _mainRb;
    private Collider _mainCol;

    public void Setup()
    {
        _animator = GetComponentInChildren<Animator>();
        _mainRb = GetComponent<Rigidbody>();
        _mainCol = GetComponent<Collider>();

        // Gather all rigidbodies in children (excluding the main one)
        Rigidbody[] childRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in childRbs)
        {
            if (rb == _mainRb) continue;

            var col = rb.GetComponent<Collider>();
            if (col == null) continue;

            _parts.Add(new RagdollPart
            {
                rb = rb,
                col = col,
                localPos = rb.transform.localPosition,
                localRot = rb.transform.localRotation
            });

            // Set default state: Kinematic and No Collision
            rb.isKinematic = true;
            col.enabled = false;
        }
    }

    public void EnableRagdoll(bool enabled)
    {
        if (_animator != null) _animator.enabled = !enabled;
        if (_mainCol != null) _mainCol.enabled = !enabled;
        
        // We keep the main RB kinematic so it doesn't fall through the floor while ragdolling
        // but we stop its movement
        if (_mainRb != null)
        {
            if (enabled)
            {
                _mainRb.linearVelocity = Vector3.zero;
                _mainRb.isKinematic = true;
            }
            else
            {
                _mainRb.isKinematic = false;
            }
        }

        foreach (var part in _parts)
        {
            part.rb.isKinematic = !enabled;
            part.col.enabled = enabled;
            
            if (enabled)
            {
                part.rb.linearVelocity = Vector3.zero;
                part.rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void ApplyExplosion(Vector3 force, Vector3 position, float radius)
    {
        foreach (var part in _parts)
        {
            part.rb.AddExplosionForce(force.magnitude * 10f, position, radius, 1f, ForceMode.Impulse);
        }
    }

    public void ApplyImpact(Vector3 force)
    {
        foreach (var part in _parts)
        {
            part.rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
