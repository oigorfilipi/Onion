using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Troca temporariamente o material do sprite para mostrar que um personagem recebeu dano.
/// </summary>
public class Flash : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;
    private float flashUntil;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMat = spriteRenderer.material;
    }

    public float GetRestoreMatTime() {
        return restoreDefaultMatTime;
    }

    public IEnumerator FlashRoutine() {
        flashUntil = Time.time + restoreDefaultMatTime;
        spriteRenderer.material = whiteFlashMat;
        while (Time.time < flashUntil) yield return null;
        spriteRenderer.material = defaultMat;
    }
}
