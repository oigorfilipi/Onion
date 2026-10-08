using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla menu inicial, apresentação de fundos, pausa com Esc e navegação de volta ao menu.
/// </summary>
public class OnionMenuController : MonoBehaviour
{
    [Header("Cenas")]
    [SerializeField] private bool isPauseMenu;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Telas")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject primaryPanel;
    [SerializeField] private SaveSlotPanelController saveSlotsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Fundos")]
    [SerializeField] private Image backgroundFront;
    [SerializeField] private Image backgroundBack;
    [SerializeField] private Sprite[] backgroundSprites;
    [SerializeField, Min(0.1f)] private float secondsPerBackground = 8f;
    [SerializeField, Min(0.1f)] private float transitionSeconds = 1f;
    [SerializeField] private RawImage pauseScreenshot;
    [SerializeField] private Material blurMaterial;

    private bool paused;
    private bool pausing;
    private Coroutine pauseRoutine;
    private Texture2D capturedScreenshot;

    public bool IsPauseOpenOrOpening => isPauseMenu && (paused || pausing);

    private void Awake()
    {
        DisplaySettingsPanel.ApplySavedSettings();

        if (isPauseMenu)
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(false);
            }
        }
        else if (menuRoot != null)
        {
            menuRoot.SetActive(true);
            BackToPrimary();
        }
    }

    private void Start()
    {
        if (!isPauseMenu)
        {
            StartCoroutine(RunBackgroundSlideshow());
        }
    }

    private void Update()
    {
        if (!isPauseMenu || Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame ||
            NpcDialogueUI.IsDialogueOpen)
        {
            return;
        }

        PlayerVitals playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (playerVitals != null && playerVitals.IsDead) return;
        if (FindAnyObjectByType<DeathScreenController>()?.IsShowing == true) return;

        if (paused || pausing)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void OpenNewGame()
    {
        if (isPauseMenu || saveSlotsPanel == null)
        {
            return;
        }

        HidePrimary();
        saveSlotsPanel.OpenNewGame();
    }

    public void OpenContinue()
    {
        if (saveSlotsPanel == null)
        {
            return;
        }

        HidePrimary();
        saveSlotsPanel.OpenContinue();
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        HidePrimary();
        settingsPanel.SetActive(true);
    }

    public void BackToPrimary()
    {
        saveSlotsPanel?.Close();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (primaryPanel != null)
        {
            primaryPanel.SetActive(true);
        }
    }

    // Fecha as interfaces de jogo e salva antes de congelar o tempo e mostrar a tela desfocada.
    public void PauseGame()
    {
        if (!isPauseMenu || paused || pausing || menuRoot == null)
        {
            return;
        }

        PlayerVitals playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (NpcDialogueUI.IsDialogueOpen || (playerVitals != null && playerVitals.IsDead)) return;
        if (FindAnyObjectByType<DeathScreenController>()?.IsShowing == true) return;

        MenuController inventoryMenu = FindAnyObjectByType<MenuController>();
        if (inventoryMenu != null && inventoryMenu.menuCanvas != null)
        {
            inventoryMenu.menuCanvas.SetActive(false);
        }

        FindAnyObjectByType<BackpackController>()?.CloseWindow();
        FindAnyObjectByType<SaveController>()?.SaveGame();
        FindAnyObjectByType<PlayerVitalsHUD>()?.HideImmediately();

        pausing = true;
        pauseRoutine = StartCoroutine(CaptureThenPause());
    }

    public void ResumeGame()
    {
        if (!isPauseMenu)
        {
            return;
        }

        if (pauseRoutine != null)
        {
            StopCoroutine(pauseRoutine);
            pauseRoutine = null;
        }

        paused = false;
        pausing = false;
        Time.timeScale = 1f;
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        ReleaseScreenshot();
    }

    public void ExitToMainMenu()
    {
        if (!isPauseMenu)
        {
            return;
        }

        FindAnyObjectByType<SaveController>()?.SaveGame();
        ResumeGame();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void HidePrimary()
    {
        if (primaryPanel != null)
        {
            primaryPanel.SetActive(false);
        }

        saveSlotsPanel?.Close();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Captura o quadro atual antes de ajustar Time.timeScale para zero, preservando o fundo visual.
    private IEnumerator CaptureThenPause()
    {
        yield return new WaitForEndOfFrame();
        if (pauseScreenshot != null)
        {
            capturedScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
            pauseScreenshot.texture = capturedScreenshot;
            pauseScreenshot.raycastTarget = false;
            if (blurMaterial != null)
            {
                pauseScreenshot.material = blurMaterial;
            }
        }

        Time.timeScale = 0f;
        menuRoot.SetActive(true);
        BackToPrimary();
        paused = true;
        pausing = false;
        pauseRoutine = null;
    }

    private IEnumerator RunBackgroundSlideshow()
    {
        if (backgroundFront == null || backgroundSprites == null || backgroundSprites.Length == 0)
        {
            yield break;
        }

        Image current = backgroundFront;
        Image next = backgroundBack;
        current.raycastTarget = false;
        if (next != null)
        {
            next.raycastTarget = false;
        }
        if (blurMaterial != null)
        {
            current.material = blurMaterial;
            if (next != null)
            {
                next.material = blurMaterial;
            }
        }

        current.sprite = backgroundSprites[0];
        SetAlpha(current, 1f);
        if (next != null)
        {
            SetAlpha(next, 0f);
        }

        int index = 0;
        while (backgroundSprites.Length > 1)
        {
            yield return new WaitForSecondsRealtime(secondsPerBackground);
            index = (index + 1) % backgroundSprites.Length;
            if (next == null || next == current)
            {
                current.sprite = backgroundSprites[index];
                continue;
            }

            next.sprite = backgroundSprites[index];
            next.transform.SetAsLastSibling();
            SetAlpha(next, 0f);
            float elapsed = 0f;
            while (elapsed < transitionSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(next, Mathf.Clamp01(elapsed / transitionSeconds));
                yield return null;
            }

            SetAlpha(next, 1f);
            SetAlpha(current, 0f);
            (current, next) = (next, current);
        }
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void ReleaseScreenshot()
    {
        if (pauseScreenshot != null)
        {
            pauseScreenshot.texture = null;
        }

        if (capturedScreenshot != null)
        {
            Destroy(capturedScreenshot);
            capturedScreenshot = null;
        }
    }

    private void OnDestroy()
    {
        if (isPauseMenu)
        {
            Time.timeScale = 1f;
        }

        ReleaseScreenshot();
    }
}
