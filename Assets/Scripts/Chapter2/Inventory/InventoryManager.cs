using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    readonly List<ItemData> items = new List<ItemData>();
    public IReadOnlyList<ItemData> Items { get { return items; } }

    public event Action<ItemData> ItemAdded;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Add(ItemData item)
    {
        if (item == null || items.Contains(item)) return false;
        items.Add(item);
        ItemAdded?.Invoke(item);
        return true;
    }

    public bool Has(string id)
    {
        return items.Exists(i => i != null && i.id == id);
    }
}
