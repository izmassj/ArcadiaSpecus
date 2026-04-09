using UnityEngine;

public class BunkerSpawnedNpcRuntime : MonoBehaviour
{
    [SerializeField] private int _prefabIndex;

    public int PrefabIndex => _prefabIndex;

    public void SetPrefabIndex(int prefabIndex)
    {
        _prefabIndex = Mathf.Max(0, prefabIndex);
    }
}
