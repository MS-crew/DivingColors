using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private bool playMenuMusicbyDDefault;
    [SerializeField] private AudioClip defaultClickSound, menuMusic;
    [SerializeField] private AudioSource themaMusicSource, globalAudioSource;

    private void Awake() => Instance = Instance.SetSingleton(this);

    public void PlayMenuMusic() => themaMusicSource.Play();

    public void StopMenuMusic() => themaMusicSource.Stop();

    public void StopGlobalSound() => globalAudioSource.Stop();

    public void PlayClickSound() => globalAudioSource.PlayOneShot(defaultClickSound);

    public void PlayGlobalSound(AudioClip clip) => globalAudioSource.PlayOneShot(clip);

    
}
