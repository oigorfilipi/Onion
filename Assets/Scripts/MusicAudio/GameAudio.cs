using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reproduz os clipes do pacote Ninja Adventure e mantém a música ao trocar de cena.</summary>
public class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private AudioSource effectsSource;
    private AudioSource ambienceSource;
    private AudioClip menuAmbience;
    private AudioClip houseAmbience;
    private AudioClip gameAmbience;
    private AudioClip storyAmbience;
    private AudioClip pickup;
    private AudioClip coin;
    private AudioClip damage;
    private AudioClip hit;
    private AudioClip sword;
    private AudioClip arrow;
    private AudioClip consume;
    private AudioClip achievement;
    private AudioClip teleport;
    private AudioClip fireShot;
    private AudioClip poisonShot;
    private AudioClip dash;
    private AudioClip levelUp;
    private AudioClip victory;
    private AudioClip defeat;
    private float pickupVolume = 0.16f;
    private float spawnedPickupVolume = 0.05f;
    private float coinVolume = 0.12f;
    private float spawnedCoinVolume = 0.05f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        GameObject audioObject = new GameObject("Onion Audio");
        instance = audioObject.AddComponent<GameAudio>();
        DontDestroyOnLoad(audioObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        ambienceSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.playOnAwake = false;
        ambienceSource.loop = true;
        ambienceSource.volume = 0.08f;
        ambienceSource.spatialBlend = 0f;
        OnionAudioLibrary library = Resources.Load<OnionAudioLibrary>("OnionAudioLibrary");
        menuAmbience = library != null && library.menuMusic != null
            ? library.menuMusic : MakeAmbient("Menu Ambient", true);
        houseAmbience = library != null && library.houseMusic != null
            ? library.houseMusic : MakeAmbient("House Ambient", true);
        gameAmbience = library != null && library.battleMusic != null
            ? library.battleMusic : MakeAmbient("World Ambient", false);
        storyAmbience = library != null && library.storyMusic != null
            ? library.storyMusic : houseAmbience;
        pickup = library != null && library.pickup != null
            ? library.pickup : Tone("Pickup", 880f, 0.09f, 0.18f, 1.45f);
        coin = library != null && library.coin != null ? library.coin : pickup;
        damage = library != null && library.damage != null
            ? library.damage : Tone("Damage", 115f, 0.22f, 0.3f, 0.45f);
        hit = library != null && library.enemyHit != null
            ? library.enemyHit : Tone("Enemy Hit", 185f, 0.12f, 0.27f, 0.55f);
        sword = library != null && library.sword != null
            ? library.sword : Tone("Sword Swing", 430f, 0.18f, 0.22f, 0.5f);
        arrow = library != null && library.arrow != null
            ? library.arrow : Tone("Arrow Shot", 680f, 0.16f, 0.2f, 0.6f);
        consume = library != null && library.consume != null
            ? library.consume : Tone("Consume", 610f, 0.15f, 0.17f, 1.1f);
        achievement = library != null && library.achievement != null
            ? library.achievement : Tone("Achievement", 990f, 0.24f, 0.2f, 1.4f);
        teleport = library != null && library.teleport != null
            ? library.teleport : Tone("Teleport", 270f, 0.35f, 0.23f, 1.8f);
        fireShot = library != null ? library.fireShot : null;
        poisonShot = library != null ? library.poisonShot : null;
        dash = library != null ? library.dash : null;
        levelUp = library != null ? library.levelUp : null;
        victory = library != null ? library.victory : null;
        defeat = library != null ? library.defeat : null;
        if (library != null)
        {
            pickupVolume = library.pickupVolume;
            spawnedPickupVolume = library.spawnedPickupVolume;
            coinVolume = library.coinVolume;
            spawnedCoinVolume = library.spawnedCoinVolume;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
        SelectAmbience(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SelectAmbience(scene);

    private void SelectAmbience(Scene scene)
    {
        AudioClip chosen = scene.name == "MainMenu" ? menuAmbience :
            scene.name == "CasaInterior" ? houseAmbience : gameAmbience;
        if (ambienceSource.clip == chosen && ambienceSource.isPlaying) return;
        ambienceSource.clip = chosen;
        ambienceSource.Play();
    }

    private void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip, volume);
    }

    public static void PlayPickup(bool spawned = false) =>
        instance?.Play(instance.pickup, spawned ? instance.spawnedPickupVolume : instance.pickupVolume);
    public static void PlayCoin(bool spawned = false) =>
        instance?.Play(instance.coin, spawned ? instance.spawnedCoinVolume : instance.coinVolume);
    public static void PlayDamage() => instance?.Play(instance.damage, 0.08f);
    public static void PlayHit() => instance?.Play(instance.hit, 0.08f);
    public static void PlaySword() => instance?.Play(instance.sword, 0.08f);
    public static void PlayArrow() => instance?.Play(instance.arrow, 0.1f);
    public static void PlayConsume() => instance?.Play(instance.consume, 0.12f);
    public static void PlayAchievement() => instance?.Play(instance.achievement, 0.12f);
    public static void PlayTeleport() => instance?.Play(instance.teleport, 0.1f);
    public static void PlaySlimeShot(bool fire) => instance?.Play(
        fire ? instance.fireShot : instance.poisonShot, 0.08f);
    public static void PlayDash() => instance?.Play(instance.dash, 0.08f);
    public static void PlayLevelUp() => instance?.Play(instance.levelUp, 0.12f);
    public static void PlayVictory() => instance?.Play(instance.victory, 0.14f);
    public static void PlayDefeat() => instance?.Play(instance.defeat, 0.12f);

    public static void PlayStoryIntro()
    {
        if (instance == null || instance.ambienceSource == null || instance.storyAmbience == null) return;
        instance.ambienceSource.clip = instance.storyAmbience;
        instance.ambienceSource.Play();
    }

    private static AudioClip Tone(string name, float frequency, float duration, float volume, float falloff)
    {
        const int rate = 22050;
        int count = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            float envelope = Mathf.Exp(-time * falloff * 8f) * Mathf.Min(1f, time * 35f);
            float bend = frequency * (1f - Mathf.Clamp01(time / duration) * 0.22f);
            float fundamental = Mathf.Sin(2f * Mathf.PI * bend * time);
            float overtone = Mathf.Sin(2f * Mathf.PI * bend * 2.03f * time) * 0.22f;
            data[i] = (fundamental + overtone) * envelope * volume;
        }
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeAmbient(string name, bool menu)
    {
        const int rate = 22050;
        const int seconds = 8;
        int count = rate * seconds;
        float[] data = new float[count];
        float baseTone = menu ? 92f : 68f;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)rate;
            float slowWave = Mathf.Sin(2f * Mathf.PI * 0.13f * time) * 0.5f + 0.5f;
            float toneA = Mathf.Sin(2f * Mathf.PI * baseTone * time);
            float toneB = Mathf.Sin(2f * Mathf.PI * baseTone * 1.498f * time + 0.4f);
            float toneC = Mathf.Sin(2f * Mathf.PI * baseTone * 2.01f * time + 1.2f);
            float noise = (Mathf.PerlinNoise(time * 1.8f, menu ? 2.6f : 6.1f) - 0.5f) * 0.25f;
            data[i] = (toneA * 0.22f + toneB * 0.12f + toneC * 0.06f + noise) * (0.45f + slowWave * 0.35f);
        }
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
