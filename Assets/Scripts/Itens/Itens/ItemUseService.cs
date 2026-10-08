using UnityEngine;

/// <summary>Centraliza o uso de consumíveis pelo clique direito e pela barra rápida.</summary>
public static class ItemUseService
{
    public static bool TryUse(GameObject itemObject, IItemSlot slot)
    {
        Item item = itemObject != null ? itemObject.GetComponent<Item>() : null;
        PlayerVitals vitals = Object.FindAnyObjectByType<PlayerVitals>();
        if (item == null || slot == null || slot is EquipmentSlot ||
            slot.CurrentItem != itemObject || vitals == null || vitals.IsDead)
            return false;

        bool used = false;
        if (item.healAmount > 0 && vitals.CurrentHealth < vitals.MaxHealth)
            used = vitals.Heal(item.healAmount);
        else if (item.staminaRestore > 0 && vitals.CurrentStamina < vitals.MaxStamina)
        {
            vitals.RestoreStamina(item.staminaRestore);
            used = true;
        }
        else if (item.speedMultiplier > 1f && item.speedDuration > 0f)
        {
            PlayerController controller = vitals.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ApplySpeedBoost(item.speedMultiplier, item.speedDuration);
                used = true;
            }
        }
        else if (item.fireImmunitySeconds > 0f || item.poisonImmunitySeconds > 0f)
        {
            vitals.GrantStatusImmunity(item.fireImmunitySeconds, item.poisonImmunitySeconds);
            used = true;
        }

        if (!used) return false;
        GameAudio.PlayConsume();
        ItemStack stack = ItemStack.Ensure(itemObject);
        if (stack != null && stack.Quantity > 1)
        {
            stack.RemoveUpTo(1);
        }
        else if (slot.TryRemoveItem(itemObject))
        {
            Object.Destroy(itemObject);
        }
        Object.FindAnyObjectByType<SaveController>()?.SaveGame();
        return true;
    }
}
