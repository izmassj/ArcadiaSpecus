using UnityEngine;

public class TurretPlasmaProjectile : MonoBehaviour
{
    private Transform _ownerRoot;
    private RobotController _player;
    private LevelDeathManager _deathManager;
    private GameObject _explosionPrefab;
    private Vector3 _direction;
    private Vector3 _virtualPosition;
    private float _speed;
    private float _hitRadius;
    private float _lifetime;
    private LayerMask _hitMask;
    private bool _destroyOnObstacleHit;
    private bool _moveVisualRoot;

    private float _lifeTimer;
    private bool _initialized;

    public void Initialize(
        Transform ownerRoot,
        Vector3 direction,
        float speed,
        float hitRadius,
        float lifetime,
        LayerMask hitMask,
        RobotController player,
        LevelDeathManager deathManager,
        GameObject explosionPrefab,
        bool destroyOnObstacleHit,
        bool moveVisualRoot)
    {
        _ownerRoot = ownerRoot;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _virtualPosition = transform.position;
        _speed = Mathf.Max(0.01f, speed);
        _hitRadius = Mathf.Max(0.01f, hitRadius);
        _lifetime = Mathf.Max(0.01f, lifetime);
        _hitMask = hitMask;
        _player = player;
        _deathManager = deathManager;
        _explosionPrefab = explosionPrefab;
        _destroyOnObstacleHit = destroyOnObstacleHit;
        _moveVisualRoot = moveVisualRoot;
        _initialized = true;
    }

    public void Initialize(
        Transform ownerRoot,
        Vector3 direction,
        float speed,
        float hitRadius,
        float lifetime,
        LayerMask hitMask,
        RobotController player,
        LevelDeathManager deathManager,
        GameObject explosionPrefab,
        bool destroyOnObstacleHit)
    {
        Initialize(
            ownerRoot,
            direction,
            speed,
            hitRadius,
            lifetime,
            hitMask,
            player,
            deathManager,
            explosionPrefab,
            destroyOnObstacleHit,
            true);
    }

    private void Update()
    {
        if (!_initialized)
            return;

        float step = _speed * Time.deltaTime;
        Vector3 start = _virtualPosition;
        Vector3 end = start + _direction * step;
        Vector3 sweepDirection = end - start;
        float sweepDistance = sweepDirection.magnitude;

        if (sweepDistance > 0f && Physics.SphereCast(start, _hitRadius, sweepDirection.normalized, out RaycastHit hit, sweepDistance, _hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null)
            {
                Transform hitTransform = hit.collider.transform;

                if (_ownerRoot != null && hitTransform.IsChildOf(_ownerRoot))
                {
                    SetVirtualPosition(end);
                }
                else
                {
                    RobotController robot = hitTransform.GetComponentInParent<RobotController>();
                    if (robot != null)
                    {
                        Vector3 explosionPosition = robot.transform.position + Vector3.up * 0.8f;
                        if (_explosionPrefab != null)
                            Object.Instantiate(_explosionPrefab, explosionPosition, Quaternion.identity);

                        if (_deathManager != null)
                            _deathManager.KillPlayer();

                        Destroy(gameObject);
                        return;
                    }

                    if (_destroyOnObstacleHit)
                    {
                        Destroy(gameObject);
                        return;
                    }

                    SetVirtualPosition(end);
                }
            }
        }
        else
        {
            SetVirtualPosition(end);
        }

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _lifetime)
            Destroy(gameObject);
    }

    private void SetVirtualPosition(Vector3 position)
    {
        _virtualPosition = position;

        if (_moveVisualRoot)
            transform.position = position;
    }
}
