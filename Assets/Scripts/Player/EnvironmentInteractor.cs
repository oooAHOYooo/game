using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class EnvironmentInteractor : MonoBehaviour
{
    private NinjaController _controller;
    private GameObject      _heldObject;
    private bool            _isHolding = false;

    void Start()
    {
        _controller = GetComponent<NinjaController>();
    }

    void Update()
    {
        if (_controller.IsGhost) return;

        // Map Interact to North button (Y/Triangle/X) or E/Q
        bool interactPressed = false;
        
        if (_controller.PlayerIndex == 0 && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
        }

        if (_controller.RefreshGamepadForInteractor() != null)
        {
            if (_controller.RefreshGamepadForInteractor().buttonNorth.wasPressedThisFrame) interactPressed = true;
        }

        if (interactPressed)
        {
            if (_isHolding) ThrowHeldObject();
            else TryPickUp();
        }
    }

    void TryPickUp()
    {
        float radius = 5f * GameSettings.NinjaScale;
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 2f, radius);
        
        foreach (var h in hits)
        {
            if (h.gameObject.name == "Tree" || h.gameObject.name == "Rock")
            {
                PickUp(h.gameObject);
                break;
            }
        }
    }

    void PickUp(GameObject obj)
    {
        _heldObject = obj;
        _isHolding  = true;

        // Disable physics/colliders while held
        var rb = _heldObject.GetComponent<Rigidbody>();
        if (rb == null) rb = _heldObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Parent to hand
        _heldObject.transform.SetParent(_controller.WeaponHolder);
        _heldObject.transform.localPosition = new Vector3(0, 1f, 0);
        _heldObject.transform.localRotation = Quaternion.identity;
    }

    void ThrowHeldObject()
    {
        if (_heldObject == null) return;

        _heldObject.transform.SetParent(null);
        var rb = _heldObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 throwDir = transform.forward + Vector3.up * 0.2f;
        rb.AddForce(throwDir.normalized * GameSettings.ThrowForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

        // Add impact script
        var thrown = _heldObject.AddComponent<ThrownObject>();
        thrown.Owner = transform;
        thrown.Damage = 100f * GameSettings.NinjaScale;

        _heldObject = null;
        _isHolding  = false;
    }
}

public class ThrownObject : MonoBehaviour
{
    public Transform Owner;
    public float     Damage;
    private bool     _hit = false;

    void OnCollisionEnter(Collision collision)
    {
        if (_hit) return;
        if (collision.transform == Owner) return;

        _hit = true;

        // Impact explosion
        SplitScreenCamera.ShakeCamera(0, 0.3f, 0.4f);
        SplitScreenCamera.ShakeCamera(1, 0.3f, 0.4f);

        Collider[] targets = Physics.OverlapSphere(transform.position, 8f);
        foreach (var t in targets)
        {
            var eb = t.GetComponentInParent<EnemyBase>();
            if (eb != null) eb.TakeDamage(Damage, Owner);
            
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null) rb.AddExplosionForce(500f, transform.position, 10f);
        }

        // Break the object or just let it lie
        Destroy(gameObject, 5f);
    }
}
