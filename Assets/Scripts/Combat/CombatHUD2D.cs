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
        if (playerVitals == null || Time.timeScale == 0f)
        {
            return;
        }

        DrawResourcePanel();
        DrawAbilityCross();
        DrawControlsHint();
    }

    private void DrawResourcePanel()
    {
        const float x = 20f;
        const float width = 270f;
        const float labelHeight = 20f;
        GUI.Box(new Rect(x, 20f, width, playerVitals.IsBurning ? 162f : 140f), GUIContent.none);

        GUI.Label(new Rect(x + 12f, 27f, width - 24f, labelHeight), $"Vida  {playerVitals.CurrentHealth}/{playerVitals.MaxHealth}");
        DrawBar(new Rect(x + 12f, 49f, width - 24f, 14f), (float)playerVitals.CurrentHealth / playerVitals.MaxHealth, new Color(0.8f, 0.18f, 0.18f));

        GUI.Label(new Rect(x + 12f, 68f, width - 24f, labelHeight), $"Estamina  {playerVitals.CurrentEnergy}/{playerVitals.MaxEnergy}");
        DrawBar(new Rect(x + 12f, 90f, width - 24f, 14f), (float)playerVitals.CurrentEnergy / playerVitals.MaxEnergy, new Color(0.2f, 0.58f, 0.95f));

        if (progression != null)
        {
            string xpText = progression.IsAtMaximumLevel
                ? $"XP  Nivel {progression.Level} - maximo"
                : $"XP  Nivel {progression.Level} | {progression.CurrentExperience}/{progression.ExperienceRequiredForNextLevel}";
            GUI.Label(new Rect(x + 12f, 110f, width - 24f, labelHeight), xpText);
            DrawBar(new Rect(x + 12f, 132f, width - 24f, 12f), progression.ExperienceProgress, new Color(0.55f, 0.78f, 0.25f));
            if (progression.ShowLevelUpNotification)
            {
                GUI.color = new Color(1f, 0.88f, 0.36f);
                GUI.Label(new Rect(x + width + 8f, 27f, 150f, 22f), "Novo nivel!");
                GUI.color = Color.white;
            }
        }

        if (playerVitals.IsBurning)
        {
            GUI.color = new Color(1f, 0.45f, 0.12f);
            GUI.Label(new Rect(x + 12f, 145f, width - 24f, 22f), "Queimando!", new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            });
            GUI.color = Color.white;
        }
    }

    private void DrawAbilityCross()
    {
        float tile = 52f;
        float gap = 4f;
        float centerX = 26f;
        float centerY = Screen.height - 126f;

        DrawAbilityTile(new Rect(centerX + tile + gap, centerY - tile - gap, tile, tile), "Z", GetContactAbilityOne());
        DrawAbilityTile(new Rect(centerX, centerY, tile, tile), "X", GetContactAbilityTwo());
        DrawAbilityTile(new Rect(centerX + (tile + gap) * 2f, centerY, tile, tile), "C", GetAbsorptionAbilityOne());
        DrawAbilityTile(new Rect(centerX + tile + gap, centerY + tile + gap, tile, tile), "V", GetAbsorptionAbilityTwo());
    }

    private void DrawControlsHint()
    {
        const float width = 530f;
        const float height = 136f;
        float x = Mathf.Max(10f, Screen.width - width - 18f);
        float y = Mathf.Max(10f, Screen.height - height - 18f);
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13,
            wordWrap = true
        };
        string blockInfo = equipment != null && equipment.EquippedShield == InventoryItemId.Shield
            ? "Escudo: bloqueia 50% e custa 10 estamina por golpe"
            : equipment != null && equipment.CanBlock
                ? "Espada: bloqueia 15% e custa 15 estamina por golpe"
                : "Bloqueio exige escudo ou espada equipada";
        GUI.Box(new Rect(x, y, width, height),
            "Espaco: dash | Ctrl: correr | F: interagir\n" +
            "E: inventario | I: menu | B: mochila (se equipada)\n" +
            "Clique esquerdo: golpe leve | Segure esquerdo: golpe pesado\n" +
            "Clique direito: arma secundaria | Segure direito: bloquear | Tab: trocar armas\n" + blockInfo,
            style);
    }

    private string GetContactAbilityOne()
    {
        if (powerLoadout == null) return "-";
        if (powerLoadout.EquippedContactPower == ContactPowerId.SpoonMagnetism)
        {
            int scrap = materialPouch != null ? materialPouch.MetalScrapCount : 0;
            return scrap > 0 ? "Magnetismo" : "Sem metal";
        }
        return powerLoadout.EquippedContactPower == ContactPowerId.RedGloveStrength ? "Soco forte" : "-";
    }

    private string GetContactAbilityTwo()
    {
        return powerLoadout != null && powerLoadout.EquippedContactPower == ContactPowerId.RedGloveStrength
            ? "Impacto em area"
            : "-";
    }

    private string GetAbsorptionAbilityOne()
    {
        return powerLoadout != null && powerLoadout.ActiveAbsorptionPower == AbsorptionPowerId.PoisonApple
            ? "Veneno"
            : "-";
    }

    private string GetAbsorptionAbilityTwo()
    {
        return "-";
    }

    private static void DrawAbilityTile(Rect rect, string key, string abilityName)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            wordWrap = true
        };
        GUI.Box(rect, key + "\n" + abilityName, style);
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
