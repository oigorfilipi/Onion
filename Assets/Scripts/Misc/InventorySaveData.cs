using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa ID, slot e quantidade de um item salvo do inventário ou da mochila.
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    public int itemID;
    public int slotIndex; //The index of the slot where the item is placed within our inventory
    public int quantity; //Saves created before stacking have zero here and load as one item.
}
