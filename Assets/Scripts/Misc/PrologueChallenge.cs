using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conta o tempo apenas no mapa externo, exibe o relógio e aciona vitória e autosave.
/// </summary>
public class PrologueChallenge : MonoBehaviour
{
    [SerializeField, Min(1f)] private float durationSeconds = 600f;
    [SerializeField, Min(1f)] private float autosaveIntervalSeconds = 10f;
    [SerializeField] private bool countTimeInThisScene = true;

    private SaveController saveController;
    private DeathScreenController endScreen;
    private PlayerVitals playerVitals;
    private MenuController inventoryMenu;
    private OnionMenuController pauseMenu;
    private GameObject timerPanel;
    private TMP_Text timerText;
    private int savedCheckpoint = -1;
    private int survivalCoinMilestone;
    public bool HasTimeLimit => GameSession.CurrentDifficulty != GameSession.Difficulty.Insane;
    public float DurationSeconds => GameSession.CurrentDifficulty switch
    {
        GameSession.Difficulty.Easy => 180f,
        GameSession.Difficulty.Hard => 900f,
        _ => durationSeconds
    };

    private void Start()
    {
        saveController = FindAnyObjectByType<SaveController>();
        endScreen = FindAnyObjectByType<DeathScreenController>();
        playerVitals = FindAnyObjectByType<PlayerVitals>();
        inventoryMenu = FindAnyObjectByType<MenuController>();
        pauseMenu = FindAnyObjectByType<OnionMenuController>();
        survivalCoinMilestone = Mathf.FloorToInt(GameSession.RunElapsedSeconds / 300f);
        CreateTimerPanel();
    }

    // Só soma Time.deltaTime no mapa externo, respeita pausa/morte e conclui os modos com tempo finito.
    private void Update()
    {
        if (saveController == null || !saveController.IsInitialized) return;
        if (savedCheckpoint < 0)
            savedCheckpoint = Mathf.FloorToInt(GameSession.RunElapsedSeconds / autosaveIntervalSeconds);

        if (GameSession.RunWon)
        {
            endScreen?.ShowVictory();
            return;
        }

        if (!countTimeInThisScene || playerVitals == null || playerVitals.IsDead || Time.timeScale <= 0f ||
            (pauseMenu != null && pauseMenu.IsPauseOpenOrOpening)) return;

        GameSession.AddRunTime(Time.deltaTime);
        int reachedRewardMilestone = Mathf.FloorToInt(GameSession.RunElapsedSeconds / 300f);
        while (survivalCoinMilestone < reachedRewardMilestone)
        {
            survivalCoinMilestone++;
            GameSession.AddCoins(10);
        }
        if (HasTimeLimit && GameSession.RunElapsedSeconds >= DurationSeconds)
        {
            GameSession.MarkRunWon();
            saveController.SaveGame();
            endScreen?.ShowVictory();
            return;
        }

        int checkpoint = Mathf.FloorToInt(GameSession.RunElapsedSeconds / autosaveIntervalSeconds);
        if (checkpoint > savedCheckpoint)
        {
            savedCheckpoint = checkpoint;
            saveController.SaveGame();
        }
    }

    // Esconde o relógio em menus e dentro da casa; Insano mostra tempo crescente.
    private void LateUpdate()
    {
        if (timerPanel == null) return;
        bool inventoryOpen = inventoryMenu != null && inventoryMenu.menuCanvas != null &&
                             inventoryMenu.menuCanvas.activeInHierarchy;
        bool visible = countTimeInThisScene && saveController != null && saveController.IsInitialized &&
                       playerVitals != null && !playerVitals.IsDead && !GameSession.RunWon &&
                       Time.timeScale > 0f && !inventoryOpen && !NpcDialogueUI.IsDialogueOpen &&
                       (pauseMenu == null || !pauseMenu.IsPauseOpenOrOpening) &&
                       (endScreen == null || !endScreen.IsShowing);
        timerPanel.SetActive(visible);
        if (!visible || timerText == null) return;

        int seconds = HasTimeLimit
            ? Mathf.CeilToInt(Mathf.Max(0f, DurationSeconds - GameSession.RunElapsedSeconds))
            : Mathf.FloorToInt(GameSession.RunElapsedSeconds);
        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void CreateTimerPanel()
    {
        Transform existing = transform.Find("ChallengeTimer");
        if (existing != null)
        {
            timerPanel = existing.gameObject;
            timerText = existing.GetComponentInChildren<TMP_Text>(true);
            return;
        }
        if (endScreen == null) endScreen = FindAnyObjectByType<DeathScreenController>();
        if (endScreen == null || endScreen.HeadlineTemplate == null) return;

        timerPanel = new GameObject("ChallengeTimer", typeof(RectTransform), typeof(Image));
        timerPanel.transform.SetParent(transform, false);
        RectTransform panelRect = timerPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(320f, 82f);

        Image panelImage = timerPanel.GetComponent<Image>();
        Image hudImage = FindAnyObjectByType<PlayerVitalsHUD>()?.GetComponent<Image>();
        if (hudImage != null)
        {
            panelImage.sprite = hudImage.sprite;
            panelImage.type = hudImage.type;
            panelImage.color = hudImage.color;
        }
        else
        {
            panelImage.color = new Color(0.15f, 0.11f, 0.08f, 0.85f);
        }
        panelImage.raycastTarget = false;

        timerText = Instantiate(endScreen.HeadlineTemplate, timerPanel.transform, false);
        timerText.name = "TimeRemainingText";
        timerText.text = "10:00";
        timerText.color = new Color(1f, 0.95f, 0.72f);
        timerText.fontSize = 38f;
        timerText.enableAutoSizing = false;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.raycastTarget = false;
        RectTransform textRect = timerText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
    }

    /// <summary>Permite que o Editor grave o relógio como objeto comum da cena.</summary>
    public void BuildEditableTimer() => CreateTimerPanel();
}
