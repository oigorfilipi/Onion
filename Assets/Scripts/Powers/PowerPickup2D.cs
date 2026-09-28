using UnityEngine;

public enum PowerPickupCategory
{
    Contact,
    Absorption
}

public sealed class PowerPickup2D : Interactable2D
{
    [SerializeField] private PowerPickupCategory category = PowerPickupCategory.Contact;
    [SerializeField] private ContactPowerId contactPower = ContactPowerId.SpoonMagnetism;
    [SerializeField] private AbsorptionPowerId absorptionPower = AbsorptionPowerId.PoisonApple;
    [SerializeField] private string itemName = "Colher brilhante";
    [SerializeField, TextArea] private string successDescription;
    [SerializeField] private DialogueManager2D dialogueManager;
    [SerializeField] private PrologueStorySequence2D prologueStory;
    [SerializeField, Min(0)] private int experienceReward = 10;

    public override void Interact(PlayerInteractor2D interactor)
    {
        PlayerPowerLoadout2D loadout = interactor.GetComponent<PlayerPowerLoadout2D>();
        if (loadout == null)
        {
            Debug.LogWarning("O jogador precisa do componente PlayerPowerLoadout2D.", this);
            return;
        }

        bool acquired;
        if (category == PowerPickupCategory.Contact)
        {
            acquired = loadout.AcquireContactPower(contactPower);
        }
        else
        {
            acquired = loadout.AcquireAbsorptionPower(absorptionPower);
        }

        if (!acquired)
        {
            StartDialogue(interactor, new[] { "Esse poder ja faz parte do seu acervo." });
            return;
        }

        PlayerProgression2D progression = interactor.GetComponent<PlayerProgression2D>();
        if (progression != null)
        {
            progression.AwardExperience(experienceReward);
        }

        string powerDescription = string.IsNullOrWhiteSpace(successDescription)
            ? category == PowerPickupCategory.Contact
                ? "O objeto ficou ligado a voce. O poder de Contato esta disponivel enquanto estiver equipado."
                : "A energia do item foi absorvida e o poder entrou no seu espaco de Absorcao."
            : successDescription;
        StartDialogue(interactor, new[] { $"Voce encontrou: {itemName}.", powerDescription }, () =>
        {
            Destroy(gameObject);
            if (category == PowerPickupCategory.Contact && contactPower == ContactPowerId.SpoonMagnetism)
            {
                prologueStory?.OnSpoonCollected();
            }
        });
    }

    private void StartDialogue(PlayerInteractor2D interactor, string[] lines, System.Action finished = null)
    {
        DialogueManager2D manager = dialogueManager != null ? dialogueManager : interactor.DialogueManager;
        if (manager != null)
        {
            manager.StartDialogue(itemName, lines, finished);
        }
        else
        {
            finished?.Invoke();
        }
    }
}
