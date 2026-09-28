using System.Collections;
using UnityEngine;

public sealed class PrologueStorySequence2D : Interactable2D
{
    private enum PrologueStage
    {
        Inactive,
        ReachExit,
        DefeatRat,
        LeaveTown,
        Completed
    }

    [SerializeField] private Transform player;
    [SerializeField] private PrologueQuest quest;
    [SerializeField] private EnemyHealth2D fireEnemy;
    [SerializeField] private EnemyFireShooter2D fireShooter;
    [SerializeField] private EnemyHealth2D mutantRat;
    [SerializeField] private PlayerPowerLoadout2D playerPowers;
    [SerializeField] private DialogueManager2D dialogueManager;
    [SerializeField] private SpriteRenderer figureRenderer;
    [SerializeField] private Collider2D interactionCollider;
    [SerializeField, Min(0f)] private float fallDuration = 0.65f;
    [SerializeField, Min(0f)] private float rescuePause = 0.8f;
    [SerializeField, Min(0.5f)] private float ratRevealDistance = 2.5f;

    private PlayerMovement2D playerMovement;
    private PlayerCombat2D playerCombat;
    private PlayerInteractor2D playerInteractor;
    private SpriteRenderer playerRenderer;
    private bool movementWasEnabled;
    private bool combatWasEnabled;
    private bool interactorWasEnabled;
    private bool spoonCollected;
    private bool rescueStarted;
    private bool rescueCompleted;
    private bool missionReturned;
    private bool returnConversationStarted;
    private bool returnConversationCompleted;
    private bool ratEncounterTriggered;
    private bool ratEncounterUnlocked;
    private PrologueStage prologueStage;
    private Quaternion playerOriginalRotation;
    private Color playerOriginalColor;

    public bool HasPrologueObjective => prologueStage != PrologueStage.Inactive;
    public bool CanLeaveTown => prologueStage == PrologueStage.LeaveTown;
    public string PrologueObjectiveText
    {
        get
        {
            switch (prologueStage)
            {
                case PrologueStage.ReachExit:
                    return "Siga para a saida leste da cidade.";
                case PrologueStage.DefeatRat:
                    return "Derrote o rato mutante que bloqueia a saida.";
                case PrologueStage.LeaveTown:
                    return "Atravesse a saida e siga pela estrada.";
                case PrologueStage.Completed:
                    return "Prologo concluido. A cidade proxima fica adiante pela estrada.";
                default:
                    return string.Empty;
            }
        }
    }

    private void Awake()
    {
        CachePlayerComponents();
        PromptText = "Falar com o desconhecido";
        SetFigureVisible(false);
        SetInteractionEnabled(false);
    }

    private void OnEnable()
    {
        SubscribeToMutantRat();
    }

    private void OnDisable()
    {
        UnsubscribeFromMutantRat();
    }

