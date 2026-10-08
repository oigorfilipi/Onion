using UnityEngine;

/// <summary>Identifica o NPC para escolher o presente e registrar se já foi entregue.</summary>
public enum NpcRole { Velho, LoucoDoArco, Mercadora, Madrasta }

/// <summary>
/// Armazena papel, nome, falas e distância de interação de cada NPC.
/// </summary>
public class NpcDialogue : MonoBehaviour
{
    [SerializeField] private NpcRole role;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string[] dialogueLines;
    [SerializeField, Min(0.5f)] private float interactionDistance = 1.5f;

    public NpcRole Role => role;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? role.ToString() : displayName;
    public string[] DialogueLines => dialogueLines;
    public float InteractionDistance => interactionDistance;
}
