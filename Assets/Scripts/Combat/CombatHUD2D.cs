using UnityEngine;

public sealed class CombatHUD2D : MonoBehaviour
{
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private PlayerPowerLoadout2D powerLoadout;
    [SerializeField] private PlayerMaterialPouch2D materialPouch;
    [SerializeField] private PlayerEquipment2D equipment;
    [SerializeField] private PlayerInventory2D inventory;
    [SerializeField] private PlayerProgression2D progression;

    private void Awake()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (playerVitals == null) playerVitals = player.GetComponent<PlayerVitals>();
            if (powerLoadout == null) powerLoadout = player.GetComponent<PlayerPowerLoadout2D>();
            if (materialPouch == null) materialPouch = player.GetComponent<PlayerMaterialPouch2D>();
            if (equipment == null) equipment = player.GetComponent<PlayerEquipment2D>();
            if (inventory == null) inventory = player.GetComponent<PlayerInventory2D>();
            if (progression == null) progression = player.GetComponent<PlayerProgression2D>();
        }
    }

    private void OnGUI()
    {
        if (playerVitals == null)
        {
            return;
        }

        float panelWidth = 270f;
        float x = Mathf.Max(10f, Screen.width - panelWidth - 20f);
        Rect panel = new Rect(x, 20f, panelWidth, 360f);
        GUI.Box(panel, GUIContent.none);

        GUI.Label(new Rect(x + 12f, 27f, panelWidth - 24f, 20f), $"Vida  {playerVitals.CurrentHealth}/{playerVitals.MaxHealth}");
        DrawBar(new Rect(x + 12f, 49f, panelWidth - 24f, 14f), (float)playerVitals.CurrentHealth / playerVitals.MaxHealth, new Color(0.8f, 0.18f, 0.18f));

        GUI.Label(new Rect(x + 12f, 68f, panelWidth - 24f, 20f), $"Energia  {playerVitals.CurrentEnergy}/{playerVitals.MaxEnergy}");
        DrawBar(new Rect(x + 12f, 90f, panelWidth - 24f, 14f), (float)playerVitals.CurrentEnergy / playerVitals.MaxEnergy, new Color(0.2f, 0.58f, 0.95f));
        bool bowEquipped = equipment != null && equipment.EquippedWeapon == InventoryItemId.Bow;
        bool swordEquipped = equipment != null && equipment.EquippedWeapon == InventoryItemId.Sword;
        string weaponControls = bowEquipped
            ? "Esq: arco (1 flecha) | Dir: pesado"
            : swordEquipped
                ? "Esq: espada | Dir: pesado"
                : "Esq: ataque basico | Dir: pesado";
        GUI.Label(new Rect(x + 12f, 108f, panelWidth - 24f, 20f), weaponControls);

        string contactPower = "nenhum";
        if (powerLoadout != null)
        {
            if (powerLoadout.EquippedContactPower == ContactPowerId.SpoonMagnetism) contactPower = "Magnetismo";
            else if (powerLoadout.EquippedContactPower == ContactPowerId.RedGloveStrength) contactPower = "Forca da luva";
        }
        int metalCount = materialPouch != null ? materialPouch.MetalScrapCount : 0;
        string absorptionPower = powerLoadout != null && powerLoadout.ActiveAbsorptionPower == AbsorptionPowerId.PoisonApple
            ? "Veneno"
            : "nenhum";
        GUI.Label(new Rect(x + 12f, 130f, panelWidth - 24f, 18f), $"Contato: {contactPower} | Metal: {metalCount}");
        GUI.Label(new Rect(x + 12f, 146f, panelWidth - 24f, 18f), $"Absorcao: {absorptionPower}");
        string contactAbilityOne = "Q: sem poder de Contato ativo";
        string contactAbilityTwo = "F/G: sem poder de Contato ativo";
        if (contactPower == "Magnetismo")
        {
            contactAbilityOne = "Q: magnetismo (1 metal + 15 energia)";
            contactAbilityTwo = "F/G: requer a luva vermelha";
        }
        else if (contactPower == "Forca da luva")
        {
            contactAbilityOne = "F: soco forte (15 energia)";
            contactAbilityTwo = "G: impacto em area (25 energia)";
        }
        GUI.Label(new Rect(x + 12f, 164f, panelWidth - 24f, 18f), contactAbilityOne);
        GUI.Label(new Rect(x + 12f, 182f, panelWidth - 24f, 18f), contactAbilityTwo);
        GUI.Label(new Rect(x + 12f, 200f, panelWidth - 24f, 18f), absorptionPower == "Veneno"
            ? "Direito: veneno (20 energia)"
            : "Direito: ataque pesado (25 energia)");
        GUI.Label(new Rect(x + 12f, 218f, panelWidth - 24f, 18f), "Tab: alternar poder de Contato");
        int arrowCount = inventory != null ? inventory.CountItem(InventoryItemId.Arrow) : 0;
        string weaponName = equipment == null || equipment.EquippedWeapon == InventoryItemId.None
            ? "nenhuma"
            : InventoryItemCatalog.GetDisplayName(equipment.EquippedWeapon);
        GUI.Label(new Rect(x + 12f, 238f, panelWidth - 24f, 18f), $"Arma: {weaponName} | Flechas: {arrowCount}");

        if (progression != null)
        {
            string experienceText = progression.IsAtMaximumLevel
                ? $"Nivel {progression.Level} | nivel maximo"
                : $"Nivel {progression.Level} | EXP {progression.CurrentExperience}/{progression.ExperienceRequiredForNextLevel}";
            GUI.Label(new Rect(x + 12f, 260f, panelWidth - 24f, 18f), experienceText);
            DrawBar(new Rect(x + 12f, 282f, panelWidth - 24f, 14f), progression.ExperienceProgress, new Color(0.55f, 0.78f, 0.25f));
            if (progression.ShowLevelUpNotification)
            {
                GUI.color = new Color(1f, 0.88f, 0.36f);
                GUI.Label(new Rect(x + 12f, 301f, panelWidth - 24f, 18f), "Novo nivel!");
                GUI.color = Color.white;
            }
        }

        GUI.Label(new Rect(x + 12f, 320f, panelWidth - 24f, 18f), "Shift: correr | Espaco: dash");
        int damageReductionPercent = Mathf.RoundToInt((1f - playerVitals.BlockDamageMultiplier) * 100f);
        GUI.Label(new Rect(x + 12f, 338f, panelWidth - 24f, 18f), playerVitals.IsBlocking
            ? $"Ctrl: defesa ativa ({damageReductionPercent}% menos dano)"
            : "Segure Ctrl para defender");
    }

    private static void DrawBar(Rect rect, float normalizedValue, Color fillColor)
    {
        GUI.color = new Color(0.12f, 0.12f, 0.12f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = fillColor;
        Rect fill = rect;
        fill.width *= Mathf.Clamp01(normalizedValue);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
