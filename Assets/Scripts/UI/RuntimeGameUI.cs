using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Conecta ou cria os indicadores globais editáveis de moeda, abates, conquistas, histórico e hotbar.</summary>
public class RuntimeGameUI : MonoBehaviour
{
    private static RuntimeGameUI instance;
    private Canvas canvas;
    private TMP_FontAsset uiFont;
    private QuickbarController quickbar;
    private GameObject quickbarRoot;
    private TMP_Text countersText;
    private TMP_Text toastText;
    private Image toastIcon;
    private TMP_Text trophyFallbackText;
    private GameObject toastRoot;
    private TMP_Text noticeText;
    private TMP_Text storyText;
    private GameObject storyRoot;
    private CanvasGroup storyCanvasGroup;
    private Image bossFill;
    private GameObject bossRoot;
    private GameObject historyButtonRoot;
    private GameObject historyRoot;
    private TMP_Text historyText;
    private RectTransform historyContent;
    private ScrollRect historyScroll;
    private float toastUntil;
    private float noticeUntil;
    private float storyUntil;
    private float storyStartedAt;
    private Sprite trophySprite;
    private readonly Queue<AchievementRecord> achievementQueue = new Queue<AchievementRecord>();
    private bool initialized;

    public static RuntimeGameUI Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureCreated()
    {
        if (instance != null) return;
        GameObject uiObject = new GameObject("Onion Runtime UI");
        uiObject.AddComponent<QuickbarController>();
        instance = uiObject.AddComponent<RuntimeGameUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        // Cada cena tem sua própria hierarquia editável; o save restaura a barra rápida ao carregar.
        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        if (initialized) return;
        initialized = true;
        PlayerProfileData ignored = PlayerProfileService.Data;
        TMP_Text template = FindAnyObjectByType<DeathScreenController>()?.HeadlineTemplate;
        if (template == null) template = FindAnyObjectByType<TMP_Text>();
        if (template != null) uiFont = template.font;
        PlayerProfileService.AchievementUnlocked += OnAchievementUnlocked;
        SceneManager.sceneLoaded += OnSceneLoaded;
        quickbar = GetComponentInChildren<QuickbarController>(true);
        if (quickbar == null) quickbar = gameObject.AddComponent<QuickbarController>();

        Transform canvasTransform = transform.Find("RuntimeOverlayCanvas");
        if (canvasTransform != null)
        {
            canvas = canvasTransform.GetComponent<Canvas>();
            BindHierarchy();
            if (quickbar != null && quickbarRoot != null) quickbar.Build(quickbarRoot.transform);
        }
        else
        {
            BuildCanvas();
            BuildHud();
        }
    }

    /// <summary>Cria uma vez os objetos editáveis para que o editor os grave dentro da cena.</summary>
    public void BuildEditableHierarchy()
    {
        TMP_Text template = FindAnyObjectByType<DeathScreenController>()?.HeadlineTemplate;
        if (template == null) template = FindAnyObjectByType<TMP_Text>();
        if (template != null) uiFont = template.font;
        if (canvas == null)
        {
            Transform canvasTransform = transform.Find("RuntimeOverlayCanvas");
            if (canvasTransform != null) canvas = canvasTransform.GetComponent<Canvas>();
        }
        if (quickbar == null) quickbar = GetComponentInChildren<QuickbarController>(true);
        if (quickbar == null) quickbar = gameObject.AddComponent<QuickbarController>();
        if (canvas != null)
        {
            BindHierarchy();
            if (quickbarRoot != null) quickbar.Build(quickbarRoot.transform);
            return;
        }
        BuildCanvas();
        BuildHud();
    }

