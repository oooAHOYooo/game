using UnityEngine;
using System.Collections;

/// <summary>
/// GhostAI — autonomous behaviour for Player 2 when no second controller is connected.
/// Mimics Player 1's tactics with slight lag and personality quirks.
/// </summary>
public class GhostAI : MonoBehaviour
{
    public NinjaController Controller;

    // ── Settings ──────────────────────────────────────────────────────────
    [Header("Ghost Settings")]
    public float ActionInterval   = 0.8f;   // how often ghost re-evaluates action
    public float FollowRadius     = 8f;     // tries to stay within this range of P1
    public float AttackRange      = 2.5f;
    public float LaserRange       = 14f;
    public float ReactionDelay    = 0.18f;  // slight lag for human feel

    // ── State ─────────────────────────────────────────────────────────────
    private NinjaController _player1;
    private EnemyBase       _currentTarget;
    private float           _actionTimer;
    private bool            _isActing;
    private Vector3         _desiredMove;
    private bool            _wantsToFly;
    private float           _laserCooldown;

    private enum GhostState { Following, Attacking, Supporting, Repositioning }
    private GhostState _state = GhostState.Following;

    void Start()
    {
        // Find Player 1
        var allControllers = FindObjectsByType<NinjaController>(FindObjectsSortMode.None);
        foreach (var c in allControllers)
            if (c.PlayerIndex == 0 && !c.IsGhost) { _player1 = c; break; }

        StartCoroutine(GhostBehaviourLoop());
    }

    void Update()
    {
        if (Controller == null) return;
        ApplyMovement();
        _laserCooldown = Mathf.Max(0, _laserCooldown - Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────
    // BEHAVIOUR LOOP
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator GhostBehaviourLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(ActionInterval + Random.Range(0f, 0.2f));
            EvaluateState();
            yield return new WaitForSeconds(ReactionDelay);
            ExecuteState();
        }
    }

    void EvaluateState()
    {
        // Find nearest active enemy
        _currentTarget = FindNearestActiveEnemy();

        if (_currentTarget == null)
        {
            _state = GhostState.Following;
            return;
        }

        float distToEnemy = Vector3.Distance(transform.position, _currentTarget.transform.position);
        float distToP1    = _player1 != null ? Vector3.Distance(transform.position, _player1.transform.position) : 0;

        if (distToEnemy <= AttackRange)
            _state = GhostState.Attacking;
        else if (distToEnemy <= LaserRange && Controller.CurrentKi > 40f && _laserCooldown <= 0)
            _state = GhostState.Supporting;
        else if (distToP1 > FollowRadius)
            _state = GhostState.Repositioning;
        else
            _state = GhostState.Following;
    }

    void ExecuteState()
    {
        if (Controller == null) return;

        switch (_state)
        {
            case GhostState.Following:
                FollowPlayer1();
                break;

            case GhostState.Attacking:
                MoveToward(_currentTarget.transform.position);
                var rng = Random.value;
                if (rng < 0.6f) Controller.AIAttackLight();
                else            Controller.AIAttackHeavy();
                break;

            case GhostState.Supporting:
                // Strafe around enemy while firing laser
                Vector3 side = Vector3.Cross(
                    (_currentTarget.transform.position - transform.position).normalized,
                    Vector3.up
                ) * (Random.value > 0.5f ? 1 : -1);
                _desiredMove = side;
                _wantsToFly  = _currentTarget.IsFlying;
                Controller.AIFireLaser();
                _laserCooldown = 2.5f;
                break;

            case GhostState.Repositioning:
                FollowPlayer1();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void FollowPlayer1()
    {
        if (_player1 == null) return;

        // Offset slightly behind/beside P1
        Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-2f, -4f));
        Vector3 target = _player1.transform.position + _player1.transform.TransformDirection(offset);
        MoveToward(target);
        _wantsToFly = _player1.IsFlying;
    }

    void MoveToward(Vector3 worldPos)
    {
        Vector3 dir = (worldPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.5f) { _desiredMove = Vector3.zero; return; }
        _desiredMove = dir.normalized;
    }

    void ApplyMovement()
    {
        // Occasionally add jitter so ghost feels alive
        Vector3 jitter = new Vector3(
            Mathf.PerlinNoise(Time.time * 1.3f, 0) - 0.5f,
            0,
            Mathf.PerlinNoise(0, Time.time * 1.3f) - 0.5f
        ) * 0.25f;

        Controller.AIMove(_desiredMove + jitter, _wantsToFly);
    }

    EnemyBase FindNearestActiveEnemy()
    {
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.IsActiveEnemy) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = e; }
        }
        return nearest;
    }
}
