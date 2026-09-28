using UnityEngine;

public abstract class Interactable2D : MonoBehaviour
{
    [SerializeField] private string promptText = "Interagir";

    public string PromptText
    {
        get => promptText;
        set => promptText = value;
    }

    public abstract void Interact(PlayerInteractor2D interactor);
}
