using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Relaciona nome e ID a prefabs canônicos usados em coleta, recompensas e carregamento.
/// </summary>
public class ItemDictionary : MonoBehaviour
{
    public List<Item> itemPrefabs;
    private Dictionary<int, GameObject> itemDictionary;
    private Dictionary<string, GameObject> itemPrefabsByName;

    // A ordem desta lista determina os IDs gravados no save; reordená-la exige migração de dados antigos.
    private void Awake()
    {
        itemDictionary = new Dictionary<int, GameObject>();
        itemPrefabsByName = new Dictionary<string, GameObject>();

        if (itemPrefabs == null)
        {
            Debug.LogError("ItemDictionary has no item prefab list assigned.", this);
            return;
        }

        //AutoIncrementIds
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i] != null)
            {
                itemPrefabs[i].ID = i + 1;
            }
        }

        foreach (Item item in itemPrefabs)
        {
            if (item == null)
            {
                continue;
            }

            itemDictionary[item.ID] = item.gameObject;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                Debug.LogError($"Item prefab '{item.name}' has an empty Item Name.", item);
                continue;
            }

            if (itemPrefabsByName.ContainsKey(item.Name))
            {
                Debug.LogError($"More than one item prefab uses the Item Name '{item.Name}'. Names must be unique.", item);
                continue;
            }

            itemPrefabsByName.Add(item.Name, item.gameObject);
        }
    }

    public GameObject GetItemPrefab(int itemID)
    {
        itemDictionary.TryGetValue(itemID, out GameObject prefab);
        if (prefab == null)
        {
            Debug.LogWarning($"Item with ID {itemID} not found in dictionary");
        }
        return prefab;
    }

    public GameObject GetItemPrefab(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName) || itemPrefabsByName == null ||
            !itemPrefabsByName.TryGetValue(itemName, out GameObject prefab))
        {
            Debug.LogWarning($"Item with name '{itemName}' not found in dictionary.");
            return null;
        }

        return prefab;
    }

    public bool TryGetItemPrefab(string itemName, out GameObject prefab)
    {
        prefab = null;
        return !string.IsNullOrWhiteSpace(itemName) && itemPrefabsByName != null &&
               itemPrefabsByName.TryGetValue(itemName, out prefab) && prefab != null;
    }
}
