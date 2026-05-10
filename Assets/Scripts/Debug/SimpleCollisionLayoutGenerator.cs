using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SimpleCollisionLayoutGenerator : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Transform targetRoot;

    [Header("Generation")]
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private bool removeMeshColliders = true;
    [SerializeField] private bool skipObjectsThatAlreadyHaveCollider = false;
    [SerializeField] private bool markObjectsAsStatic = true;

    [Header("Box Settings")]
    [SerializeField] private Vector3 extraPadding = Vector3.zero;
    [SerializeField] private float minimumSize = 0.01f;

    [ContextMenu("Generate Simple Box Colliders")]
    public void GenerateSimpleBoxColliders()
    {
        Transform root = targetRoot != null ? targetRoot : transform;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactiveObjects);

        int created = 0;
        int skipped = 0;
        int removedMeshColliders = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                skipped++;
                continue;
            }

            GameObject obj = meshFilter.gameObject;

            Collider existingCollider = obj.GetComponent<Collider>();

            if (skipObjectsThatAlreadyHaveCollider && existingCollider != null)
            {
                skipped++;
                continue;
            }

            if (removeMeshColliders)
            {
                MeshCollider[] meshColliders = obj.GetComponents<MeshCollider>();

                foreach (MeshCollider meshCollider in meshColliders)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Undo.DestroyObjectImmediate(meshCollider);
                    else
                        Destroy(meshCollider);
#else
                    Destroy(meshCollider);
#endif
                    removedMeshColliders++;
                }
            }

            BoxCollider boxCollider = obj.GetComponent<BoxCollider>();

            if (boxCollider == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    boxCollider = Undo.AddComponent<BoxCollider>(obj);
                else
                    boxCollider = obj.AddComponent<BoxCollider>();
#else
                boxCollider = obj.AddComponent<BoxCollider>();
#endif
                created++;
            }

            Bounds meshBounds = meshFilter.sharedMesh.bounds;

            Vector3 finalSize = meshBounds.size + extraPadding;

            finalSize.x = Mathf.Max(finalSize.x, minimumSize);
            finalSize.y = Mathf.Max(finalSize.y, minimumSize);
            finalSize.z = Mathf.Max(finalSize.z, minimumSize);

            boxCollider.center = meshBounds.center;
            boxCollider.size = finalSize;
            boxCollider.isTrigger = false;

            if (markObjectsAsStatic)
                obj.isStatic = true;
        }

        Debug.Log($"Generated simple collision layout for '{root.name}'. Created BoxColliders: {created}. Removed MeshColliders: {removedMeshColliders}. Skipped: {skipped}.");
    }

    [ContextMenu("Remove All Mesh Colliders In Children")]
    public void RemoveAllMeshCollidersInChildren()
    {
        Transform root = targetRoot != null ? targetRoot : transform;

        MeshCollider[] meshColliders = root.GetComponentsInChildren<MeshCollider>(includeInactiveObjects);

        int removed = 0;

        foreach (MeshCollider meshCollider in meshColliders)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(meshCollider);
            else
                Destroy(meshCollider);
#else
            Destroy(meshCollider);
#endif
            removed++;
        }

        Debug.Log($"Removed {removed} MeshColliders from '{root.name}'.");
    }

    [ContextMenu("Remove All Box Colliders In Children")]
    public void RemoveAllBoxCollidersInChildren()
    {
        Transform root = targetRoot != null ? targetRoot : transform;

        BoxCollider[] boxColliders = root.GetComponentsInChildren<BoxCollider>(includeInactiveObjects);

        int removed = 0;

        foreach (BoxCollider boxCollider in boxColliders)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(boxCollider);
            else
                Destroy(boxCollider);
#else
            Destroy(boxCollider);
#endif
            removed++;
        }

        Debug.Log($"Removed {removed} BoxColliders from '{root.name}'.");
    }
}