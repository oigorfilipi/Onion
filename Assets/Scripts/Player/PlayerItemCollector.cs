using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converte itens tocados no mapa em unidades de inventário e registra coleta e experiência.
/// </summary>
public class PlayerItemCollector : MonoBehaviour
{
    private InventoryController inventoryController;
    private PlayerVitals playerVitals;

    // Start is called before the first frame update
    void Start()
    {
        inventoryController = FindAnyObjectByType<InventoryController>();
        playerVitals = GetComponentInParent<PlayerVitals>();
    }

    // Apenas a quantidade aceita no inventário sai do chão; uma coleta concluída rende 5 XP e é registrada.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            Item item = collision.GetComponent<Item>();
            if (item == null) return;

            ItemStack worldStack = item.GetComponent<ItemStack>();
            int quantity = worldStack != null ? worldStack.Quantity : 1;
            if (item.Name.ToLowerInvariant().Contains("moeda") || item.Name.ToLowerInvariant().Contains("coin"))
            {
                GameSession.AddCoins(quantity);
                GameAudio.PlayCoin(item.IsRuntimeSpawn);
                playerVitals?.GainExperience(5);
                FindAnyObjectByType<SaveController>()?.RegisterCollectedWorldItem(item);
                Destroy(item.gameObject);
                return;
            }

            if (inventoryController == null) return;

            int added = inventoryController.AddItemQuantity(item, quantity);

            if (added > 0)
            {
                item.PickUp();
                GameAudio.PlayPickup(item.IsRuntimeSpawn);
                if (added == quantity)
                {
                    FindAnyObjectByType<SaveController>()?.RegisterCollectedWorldItem(item);
                    FindAnyObjectByType<SaveController>()?.UpdateRewardDrop(item.RewardDropId, 0);
                    playerVitals?.GainExperience(5);
                    Destroy(item.gameObject);
                }
                else
                {
                    worldStack.SetQuantity(quantity - added);
                    FindAnyObjectByType<SaveController>()?.UpdateRewardDrop(item.RewardDropId,
                                                                          quantity - added);
                }
            }
        }
    }
}
