using UnityEngine;

public sealed class EnemyLootDrop2D : MonoBehaviour
{
    [SerializeField] private InventoryItemId itemId = InventoryItemId.Cheese;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private Color itemColor = new Color(1f, 0.88f, 0.42f);

    private EnemyHealth2D enemyHealth;

    private void OnEnable()
    {
        enemyHealth = GetComponent<EnemyHealth2D>();
        if (enemyHealth != null)
        {
            enemyHealth.Died += DropLoot;
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Died -= DropLoot;
        }
    }

    public void Configure(InventoryItemId id, int amount, Sprite sprite, Color color)
    {
        itemId = id;
        quantity = Mathf.Max(1, amount);
        itemSprite = sprite;
        itemColor = color;
    }

    private void DropLoot(EnemyHealth2D defeatedEnemy)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning("O drop do inimigo precisa de um sprite.", this);
            return;
        }

        WorldItemPickup2D.SpawnItem(itemId, quantity, defeatedEnemy.transform.position, itemSprite, itemColor);
    }
}
