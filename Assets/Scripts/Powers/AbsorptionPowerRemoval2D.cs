using UnityEngine;

public sealed class AbsorptionPowerRemoval2D : Interactable2D
{
    [SerializeField] private AbsorptionPowerId powerToRemove = AbsorptionPowerId.PoisonApple;
    [SerializeField] private string objectName = "Vaso sanitario";
    [SerializeField] private DialogueManager2D dialogueManager;

    public override void Interact(PlayerInteractor2D interactor)
    {
        PlayerPowerLoadout2D loadout = interactor.GetComponent<PlayerPowerLoadout2D>();
        if (loadout == null)
        {
            Debug.LogWarning("O jogador precisa do componente PlayerPowerLoadout2D.", this);
            return;
        }

        if (loadout.ActiveAbsorptionPower == powerToRemove && loadout.RemoveAbsorptionPower(powerToRemove))
        {
            StartDialogue(interactor, "O poder de veneno foi removido. A maca ja foi consumida.");
        }
        else
        {
            StartDialogue(interactor, "Voce nao tem esse poder de Absorcao para remover.");
        }
    }

    private void StartDialogue(PlayerInteractor2D interactor, string line)
    {
        DialogueManager2D manager = dialogueManager != null ? dialogueManager : interactor.DialogueManager;
        if (manager != null)
        {
            manager.StartDialogue(objectName, new[] { line });
        }
    }
}
