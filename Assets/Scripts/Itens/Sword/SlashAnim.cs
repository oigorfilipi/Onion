using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Remove o efeito visual de corte quando seu sistema de partículas termina.
/// </summary>
public class SlashAnim : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake() {
        ps = GetComponent<ParticleSystem>();
    }

    private void Update() {
        if (ps && !ps.IsAlive()) {
            DestroySelf();
        }
    }

    public void DestroySelf() {
        Destroy(gameObject);
    }
}
