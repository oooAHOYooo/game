using UnityEngine;

/// <summary>
/// Syncs NinjaController state with Animator parameters
/// Applied to the animated character model
/// </summary>
public class AnimatedCharacter : MonoBehaviour
{
    public NinjaController Controller { get; set; }
    private Animator _animator;
    private Rigidbody _rb;
    private float _speedBlend = 0f;

    void Start()
    {
        if (Controller == null)
            Controller = GetComponent<NinjaController>();

        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();

        if (_animator == null)
        {
            Debug.LogError($"No Animator on {gameObject.name}");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!_animator || !Controller || !_rb) return;

        // Speed blending: Lerp between 0 (idle) and 1 (moving)
        Vector3 velocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        float currentSpeed = velocity.magnitude;
        _speedBlend = Mathf.Lerp(_speedBlend, currentSpeed > 0.5f ? 1f : 0f, Time.deltaTime * 5f);

        // Update animator parameters
        _animator.SetFloat("Speed", _speedBlend);
        _animator.SetBool("IsFlying", Controller.IsFlying);
        _animator.SetBool("IsAttacking", Controller.IsAttacking);
        _animator.SetBool("IsChargingKi", Controller.IsChargingKi);

        // Attack type: 0=none, 1=light punch, 2=heavy punch, 3=light kick, 4=heavy kick
        _animator.SetInteger("AttackType", Controller.IsAttacking ? Controller.LastAttackType : 0);
    }
}
