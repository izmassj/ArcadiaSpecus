using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDeathExplosion : MonoBehaviour
{
    [System.Serializable]
    private class PieceRuntimeData
    {
        public Transform transform;
        public Transform originalParent;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 originalLocalScale;
        public bool originalActiveSelf;
        public Rigidbody rigidbody;
        public Collider[] colliders;
        public BoxCollider addedBoxCollider;
    }

    [Header("References")]
    [SerializeField] private Transform _normalVisualRoot;
    [SerializeField] private Transform _physicsBodyRoot;
    [SerializeField] private GameObject _explosionEffectPrefab;

    [Header("Hierarchy Names")]
    [SerializeField] private string _physicsBodyRootName = "PhysicsBody";
    [SerializeField] private string _normalVisualRootName = "Body";
    [SerializeField] private string _pieceContainerName = "RobotSkeleton";

    [Header("Piece Root Names")]
    [SerializeField] private string[] _pieceRootNames =
    {
        "Head",
        "Body",
        "RightHand",
        "LeftHand",
        "RightLeg",
        "LeftLeg"
    };

    [Header("Explosion")]
    [SerializeField] private float _minImpulse = 4.5f;
    [SerializeField] private float _maxImpulse = 8.5f;
    [SerializeField] private float _explosionRadius = 2.5f;
    [SerializeField] private float _upwardModifier = 1.25f;
    [SerializeField] private float _randomDirectionStrength = 0.65f;
    [SerializeField] private float _torqueImpulse = 12f;
    [SerializeField] private Vector3 _explosionOffset = new Vector3(0f, 0.75f, 0f);

    [Header("Physics")]
    [SerializeField] private float _pieceMass = 0.35f;
    [SerializeField] private float _pieceDrag = 0.2f;
    [SerializeField] private float _pieceAngularDrag = 0.05f;
    [SerializeField] private float _minimumColliderSize = 0.08f;
    [SerializeField] private bool _useGravity = true;
    [SerializeField] private bool _addBoxColliderIfMissing = true;
    [SerializeField] private bool _ignoreRobotColliders = true;

    [Header("Visibility")]
    [SerializeField] private bool _detachPiecesWhileDead = true;
    [SerializeField] private bool _hideBrokenPiecesAfterLifetime = false;
    [SerializeField] private float _brokenBodyLifetime = 4f;

    private readonly List<Renderer> _aliveRenderers = new List<Renderer>();
    private readonly List<Collider> _robotColliders = new List<Collider>();
    private readonly List<PieceRuntimeData> _pieces = new List<PieceRuntimeData>();

    private Coroutine _hideBrokenBodyCoroutine;
    private bool _cached;
    private bool _dead;

    private void Awake()
    {
        CacheReferences(true);
        RestoreAliveModel();
    }

    public void PlayDeathExplosion()
    {
        CacheReferences(true);

        if (_physicsBodyRoot == null)
        {
            Debug.LogWarning($"{nameof(RobotDeathExplosion)}: no se ha encontrado el objeto PhysicsBody en {name}.", this);
            return;
        }

        if (_hideBrokenBodyCoroutine != null)
        {
            StopCoroutine(_hideBrokenBodyCoroutine);
            _hideBrokenBodyCoroutine = null;
        }

        _dead = true;

        HideAliveModel();

        _physicsBodyRoot.SetParent(transform, true);
        _physicsBodyRoot.gameObject.SetActive(true);

        Vector3 origin = transform.position + _explosionOffset;

        if (_explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(_explosionEffectPrefab, origin, Quaternion.identity);
            Destroy(effect, GetEffectLifetime(effect));
        }

        PreparePiecesForExplosion();
        ExplodePieces(origin);

        if (_hideBrokenPiecesAfterLifetime)
            _hideBrokenBodyCoroutine = StartCoroutine(HideBrokenBodyAfterLifetime());
    }

    public void RestoreAliveModel()
    {
        CacheReferences();

        if (_hideBrokenBodyCoroutine != null)
        {
            StopCoroutine(_hideBrokenBodyCoroutine);
            _hideBrokenBodyCoroutine = null;
        }

        _dead = false;

        RestorePiecesToOriginalHierarchy();

        if (_physicsBodyRoot != null)
            _physicsBodyRoot.gameObject.SetActive(false);

        for (int i = 0; i < _aliveRenderers.Count; i++)
        {
            if (_aliveRenderers[i] != null)
                _aliveRenderers[i].enabled = true;
        }
    }

    private void CacheReferences(bool forceRefresh = false)
    {
        if (_cached && !forceRefresh)
            return;

        _cached = true;

        ResolveHierarchyReferences();
        CacheAliveRenderers();
        CacheRobotColliders();
        CachePieces();

        if (_physicsBodyRoot != null && !_dead)
            _physicsBodyRoot.gameObject.SetActive(false);
    }

    private void ResolveHierarchyReferences()
    {
        Transform resolvedPhysicsBody = FindDirectChild(transform, _physicsBodyRootName);
        if (resolvedPhysicsBody == null)
            resolvedPhysicsBody = FindChildRecursive(transform, _physicsBodyRootName);

        if (resolvedPhysicsBody != null)
            _physicsBodyRoot = resolvedPhysicsBody;

        if (_normalVisualRoot == null || _normalVisualRoot == _physicsBodyRoot || IsChildOf(_normalVisualRoot, _physicsBodyRoot))
        {
            Transform resolvedNormalBody = FindDirectChildExcluding(transform, _normalVisualRootName, _physicsBodyRoot);
            if (resolvedNormalBody == null)
                resolvedNormalBody = FindChildRecursiveExcluding(transform, _normalVisualRootName, _physicsBodyRoot);

            if (resolvedNormalBody != null)
                _normalVisualRoot = resolvedNormalBody;
        }

        if (_physicsBodyRoot != null && _normalVisualRoot == _physicsBodyRoot)
            _normalVisualRoot = null;
    }

    private void CacheAliveRenderers()
    {
        _aliveRenderers.Clear();

        Transform rendererRoot = _normalVisualRoot != null ? _normalVisualRoot : transform;
        Renderer[] renderers = rendererRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (_physicsBodyRoot != null && renderer.transform.IsChildOf(_physicsBodyRoot))
                continue;

            _aliveRenderers.Add(renderer);
        }
    }

    private void CacheRobotColliders()
    {
        _robotColliders.Clear();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null)
                continue;

            if (_physicsBodyRoot != null && collider.transform.IsChildOf(_physicsBodyRoot))
                continue;

            _robotColliders.Add(collider);
        }
    }

    private void CachePieces()
    {
        _pieces.Clear();

        if (_physicsBodyRoot == null)
            return;

        bool wasPhysicsBodyActive = _physicsBodyRoot.gameObject.activeSelf;
        _physicsBodyRoot.gameObject.SetActive(true);

        Transform pieceContainer = FindChildRecursive(_physicsBodyRoot, _pieceContainerName);
        if (pieceContainer == null)
            pieceContainer = _physicsBodyRoot;

        List<Transform> foundPieces = GetPieceRoots(pieceContainer);

        for (int i = 0; i < foundPieces.Count; i++)
        {
            Transform piece = foundPieces[i];

            if (piece == null)
                continue;

            PieceRuntimeData data = new PieceRuntimeData
            {
                transform = piece,
                originalParent = piece.parent,
                originalLocalPosition = piece.localPosition,
                originalLocalRotation = piece.localRotation,
                originalLocalScale = piece.localScale,
                originalActiveSelf = piece.gameObject.activeSelf
            };

            data.rigidbody = EnsureRigidbody(piece.gameObject);
            data.colliders = EnsureColliders(piece.gameObject, out data.addedBoxCollider);

            SetPiecePhysicsEnabled(data, false);
            _pieces.Add(data);
        }

        _physicsBodyRoot.gameObject.SetActive(wasPhysicsBodyActive);
    }

    private void HideAliveModel()
    {
        for (int i = 0; i < _aliveRenderers.Count; i++)
        {
            if (_aliveRenderers[i] != null)
                _aliveRenderers[i].enabled = false;
        }
    }

    private void PreparePiecesForExplosion()
    {
        for (int i = 0; i < _pieces.Count; i++)
        {
            PieceRuntimeData piece = _pieces[i];

            if (piece == null || piece.transform == null)
                continue;

            piece.transform.gameObject.SetActive(true);
            piece.transform.localScale = piece.originalLocalScale;

            if (_detachPiecesWhileDead)
            {
                Vector3 worldScale = piece.transform.lossyScale;
                piece.transform.SetParent(null, true);
                SetWorldScale(piece.transform, worldScale);
            }

            SetPiecePhysicsEnabled(piece, true);
            IgnoreRobotCollision(piece);
        }
    }

    private void SetWorldScale(Transform target, Vector3 worldScale)
    {
        if (target == null)
            return;

        Transform parent = target.parent;

        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;

        target.localScale = new Vector3(
            SafeScaleDivision(worldScale.x, parentScale.x),
            SafeScaleDivision(worldScale.y, parentScale.y),
            SafeScaleDivision(worldScale.z, parentScale.z)
        );
    }

    private float SafeScaleDivision(float value, float parentValue)
    {
        if (Mathf.Abs(parentValue) < 0.0001f)
            return value;

        return value / parentValue;
    }

    private void ExplodePieces(Vector3 origin)
    {
        for (int i = 0; i < _pieces.Count; i++)
        {
            PieceRuntimeData piece = _pieces[i];

            if (piece == null || piece.transform == null || piece.rigidbody == null)
                continue;

            Rigidbody rb = piece.rigidbody;

            Vector3 randomDirection = Random.insideUnitSphere * _randomDirectionStrength;
            Vector3 direction = rb.worldCenterOfMass - origin;

            if (direction.sqrMagnitude < 0.001f)
                direction = Random.onUnitSphere;

            direction = (direction.normalized + randomDirection).normalized;
            direction.y = Mathf.Abs(direction.y) + _upwardModifier;
            direction.Normalize();

            float impulse = Random.Range(_minImpulse, _maxImpulse);

            rb.AddForce(direction * impulse, ForceMode.Impulse);
            rb.AddExplosionForce(impulse, origin, _explosionRadius, _upwardModifier, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * _torqueImpulse, ForceMode.Impulse);
        }
    }

    private void RestorePiecesToOriginalHierarchy()
    {
        for (int i = 0; i < _pieces.Count; i++)
        {
            PieceRuntimeData piece = _pieces[i];

            if (piece == null || piece.transform == null)
                continue;

            SetPiecePhysicsEnabled(piece, false);

            piece.transform.SetParent(piece.originalParent, false);
            piece.transform.localPosition = piece.originalLocalPosition;
            piece.transform.localRotation = piece.originalLocalRotation;
            piece.transform.localScale = piece.originalLocalScale;
            piece.transform.gameObject.SetActive(piece.originalActiveSelf);
        }
    }

    private void SetPiecePhysicsEnabled(PieceRuntimeData piece, bool enabled)
    {
        if (piece.rigidbody != null)
        {
            piece.rigidbody.isKinematic = !enabled;
            piece.rigidbody.useGravity = enabled && _useGravity;
            piece.rigidbody.detectCollisions = enabled;

            ResetRigidbodyVelocity(piece.rigidbody);

            if (!enabled)
                piece.rigidbody.Sleep();
            else
                piece.rigidbody.WakeUp();
        }

        if (piece.colliders != null)
        {
            for (int i = 0; i < piece.colliders.Length; i++)
            {
                if (piece.colliders[i] != null)
                    piece.colliders[i].enabled = enabled;
            }
        }
    }


    private void ResetRigidbodyVelocity(Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    private void SetRigidbodyDamping(Rigidbody rb, float linearDamping, float angularDamping)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
#else
        rb.drag = linearDamping;
        rb.angularDrag = angularDamping;
#endif
    }

    private Rigidbody EnsureRigidbody(GameObject piece)
    {
        Rigidbody rb = piece.GetComponent<Rigidbody>();

        if (rb == null)
            rb = piece.AddComponent<Rigidbody>();

        rb.mass = Mathf.Max(0.01f, _pieceMass);
        SetRigidbodyDamping(rb, Mathf.Max(0f, _pieceDrag), Mathf.Max(0f, _pieceAngularDrag));
        rb.useGravity = _useGravity;
        rb.isKinematic = true;
        rb.detectCollisions = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        return rb;
    }

    private Collider[] EnsureColliders(GameObject piece, out BoxCollider addedBoxCollider)
    {
        addedBoxCollider = null;

        Collider[] existingColliders = piece.GetComponentsInChildren<Collider>(true);

        if (existingColliders.Length > 0 || !_addBoxColliderIfMissing)
            return existingColliders;

        Bounds? bounds = GetRendererBounds(piece.transform);

        if (!bounds.HasValue)
            return existingColliders;

        addedBoxCollider = piece.AddComponent<BoxCollider>();
        ApplyWorldBoundsToBoxCollider(addedBoxCollider, bounds.Value);

        return piece.GetComponentsInChildren<Collider>(true);
    }

    private Bounds? GetRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds : null;
    }

    private void ApplyWorldBoundsToBoxCollider(BoxCollider box, Bounds worldBounds)
    {
        Transform target = box.transform;
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        Vector3 localMin = target.InverseTransformPoint(corners[0]);
        Vector3 localMax = localMin;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 local = target.InverseTransformPoint(corners[i]);
            localMin = Vector3.Min(localMin, local);
            localMax = Vector3.Max(localMax, local);
        }

        Vector3 localSize = localMax - localMin;
        localSize.x = Mathf.Max(localSize.x, _minimumColliderSize);
        localSize.y = Mathf.Max(localSize.y, _minimumColliderSize);
        localSize.z = Mathf.Max(localSize.z, _minimumColliderSize);

        box.center = (localMin + localMax) * 0.5f;
        box.size = localSize;
    }

    private void IgnoreRobotCollision(PieceRuntimeData piece)
    {
        if (!_ignoreRobotColliders || piece == null || piece.colliders == null)
            return;

        for (int i = 0; i < piece.colliders.Length; i++)
        {
            Collider pieceCollider = piece.colliders[i];

            if (pieceCollider == null)
                continue;

            for (int j = 0; j < _robotColliders.Count; j++)
            {
                Collider robotCollider = _robotColliders[j];

                if (robotCollider != null)
                    Physics.IgnoreCollision(pieceCollider, robotCollider, true);
            }
        }
    }

    private IEnumerator HideBrokenBodyAfterLifetime()
    {
        yield return new WaitForSeconds(_brokenBodyLifetime);

        if (!_dead)
            yield break;

        RestorePiecesToOriginalHierarchy();

        if (_physicsBodyRoot != null)
            _physicsBodyRoot.gameObject.SetActive(false);

        _hideBrokenBodyCoroutine = null;
    }

    private List<Transform> GetPieceRoots(Transform pieceContainer)
    {
        List<Transform> pieces = new List<Transform>();

        for (int i = 0; i < _pieceRootNames.Length; i++)
        {
            Transform piece = FindDirectChild(pieceContainer, _pieceRootNames[i]);

            if (piece != null && !pieces.Contains(piece))
                pieces.Add(piece);
        }

        if (pieces.Count > 0)
            return pieces;

        for (int i = 0; i < pieceContainer.childCount; i++)
        {
            Transform child = pieceContainer.GetChild(i);

            if (child.GetComponentsInChildren<Renderer>(true).Length > 0)
                pieces.Add(child);
        }

        return pieces;
    }

    private Transform FindDirectChildExcluding(Transform root, string childName, Transform excludedRoot)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null || child == excludedRoot || IsChildOf(child, excludedRoot))
                continue;

            if (child.name == childName)
                return child;
        }

        return null;
    }

    private Transform FindChildRecursiveExcluding(Transform root, string childName, Transform excludedRoot)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        if (root == excludedRoot || IsChildOf(root, excludedRoot))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursiveExcluding(root.GetChild(i), childName, excludedRoot);

            if (result != null)
                return result;
        }

        return null;
    }

    private bool IsChildOf(Transform child, Transform possibleParent)
    {
        if (child == null || possibleParent == null)
            return false;

        return child.IsChildOf(possibleParent);
    }

    private Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private float GetEffectLifetime(GameObject effect)
    {
        if (effect == null)
            return 3f;

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        float lifetime = 0f;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];

            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }

        return Mathf.Max(1f, lifetime + 0.5f);
    }
}
