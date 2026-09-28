using UnityEngine;

public sealed class ScrapPickup2D : Interactable2D
{
    [SerializeField] private DialogueManager2D dialogueManager;
    [SerializeField] private PrologueQuest quest;

    public override void Interact(PlayerInteractor2D interactor)
    {
        if (dialogueManager == null || quest == null)
        {
            Debug.LogWarning("A peca de metal precisa de DialogueManager2D e PrologueQuest.", this);
            return;
        }

        if (quest.CurrentPhase != PrologueQuest.ScrapQuestPhase.CollectingScrap)
        {
            dialogueManager.StartDialogue("Pecas de metal", new[] { "O dono do ferro-velho pediu para eu recolher essas pecas." });
            return;
        }

        PlayerMaterialPouch2D materialPouch = interactor.GetComponent<PlayerMaterialPouch2D>();
        if (materialPouch != null && !materialPouch.CanAddMetalScrap())
        {
            dialogueManager.StartDialogue("Inventario cheio", new[] { "Abra o inventario e libere um espaco antes de pegar essa peca." });
            return;
        }

        if (materialPouch != null && !materialPouch.AddMetalScrap()) return;
        if (!quest.RegisterScrap()) return;

        PlayerProgression2D progression = interactor.GetComponent<PlayerProgression2D>();
        if (progression != null)
        {
            progression.AwardExperience(10);
        }

        int collected = quest.ScrapCollected;
        int required = quest.ScrapRequired;

        Destroy(gameObject);
        dialogueManager.StartDialogue("Pecas de metal", new[] { $"Peca recolhida: {collected}/{required}." });
    }
}