    private void BindHierarchy()
    {
        if (canvas == null) return;
        countersText = FindText(canvas.transform, "RunCounters");
        toastRoot = FindObject(canvas.transform, "AchievementToast");
        toastText = FindText(toastRoot != null ? toastRoot.transform : null, "AchievementText");
        toastIcon = FindImage(toastRoot != null ? toastRoot.transform : null, "TrophyIcon");
        trophyFallbackText = FindText(toastRoot != null ? toastRoot.transform : null, "TrophyFallback");
        noticeText = FindText(canvas.transform, "SpawnNotice");
        storyRoot = FindObject(canvas.transform, "StoryOverlay");
        storyText = FindText(storyRoot != null ? storyRoot.transform : null, "StoryText");
        storyCanvasGroup = storyRoot != null ? storyRoot.GetComponent<CanvasGroup>() : null;
        bossRoot = FindObject(canvas.transform, "BossHealthHud");
        bossFill = FindImage(bossRoot != null ? bossRoot.transform : null, "BossHealthFill");
        quickbarRoot = FindObject(canvas.transform, "QuickbarBackground");
        historyButtonRoot = FindObject(canvas.transform, "HistoryButton");
        historyRoot = FindObject(canvas.transform, "HistoryPanel");
        Button historyButton = historyButtonRoot != null ? historyButtonRoot.GetComponent<Button>() : null;
        if (historyButton != null)
        {
            historyButton.onClick.RemoveListener(ToggleHistory);
            historyButton.onClick.AddListener(ToggleHistory);
        }
        Button closeButton = FindObject(historyRoot != null ? historyRoot.transform : null,
            "CloseHistory")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseHistory);
            closeButton.onClick.AddListener(CloseHistory);
        }
        Transform scrollTransform = historyRoot != null ? historyRoot.transform.Find("HistoryScrollView") : null;
        historyScroll = scrollTransform != null ? scrollTransform.GetComponent<ScrollRect>() : null;
        Transform contentTransform = scrollTransform != null
            ? scrollTransform.Find("Viewport/Content") : null;
        historyContent = contentTransform != null ? contentTransform.GetComponent<RectTransform>() : null;
        historyText = FindText(contentTransform, "HistoryText");
    }

    private void Update()
    {
        PlayerVitals player = FindAnyObjectByType<PlayerVitals>();
        OnionMenuController menu = FindAnyObjectByType<OnionMenuController>();
        DeathScreenController endScreen = FindAnyObjectByType<DeathScreenController>();
        bool menuVisible = menu != null && menu.IsMenuVisible;
        bool pauseVisible = menu != null && menu.IsPauseMenu && menu.IsPauseOpenOrOpening;
        bool gameVisible = player != null && !player.IsDead && !GameSession.RunWon &&
            !NpcDialogueUI.IsDialogueOpen && !pauseVisible && !menuVisible &&
            (endScreen == null || !endScreen.IsShowing);

        if (historyRoot != null && historyRoot.activeSelf && !menuVisible && !pauseVisible)
            historyRoot.SetActive(false);

        if (countersText != null)
        {
            countersText.gameObject.SetActive(gameVisible);
            if (gameVisible)
                countersText.text = $"MOEDAS  {GameSession.RunCoins}\nABATES  {GameSession.RunEnemiesKilled}";
        }

        if (quickbarRoot != null) quickbarRoot.SetActive(gameVisible);
        if (historyButtonRoot != null) historyButtonRoot.SetActive(menuVisible || pauseVisible);
        if (bossRoot != null)
        {
            EnemyHealth boss = null;
            foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
                if (enemy.IsBoss && !enemy.IsDead) { boss = enemy; break; }
            bossRoot.SetActive(gameVisible && boss != null);
            if (boss != null && bossFill != null)
                bossFill.fillAmount = boss.MaxHealth > 0 ? (float)boss.CurrentHealth / boss.MaxHealth : 0f;
        }

        if (toastRoot != null && toastRoot.activeSelf && Time.unscaledTime >= toastUntil)
        {
            if (achievementQueue.Count > 0) DisplayNextAchievement();
            else toastRoot.SetActive(false);
        }
        if (noticeText != null && Time.unscaledTime >= noticeUntil) noticeText.gameObject.SetActive(false);
        if (storyRoot != null && storyRoot.activeSelf)
        {
            if (Time.unscaledTime >= storyUntil) storyRoot.SetActive(false);
            else if (storyCanvasGroup != null)
            {
                float fadeTime = Mathf.Min(Time.unscaledTime - storyStartedAt,
                    storyUntil - Time.unscaledTime);
                storyCanvasGroup.alpha = Mathf.Clamp01(fadeTime / 0.45f);
            }
        }
        RefreshTrophySprite();
    }

    public static void ShowSpawnNotice(string itemName)
    {
        if (instance == null || instance.noticeText == null) return;
        instance.noticeText.text = "Algo apareceu no mapa: " + itemName;
        instance.noticeText.gameObject.SetActive(true);
        instance.noticeUntil = Time.unscaledTime + 3.5f;
    }

    public static void ShowStory(string text, float duration = 10f)
    {
        if (instance == null || instance.storyRoot == null || instance.storyText == null) return;
        instance.storyText.text = text;
        Image panel = instance.storyRoot.GetComponent<Image>();
        if (panel != null)
        {
            panel.enabled = true;
            panel.color = Color.black;
            panel.raycastTarget = false;
        }
        if (instance.storyCanvasGroup == null)
            instance.storyCanvasGroup = instance.storyRoot.AddComponent<CanvasGroup>();
        instance.storyCanvasGroup.alpha = 0f;
        instance.storyCanvasGroup.blocksRaycasts = false;
        instance.storyRoot.transform.SetAsLastSibling();
        instance.storyStartedAt = Time.unscaledTime;
        instance.storyRoot.SetActive(true);
        instance.storyUntil = Time.unscaledTime + Mathf.Max(1f, duration);
    }

    public static void HideStory()
    {
        if (instance != null && instance.storyRoot != null) instance.storyRoot.SetActive(false);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("RuntimeOverlayCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void BuildHud()
    {
        countersText = CreateText("RunCounters", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, -176f), new Vector2(320f, 82f), 25f, TextAlignmentOptions.TopLeft);
        countersText.color = new Color(1f, 0.94f, 0.76f);

        toastRoot = CreatePanel("AchievementToast", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-30f, -40f), new Vector2(510f, 110f));
        Image panelImage = toastRoot.GetComponent<Image>();
        panelImage.color = new Color(0.16f, 0.11f, 0.08f, 0.94f);
        toastIcon = CreateImage("TrophyIcon", toastRoot.transform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(72f, 72f));
        toastIcon.color = new Color(1f, 0.78f, 0.22f);
        toastIcon.enabled = false;
        trophyFallbackText = CreateText("TrophyFallback", toastRoot.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 0f),
            new Vector2(72f, 72f), 46f, TextAlignmentOptions.Center);
        trophyFallbackText.text = "T";
        trophyFallbackText.color = new Color(1f, 0.78f, 0.22f);
        toastText = CreateText("AchievementText", toastRoot.transform, new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(28f, 0f), new Vector2(-40f, 0f), 23f,
            TextAlignmentOptions.MidlineLeft);
        toastText.color = Color.white;
        toastRoot.SetActive(false);

        noticeText = CreateText("SpawnNotice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -126f), new Vector2(720f, 60f), 25f, TextAlignmentOptions.Center);
        noticeText.color = new Color(1f, 0.9f, 0.52f);
        noticeText.gameObject.SetActive(false);

        storyRoot = CreatePanel("StoryOverlay", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        storyRoot.GetComponent<Image>().color = Color.black;
        storyRoot.GetComponent<Image>().raycastTarget = false;
        storyCanvasGroup = storyRoot.AddComponent<CanvasGroup>();
        storyCanvasGroup.blocksRaycasts = false;
        storyText = CreateText("StoryText", storyRoot.transform, Vector2.zero, Vector2.one,
            Vector2.zero, new Vector2(-70f, -50f), 30f, TextAlignmentOptions.Center);
        storyText.color = new Color(1f, 0.94f, 0.79f);
        storyText.text = "A porta se abre...";
        storyText.textWrappingMode = TextWrappingModes.Normal;
        storyRoot.SetActive(false);

        BuildBossHud();
        BuildHistoryPanel();
        BuildQuickbarBackground();
    }

    private void BuildBossHud()
    {
        bossRoot = CreatePanel("BossHealthHud", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 140f), new Vector2(700f, 30f));
        bossRoot.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.07f, 0.9f);
        GameObject fillObject = new GameObject("BossHealthFill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(bossRoot.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f); fillRect.offsetMax = new Vector2(-3f, -3f);
        bossFill = fillObject.GetComponent<Image>();
        bossFill.color = new Color(0.76f, 0.1f, 0.12f);
        bossFill.type = Image.Type.Filled; bossFill.fillMethod = Image.FillMethod.Horizontal;
        bossRoot.SetActive(false);
    }

    private void BuildQuickbarBackground()
    {
        GameObject background = CreatePanel("QuickbarBackground", new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(720f, 82f));
        quickbarRoot = background;
        background.GetComponent<Image>().color = new Color(0.17f, 0.12f, 0.09f, 0.78f);
        HorizontalLayoutGroup layout = background.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = false; layout.childControlHeight = false;
        if (quickbar != null) quickbar.Build(background.transform);
    }

    private void BuildHistoryPanel()
    {
        historyButtonRoot = CreatePanel("HistoryButton", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-140f, -100f), new Vector2(220f, 58f));
        Image buttonImage = historyButtonRoot.GetComponent<Image>();
        buttonImage.color = new Color(0.35f, 0.18f, 0.09f, 0.96f);
        Button button = historyButtonRoot.AddComponent<Button>();
        button.onClick.AddListener(ToggleHistory);
        TMP_Text buttonLabel = CreateText("Label", historyButtonRoot.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, 23f, TextAlignmentOptions.Center);
        buttonLabel.text = "HISTÓRICO";
        buttonLabel.color = Color.white;

        historyRoot = CreatePanel("HistoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1250f, 800f));
        historyRoot.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.07f, 0.97f);
        GameObject scrollObject = new GameObject("HistoryScrollView", typeof(RectTransform),
            typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollObject.transform.SetParent(historyRoot.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(28f, 96f); scrollRect.offsetMax = new Vector2(-28f, -78f);
        scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.035f);
        scrollObject.GetComponent<Mask>().showMaskGraphic = false;
        historyScroll = scrollObject.GetComponent<ScrollRect>();
        historyScroll.horizontal = false;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        historyContent = content.GetComponent<RectTransform>();
        historyContent.anchorMin = new Vector2(0f, 1f); historyContent.anchorMax = new Vector2(1f, 1f);
        historyContent.pivot = new Vector2(0.5f, 1f);
        historyContent.sizeDelta = new Vector2(0f, 0f);
        historyText = CreateText("HistoryText", content.transform, new Vector2(0f, 1f),
            new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(-20f, 0f),
            22f, TextAlignmentOptions.TopLeft);
        historyText.textWrappingMode = TextWrappingModes.Normal;
        historyText.rectTransform.pivot = new Vector2(0.5f, 1f);
        historyScroll.viewport = viewportRect;
        historyScroll.content = historyContent;
        Button close = CreateButton("CloseHistory", historyRoot.transform, "FECHAR",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-105f, 55f),
            new Vector2(170f, 54f), CloseHistory);
        historyRoot.SetActive(false);
    }

    private void ToggleHistory()
    {
        bool show = historyRoot != null && !historyRoot.activeSelf;
        if (historyRoot != null) historyRoot.SetActive(show);
        if (show) RefreshHistory();
    }

    private void CloseHistory()
    {
        if (historyRoot != null) historyRoot.SetActive(false);
    }

    private void RefreshHistory()
    {
        if (historyText == null || historyContent == null || historyScroll == null) return;
        PlayerProfileData data = PlayerProfileService.Data;
        StringBuilder text = new StringBuilder();
        text.AppendLine("<size=31><b>HISTÓRICO DE PARTIDAS</b></size>");
        text.AppendLine("<color=#C9AB8E>Progresso de todas as vagas  ·  até 500 tentativas</color>");
        text.AppendLine();
        text.AppendLine("<color=#F3BE83><size=25><b>PARTIDAS</b></size></color>");
        text.AppendLine($"<b>{data.totalRuns}</b> no total       Fácil <b>{data.easyRuns}</b> / {data.easyWins} vitórias       Médio <b>{data.mediumRuns}</b> / {data.mediumWins} vitórias");
        text.AppendLine($"Difícil <b>{data.hardRuns}</b> / {data.hardWins} vitórias       Insano <b>{data.insaneRuns}</b>");
        text.AppendLine();
        text.AppendLine("<color=#F3BE83><size=25><b>RECORDES</b></size></color>");
        text.AppendLine($"Nível <b>{data.highestLevel}</b>       Sobrevivência <b>{FormatTime(data.longestSurvivalSeconds)}</b>       Defesa <b>{data.highestDefense}</b>");
        text.AppendLine($"Vida <b>{data.highestHealth}</b>       Estamina <b>{data.highestStamina}</b>       Espada <b>{data.highestSwordDamage}</b>       Arco <b>{data.highestBowDamage}</b>");
        text.AppendLine($"Moedas <b>{data.totalCoins}</b>       Máximo numa partida <b>{data.mostCoinsInOneRun}</b>");
        text.AppendLine($"Abates: Fogo <b>{data.totalFireSlimesKilled}</b>       Fantasma <b>{data.totalGhostSlimesKilled}</b>       Chefes <b>{data.totalBossSlimesKilled}</b>");
        text.AppendLine();
        text.AppendLine($"<color=#F3BE83><size=25><b>CONQUISTAS  {data.achievements.Count}</b></size></color>");
        if (data.achievements.Count == 0) text.AppendLine("Nenhuma conquista ainda.");
        else foreach (AchievementRecord achievement in data.achievements)
            text.AppendLine("<color=#D8C59E>[+]</color>  " + achievement.title);
        text.AppendLine();
        text.AppendLine("<color=#F3BE83><size=25><b>ÚLTIMAS TENTATIVAS</b></size></color>");
        if (data.runs.Count == 0) text.AppendLine("Nenhuma partida registrada.");
        for (int i = data.runs.Count - 1; i >= 0; i--)
        {
            RunHistoryRecord run = data.runs[i];
            string resultColor = run.result == "Vitória" ? "#94DFA4" :
                run.result == "Derrota" ? "#F1948A" : "#E7CA92";
            text.AppendLine();
            text.AppendLine($"<size=23><b>VAGA {run.slotIndex}  ·  {run.playerName}  ·  {run.difficulty}</b></size>  <color={resultColor}>{run.result}</color>");
            text.AppendLine($"<color=#C9AB8E>Tempo</color> {FormatTime(run.survivalSeconds)}       <color=#C9AB8E>Nível</color> {run.level}       <color=#C9AB8E>Moedas</color> {run.coins}       <color=#C9AB8E>Abates</color> {run.enemiesKilled}");
            text.AppendLine($"PV {run.currentHealth}/{run.maxHealth}       Estamina {run.currentStamina}/{run.maxStamina}       Defesa {run.defense}       Espada {run.swordDamage}       Arco {run.bowDamage}");
        }
        historyText.text = text.ToString();
        Canvas.ForceUpdateCanvases();
        float height = historyText.preferredHeight + 24f;
        historyText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        historyContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        historyScroll.verticalNormalizedPosition = 1f;
    }

    private void OnAchievementUnlocked(AchievementRecord achievement)
    {
        if (toastRoot == null || achievement == null) return;
        achievementQueue.Enqueue(achievement);
        if (!toastRoot.activeSelf) DisplayNextAchievement();
    }

    private void DisplayNextAchievement()
    {
        if (achievementQueue.Count == 0) return;
        AchievementRecord achievement = achievementQueue.Dequeue();
        toastRoot.SetActive(true);
        toastText.text = "CONQUISTA\n" + achievement.title;
        toastUntil = Time.unscaledTime + 4.5f;
        GameAudio.PlayAchievement();
    }

    private void RefreshTrophySprite()
    {
        if (toastIcon == null || trophyFallbackText == null) return;
        PlayerVitalsHUD hud = FindAnyObjectByType<PlayerVitalsHUD>();
        Sprite current = hud != null ? hud.AchievementTrophySprite : null;
        if (current == null)
        {
            toastIcon.enabled = false;
            trophyFallbackText.gameObject.SetActive(true);
            return;
        }
        if (trophySprite == current) return;
        trophySprite = current;
        toastIcon.sprite = trophySprite;
        toastIcon.color = Color.white;
        toastIcon.enabled = true;
        trophyFallbackText.gameObject.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (historyRoot != null) historyRoot.SetActive(false);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    private static GameObject FindObject(Transform parent, string objectName)
    {
        Transform found = parent != null ? parent.Find(objectName) : null;
        return found != null ? found.gameObject : null;
    }

    private static TMP_Text FindText(Transform parent, string objectName)
    {
        GameObject found = FindObject(parent, objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImage(Transform parent, string objectName)
    {
        GameObject found = FindObject(parent, objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return obj;
    }

    private TMP_Text CreateText(string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment) =>
        CreateText(name, canvas.transform, anchorMin, anchorMax, position, size, fontSize, alignment);

    private TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        TMP_Text text = obj.GetComponent<TMP_Text>();
        if (uiFont != null) text.font = uiFont;
        text.fontSize = fontSize; text.alignment = alignment; text.color = Color.white;
        text.raycastTarget = false; text.text = string.Empty;
        return text;
    }

    private Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return obj.GetComponent<Image>();
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(0.42f, 0.2f, 0.1f, 1f);
        obj.GetComponent<Button>().onClick.AddListener(action);
        TMP_Text buttonText = CreateText(label, obj.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, 21f, TextAlignmentOptions.Center);
        buttonText.color = Color.white;
        return obj.GetComponent<Button>();
    }

    private void OnDestroy()
    {
        PlayerProfileService.AchievementUnlocked -= OnAchievementUnlocked;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
