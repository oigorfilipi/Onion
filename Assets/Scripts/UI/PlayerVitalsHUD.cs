using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Atualiza barras de vida, estamina e XP e oculta o HUD quando outra tela assume o foco.
/// </summary>
public class PlayerVitalsHUD : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private Image experienceFill;
    [SerializeField] private TMP_Text experienceText;

    private PlayerVitals playerVitals;
    private MenuController inventoryMenu;
    private OnionMenuController pauseMenu;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnEnable()
    {
        ConnectToPlayer();
    }

    private void Start()
    {
        ConnectToPlayer();
        inventoryMenu = FindAnyObjectByType<MenuController>();
        pauseMenu = FindAnyObjectByType<OnionMenuController>();
    }

    private void LateUpdate()
    {
        if (inventoryMenu == null) inventoryMenu = FindAnyObjectByType<MenuController>();
        if (pauseMenu == null) pauseMenu = FindAnyObjectByType<OnionMenuController>();

        bool inventoryOpen = inventoryMenu != null && inventoryMenu.menuCanvas != null &&
                             inventoryMenu.menuCanvas.activeInHierarchy;
        bool show = playerVitals != null && !playerVitals.IsDead &&
                    Time.timeScale > 0f && !inventoryOpen &&
                    !NpcDialogueUI.IsDialogueOpen &&
                    (pauseMenu == null || !pauseMenu.IsPauseOpenOrOpening);
        canvasGroup.alpha = show ? 1f : 0f;
    }

    public void HideImmediately()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        if (playerVitals != null)
        {
            playerVitals.StatsChanged -= Refresh;
        }

        playerVitals = null;
    }

    private void ConnectToPlayer()
    {
        if (playerVitals != null)
        {
            return;
        }

        playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.StatsChanged += Refresh;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (playerVitals == null)
        {
            return;
        }

        if (healthFill != null)
        {
            healthFill.fillAmount = playerVitals.MaxHealth > 0
                ? (float)playerVitals.CurrentHealth / playerVitals.MaxHealth
                : 0f;
        }

        if (staminaFill != null)
        {
            staminaFill.fillAmount = playerVitals.MaxStamina > 0
                ? (float)playerVitals.CurrentStamina / playerVitals.MaxStamina
                : 0f;
        }

        if (healthText != null)
        {
            healthText.text = $"{playerVitals.CurrentHealth}/{playerVitals.MaxHealth}";
        }

        if (staminaText != null)
        {
            staminaText.text = $"{playerVitals.CurrentStamina}/{playerVitals.MaxStamina}";
        }

        if (experienceFill != null)
        {
            experienceFill.fillAmount = (float)playerVitals.Experience /
                                        playerVitals.ExperienceToNextLevel;
        }

        if (experienceText != null)
        {
            experienceText.text = $"Nível {playerVitals.Level}  •  " +
                                  $"{playerVitals.Experience}/{playerVitals.ExperienceToNextLevel} XP";
        }
    }
}
