using TMPro;
using UnityEngine;

/// <summary>Mostra o dano aplicado ao jogador ou aos inimigos em texto flutuante no mundo.</summary>
public class WorldDamageNumbers : MonoBehaviour
{
    private float expiresAt;
    private Vector3 drift;

    public static void Show(Vector3 position, int amount, Color color)
    {
        if (amount <= 0) return;
        GameObject floating = new GameObject("Damage Number");
        floating.transform.position = position;
        TextMeshPro text = floating.AddComponent<TextMeshPro>();
        text.text = amount.ToString();
        text.fontSize = 5f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineWidth = 0.18f;
        text.outlineColor = Color.black;
        text.sortingOrder = 500;
        WorldDamageNumbers behaviour = floating.AddComponent<WorldDamageNumbers>();
        behaviour.expiresAt = Time.time + 0.8f;
        behaviour.drift = Vector3.up * 0.55f;
    }

    private void Update()
    {
        transform.position += drift * Time.deltaTime;
        if (Time.time >= expiresAt) Destroy(gameObject);
    }
}
