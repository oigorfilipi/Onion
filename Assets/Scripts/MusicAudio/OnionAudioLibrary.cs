using UnityEngine;

/// <summary>Referências editáveis aos áudios do pacote Ninja Adventure usados pelo jogo.</summary>
[CreateAssetMenu(fileName = "OnionAudioLibrary", menuName = "Onion/Audio Library")]
public class OnionAudioLibrary : ScriptableObject
{
    [Header("Músicas")]
    public AudioClip menuMusic;
    public AudioClip houseMusic;
    public AudioClip battleMusic;
    public AudioClip storyMusic;

    [Header("Efeitos")]
    public AudioClip pickup;
    public AudioClip coin;
    public AudioClip damage;
    public AudioClip enemyHit;
    public AudioClip sword;
    public AudioClip arrow;
    public AudioClip consume;
    public AudioClip achievement;
    public AudioClip teleport;
    public AudioClip fireShot;
    public AudioClip poisonShot;
    public AudioClip dash;
    public AudioClip levelUp;
    public AudioClip victory;
    public AudioClip defeat;

    [Header("Volumes de coleta")]
    [Range(0f, 1f)] public float pickupVolume = 0.16f;
    [Range(0f, 1f)] public float spawnedPickupVolume = 0.05f;
    [Range(0f, 1f)] public float coinVolume = 0.12f;
    [Range(0f, 1f)] public float spawnedCoinVolume = 0.05f;
}