    private void Update()
    {
        if (!ratEncounterUnlocked || ratEncounterTriggered || mutantRat == null || player == null)
        {
            return;
        }

        Vector2 offset = (Vector2)mutantRat.transform.position - (Vector2)player.position;
        if (offset.sqrMagnitude > ratRevealDistance * ratRevealDistance)
        {
            return;
        }

        ratEncounterTriggered = true;
        prologueStage = PrologueStage.DefeatRat;
        mutantRat.gameObject.SetActive(true);
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(
                "Protagonista",
                new[] { "Um rato mutante bloqueia a saida! Ele esta cuspindo bolas de queijo." });
        }
    }

    public void ConfigureMutantRat(EnemyHealth2D rat)
    {
        UnsubscribeFromMutantRat();
        mutantRat = rat;
        if (mutantRat != null)
        {
            SubscribeToMutantRat();
            mutantRat.gameObject.SetActive(false);
        }
    }

    public void OnSpoonCollected()
    {
        if (spoonCollected || fireEnemy == null)
        {
            return;
        }

        CachePlayerComponents();
        spoonCollected = true;
        fireEnemy.HealthChanged += OnFireEnemyHealthChanged;
        fireEnemy.SetDamageLocked(false);
        fireEnemy.gameObject.SetActive(true);

        if (fireShooter != null)
        {
            fireShooter.enabled = true;
        }
    }

    public void OnScrapyardMissionReturned(Transform returningPlayer)
    {
        missionReturned = true;
        if (returningPlayer != null)
        {
            player = returningPlayer;
        }

        TryStartReturnConversation();
    }

    public override void Interact(PlayerInteractor2D interactor)
    {
        if (!returnConversationCompleted || dialogueManager == null)
        {
            return;
        }

        dialogueManager.StartDialogue(
            "Desconhecido",
            new[] { "Encontre-me na estrada. A cidade mais proxima e o melhor lugar para comecarmos a investigar." });
    }

    private void OnFireEnemyHealthChanged(EnemyHealth2D enemy, int currentHealth, int maxHealth)
    {
        int halfHealth = Mathf.CeilToInt(maxHealth * 0.5f);
        if (rescueStarted || currentHealth > halfHealth)
        {
            return;
        }

        rescueStarted = true;
        enemy.HealthChanged -= OnFireEnemyHealthChanged;
        enemy.SetDamageLocked(true);
        if (fireShooter != null)
        {
            fireShooter.enabled = false;
        }

        StartCoroutine(PlayRescueSequence(enemy));
    }

    private IEnumerator PlayRescueSequence(EnemyHealth2D enemy)
    {
        CachePlayerComponents();
        LockPlayerControls();

        if (player != null)
        {
            playerOriginalRotation = player.rotation;
            player.rotation = Quaternion.Euler(0f, 0f, 75f);
        }

        if (playerRenderer != null)
        {
            playerOriginalColor = playerRenderer.color;
            playerRenderer.color = new Color(0.58f, 0.58f, 0.58f, playerOriginalColor.a);
        }

        yield return new WaitForSeconds(fallDuration);

        if (enemy != null)
        {
            transform.position = enemy.transform.position + new Vector3(0.8f, 0.8f, 0f);
        }
        SetFigureVisible(true);

        yield return new WaitForSeconds(rescuePause);

        if (enemy != null)
        {
            enemy.DefeatImmediately();
        }

        if (playerPowers != null)
        {
            playerPowers.RemoveContactPower(ContactPowerId.SpoonMagnetism);
        }

        yield return new WaitForSeconds(0.35f);

        SetFigureVisible(false);
        if (player != null)
        {
            player.rotation = playerOriginalRotation;
        }

        if (playerRenderer != null)
        {
            playerRenderer.color = playerOriginalColor;
        }

        UnlockPlayerControls();
        rescueCompleted = true;
        TryStartReturnConversation();
    }

    private void TryStartReturnConversation()
    {
        if (!missionReturned || !rescueCompleted || returnConversationStarted)
        {
            return;
        }

        returnConversationStarted = true;
        if (player != null)
        {
            transform.position = player.position + new Vector3(1.1f, 0.45f, 0f);
        }

        SetFigureVisible(true);
        SetInteractionEnabled(false);

        string[] lines =
        {
            "A colher foi alterada pela radiacao, mas seu corpo conseguiu suportar o contato.",
            "Ela canaliza magnetismo. Poderes de Contato funcionam enquanto o objeto correspondente estiver equipado.",
            "Eu a tirei de voce para interromper a luta. Fique com ela e venha comigo ate a cidade proxima."
        };

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue("Desconhecido", lines, FinishReturnConversation);
        }
        else
        {
            FinishReturnConversation();
        }
    }

    private void FinishReturnConversation()
    {
        if (playerPowers != null)
        {
            playerPowers.AcquireContactPower(ContactPowerId.SpoonMagnetism);
        }

        returnConversationCompleted = true;
        ratEncounterUnlocked = true;
        prologueStage = PrologueStage.ReachExit;
        SetInteractionEnabled(true);
    }

    public bool MarkTownExited()
    {
        if (!CanLeaveTown)
        {
            return false;
        }

        prologueStage = PrologueStage.Completed;
        return true;
    }

    private void SubscribeToMutantRat()
    {
        if (mutantRat == null)
        {
            return;
        }

        mutantRat.Died -= OnMutantRatDefeated;
        mutantRat.Died += OnMutantRatDefeated;
    }

    private void UnsubscribeFromMutantRat()
    {
        if (mutantRat != null)
        {
            mutantRat.Died -= OnMutantRatDefeated;
        }
    }

    private void OnMutantRatDefeated(EnemyHealth2D defeatedRat)
    {
        if (defeatedRat == mutantRat && prologueStage == PrologueStage.DefeatRat)
        {
            prologueStage = PrologueStage.LeaveTown;
        }
    }

    private void CachePlayerComponents()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement2D>();
            playerCombat = player.GetComponent<PlayerCombat2D>();
            playerInteractor = player.GetComponent<PlayerInteractor2D>();
            playerRenderer = player.GetComponent<SpriteRenderer>();
            if (playerPowers == null)
            {
                playerPowers = player.GetComponent<PlayerPowerLoadout2D>();
            }
        }
    }

    private void LockPlayerControls()
    {
        if (playerMovement != null)
        {
            movementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        if (playerCombat != null)
        {
            combatWasEnabled = playerCombat.enabled;
            playerCombat.enabled = false;
        }

        if (playerInteractor != null)
        {
            interactorWasEnabled = playerInteractor.enabled;
            playerInteractor.enabled = false;
        }
    }

    private void UnlockPlayerControls()
    {
        if (playerMovement != null) playerMovement.enabled = movementWasEnabled;
        if (playerCombat != null) playerCombat.enabled = combatWasEnabled;
        if (playerInteractor != null) playerInteractor.enabled = interactorWasEnabled;
    }

    private void SetFigureVisible(bool visible)
    {
        if (figureRenderer != null)
        {
            figureRenderer.enabled = visible;
        }
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (interactionCollider != null)
        {
            interactionCollider.enabled = enabled;
        }
    }
}
