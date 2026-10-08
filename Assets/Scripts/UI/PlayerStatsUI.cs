using TMPro;
using UnityEngine;

/// <summary>
/// Mostra nome e atributos base e adicionais na aba Player.
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text staminaText;

    private PlayerVitals playerVitals;

    private void OnEnable()
    {
        playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.StatsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (playerVitals != null)
        {
            playerVitals.StatsChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (playerVitals == null)
        {
            return;
        }

        if (playerNameText != null)
        {
            playerNameText.text = $"NOME - {playerVitals.PlayerName}";
        }

        if (healthText != null)
        {
            healthText.text = $"VIDA - {playerVitals.CurrentHealth}/{playerVitals.MaxHealth} " +
                              $"| BASE {playerVitals.BaseHealth} (+{playerVitals.BonusHealth})";
        }

        if (defenseText != null)
        {
            defenseText.text = $"DEFESA - {playerVitals.BaseDefense} (+{playerVitals.BonusDefense})";
        }

        if (staminaText != null)
        {
            staminaText.text = $"ESTAMINA - {playerVitals.CurrentStamina}/{playerVitals.MaxStamina} " +
                               $"| BASE {playerVitals.BaseStamina} (+{playerVitals.BonusStamina})";
        }
    }
}
