using UnityEngine;

public sealed class ScrapyardQuestNPC2D : Interactable2D
{
    [SerializeField] private string npcName = "Dono do ferro-velho";
    [SerializeField] private DialogueManager2D dialogueManager;
    [SerializeField] private PrologueQuest quest;
    [SerializeField] private PrologueStorySequence2D prologueStory;
    [SerializeField, Min(0)] private int questExperienceReward = 50;
    [SerializeField, Min(0)] private int swordExperienceReward = 10;
    private bool swordRewardClaimed;

    public override void Interact(PlayerInteractor2D interactor)
    {
        if (dialogueManager == null || quest == null)
        {
            Debug.LogWarning("O NPC do ferro-velho precisa de DialogueManager2D e PrologueQuest.", this);
            return;
        }

        switch (quest.CurrentPhase)
        {
            case PrologueQuest.ScrapQuestPhase.NotStarted:
                dialogueManager.StartDialogue(
                    npcName,
                    new[]
                    {
                        "Bom dia. Umas pecas de metal apareceram perto da praca durante a noite.",
                        "Traga duas para mim. Quero ver se ainda podem ser aproveitadas."
                    },
                    quest.BeginScrapQuest);
                break;

            case PrologueQuest.ScrapQuestPhase.CollectingScrap:
                int remaining = quest.ScrapRequired - quest.ScrapCollected;
                dialogueManager.StartDialogue(npcName, new[] { $"Ainda faltam {remaining} pecas. Procure perto da praca." });
                break;

            case PrologueQuest.ScrapQuestPhase.ReturnToScrapyard:
                dialogueManager.StartDialogue(
                    npcName,
                    new[]
                    {
                        "Trouxe as pecas. Obrigado, jovem.",
                        "Parece que vou ter de descobrir de onde elas vieram."
                    },
                    () =>
                    {
                        bool completingNow = quest.CurrentPhase == PrologueQuest.ScrapQuestPhase.ReturnToScrapyard;
                        quest.CompleteScrapQuest();
                        if (completingNow && quest.CurrentPhase == PrologueQuest.ScrapQuestPhase.Completed)
                        {
                            PlayerProgression2D progression = interactor.GetComponent<PlayerProgression2D>();
                            if (progression != null) progression.AwardExperience(questExperienceReward);
                            prologueStory?.OnScrapyardMissionReturned(interactor.transform);
                        }
                    });
                break;

            default:
                GiveSwordReward(interactor);
                break;
        }
    }

    private void GiveSwordReward(PlayerInteractor2D interactor)
    {
        if (swordRewardClaimed)
        {
            dialogueManager.StartDialogue(npcName, new[] { "A espada que te dei deve ajudar na estrada." });
            return;
        }

        PlayerInventory2D inventory = interactor.GetComponent<PlayerInventory2D>();
        if (inventory == null || !inventory.CanAddItem(InventoryItemId.Sword))
        {
            dialogueManager.StartDialogue(npcName, new[] { "Obrigado por me ajudar. Libere um espaco na mochila e volte; quero te dar uma coisa." });
            return;
        }

        dialogueManager.StartDialogue(
            npcName,
            new[]
            {
                "Voce me ajudou com aquelas pecas. Fique com esta espada de metal comum.",
                "Nao tem radiacao; pode ser util contra as criaturas la fora."
            },
            () =>
            {
                if (inventory.TryAddItem(InventoryItemId.Sword))
                {
                    swordRewardClaimed = true;
                    PlayerProgression2D progression = interactor.GetComponent<PlayerProgression2D>();
                    if (progression != null) progression.AwardExperience(swordExperienceReward);
                }
            });
    }
}
