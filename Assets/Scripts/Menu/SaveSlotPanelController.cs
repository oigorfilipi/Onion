using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mostra dificuldade, nome e cinco vagas, incluindo confirmação para sobrescrever um save.
/// </summary>
public class SaveSlotPanelController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string startingSceneName = "CasaInterior";
    [SerializeField] private Button[] slotButtons = new Button[SaveSlotService.SlotCount];
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject overwritePanel;
    [SerializeField] private TMP_Text overwriteText;

    private bool creatingNewGame;
    private bool choosingDifficulty;
    private GameSession.Difficulty selectedDifficulty = GameSession.Difficulty.Medium;
    private int pendingOverwriteSlot;

    private void Awake()
    {
        for (int i = 0; i < Mathf.Min(slotButtons.Length, SaveSlotService.SlotCount); i++)
        {
            int slotIndex = i + 1;
            if (slotButtons[i] != null)
            {
                slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));
            }
        }

        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }
    }

    public void OpenNewGame()
    {
        creatingNewGame = true;
        choosingDifficulty = true;
        Open();
    }

    public void OpenContinue()
    {
        creatingNewGame = false;
        choosingDifficulty = false;
        Open();
    }

    public void Close()
    {
        pendingOverwriteSlot = 0;
        choosingDifficulty = false;
        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void ConfirmOverwrite()
    {
        if (pendingOverwriteSlot < 1 || !creatingNewGame)
        {
            return;
        }

        int slotIndex = pendingOverwriteSlot;
        pendingOverwriteSlot = 0;
        BeginNewGame(slotIndex);
    }

    public void CancelOverwrite()
    {
        pendingOverwriteSlot = 0;
        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }
    }

    private void Open()
    {
        gameObject.SetActive(true);
        pendingOverwriteSlot = 0;
        if (overwritePanel != null)
        {
            overwritePanel.SetActive(false);
        }

        if (headingText != null)
        {
            headingText.text = choosingDifficulty ? "ESCOLHA A DIFICULDADE" :
                creatingNewGame ? "NOVO JOGO - ESCOLHA A VAGA" : "CONTINUAR";
        }

        if (playerNameInput != null)
        {
            playerNameInput.gameObject.SetActive(creatingNewGame && !choosingDifficulty);
        }

        SetStatus(string.Empty);
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        string[] difficultyLabels =
        {
            "FÁCIL - 3 MINUTOS",
            "MÉDIO - 10 MINUTOS",
            "DIFÍCIL - 15 MINUTOS",
            "INSANO - TEMPO ILIMITADO"
        };
        for (int i = 0; i < Mathf.Min(slotButtons.Length, SaveSlotService.SlotCount); i++)
        {
            Button button = slotButtons[i];
            if (button == null)
            {
                continue;
            }

            button.gameObject.SetActive(!choosingDifficulty || i < difficultyLabels.Length);
            if (choosingDifficulty)
            {
                button.interactable = true;
                TMP_Text difficultyLabel = button.GetComponentInChildren<TMP_Text>(true);
                if (difficultyLabel != null) difficultyLabel.text = difficultyLabels[i];
                continue;
            }

            int slotIndex = i + 1;
            bool readable = SaveSlotService.TryRead(slotIndex, out SaveData saveData);
            bool exists = SaveSlotService.Exists(slotIndex);
            button.interactable = creatingNewGame || readable;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = readable
                    ? $"VAGA {slotIndex} - {saveData.playerName} (Nível {Mathf.Max(1, saveData.playerLevel)})"
                    : exists ? $"VAGA {slotIndex} - SAVE INDISPONÍVEL" : $"VAGA {slotIndex} - LIVRE";
            }
        }
    }

    // O mesmo conjunto de botões seleciona primeiro a dificuldade e depois uma das cinco vagas.
    private void SelectSlot(int slotIndex)
    {
        if (creatingNewGame && choosingDifficulty)
        {
            switch (slotIndex)
            {
                case 1: selectedDifficulty = GameSession.Difficulty.Easy; break;
                case 2: selectedDifficulty = GameSession.Difficulty.Medium; break;
                case 3: selectedDifficulty = GameSession.Difficulty.Hard; break;
                case 4: selectedDifficulty = GameSession.Difficulty.Insane; break;
                default: return;
            }

            choosingDifficulty = false;
            if (headingText != null) headingText.text = "NOVO JOGO - ESCOLHA A VAGA";
            if (playerNameInput != null) playerNameInput.gameObject.SetActive(true);
            RefreshSlots();
            SetStatus("Digite seu nome e escolha uma das cinco vagas.");
            return;
        }

        if (creatingNewGame)
        {
            if (playerNameInput == null || !SaveSlotService.IsValidName(playerNameInput.text))
            {
                SetStatus("Digite um nome de até 40 caracteres para o jogador.");
                return;
            }

            if (SaveSlotService.Exists(slotIndex))
            {
                if (overwritePanel == null)
                {
                    SetStatus("Configure o painel de confirmação antes de sobrescrever um save.");
                    return;
                }

                pendingOverwriteSlot = slotIndex;
                if (overwriteText != null)
                {
                    overwriteText.text = $"Sobrescrever a vaga {slotIndex}? O progresso anterior será perdido.";
                }

                if (overwritePanel != null)
                {
                    overwritePanel.SetActive(true);
                }

                return;
            }

            BeginNewGame(slotIndex);
            return;
        }

        if (!SaveSlotService.TryRead(slotIndex, out SaveData saveData))
        {
            SetStatus("Essa vaga não contém um save válido.");
            return;
        }

        GameSession.ChooseContinue(slotIndex);
        Time.timeScale = 1f;
        SceneManager.LoadScene(string.IsNullOrWhiteSpace(saveData.sceneName)
            ? gameSceneName : saveData.sceneName);
    }

    // Guarda nome, vaga e dificuldade na sessão antes de carregar a casa inicial.
    private void BeginNewGame(int slotIndex)
    {
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : string.Empty;
        if (!SaveSlotService.IsValidName(playerName))
        {
            SetStatus("Digite um nome válido antes de escolher a vaga.");
            return;
        }

        GameSession.ChooseNewGame(slotIndex, playerName, selectedDifficulty);
        Time.timeScale = 1f;
        SceneManager.LoadScene(startingSceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
