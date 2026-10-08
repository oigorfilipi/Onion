using UnityEngine;
 
/// <summary>Agrupa variações de áudio sob uma identificação usada pelo SoundManager.</summary>
[System.Serializable]
public struct SoundEffect
{
    public string groupID;
    public AudioClip[] clips;
}
 
/// <summary>
/// Organiza grupos de AudioClips por nome para que efeitos sonoros possam variar.
/// </summary>
public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] soundEffects;
 
    public AudioClip GetClipFromName(string name)
    {
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect.groupID == name)
            {
                return soundEffect.clips[Random.Range(0, soundEffect.clips.Length)];
            }
        }
        return null;
    }
}
