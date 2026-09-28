using UnityEngine;

public sealed class StepmotherNPC2D : Interactable2D
{
    [SerializeField] private string speakerName = "Madrasta";
    [SerializeField] private DialogueManager2D dialogueManager;

    public override void Interact(PlayerInteractor2D interactor)
    {
        DialogueManager2D dialogue = dialogueManager != null ? dialogueManager : interactor.DialogueManager;
        if (dialogue == null)
        {
            Debug.LogWarning("A madrasta precisa de um DialogueManager2D.", this);
            return;
        }

        dialogue.StartDialogue(
            speakerName,
            new[]
            {
                "Bom dia. Finalmente acordou.",
                "O dono do ferro-velho estava procurando alguem para ajudar. Se quiser, passe por la.",
                "Tome cuidado com o que encontrar pelo caminho. Volte quando puder."
            });
    }
}
