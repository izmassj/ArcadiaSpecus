using System;
using UnityEngine;

public class BunkerResourceManager : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField] private int _scrap;
    [SerializeField] private int _electricity;
    [SerializeField] private int _water;
    [SerializeField] private int _food;

    public event Action<BunkerResourceType, int> ResourcesAdded;

    public int Scrap => _scrap;
    public int Electricity => _electricity;
    public int Water => _water;
    public int Food => _food;

    public int GetAmount(BunkerResourceType resourceType)
    {
        switch (resourceType)
        {
            case BunkerResourceType.Scrap:
                return _scrap;
            case BunkerResourceType.Electricity:
                return _electricity;
            case BunkerResourceType.Water:
                return _water;
            case BunkerResourceType.Food:
                return _food;
            default:
                return 0;
        }
    }

    public void Add(BunkerResourceType resourceType, int amount)
    {
        if (amount <= 0)
            return;

        switch (resourceType)
        {
            case BunkerResourceType.Scrap:
                _scrap += amount;
                break;
            case BunkerResourceType.Electricity:
                _electricity += amount;
                break;
            case BunkerResourceType.Water:
                _water += amount;
                break;
            case BunkerResourceType.Food:
                _food += amount;
                break;
        }

        ResourcesAdded?.Invoke(resourceType, amount);
    }

    public bool TrySpend(BunkerResourceType resourceType, int amount)
    {
        if (amount <= 0)
            return true;

        int currentAmount = GetAmount(resourceType);
        if (currentAmount < amount)
            return false;

        switch (resourceType)
        {
            case BunkerResourceType.Scrap:
                _scrap -= amount;
                break;
            case BunkerResourceType.Electricity:
                _electricity -= amount;
                break;
            case BunkerResourceType.Water:
                _water -= amount;
                break;
            case BunkerResourceType.Food:
                _food -= amount;
                break;
        }

        return true;
    }
}
