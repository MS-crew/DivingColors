using System.Collections.Generic;

using UnityEngine;

using static Assets.PublicEnums;

[RequireComponent(typeof(AudioSource), typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private bool playMenuMusicbyDDefault;
    [SerializeField] private AudioClip defaultClickSound, menuMusic;
    [SerializeField] private AudioSource themaMusicSource, globalAudioSource;

    [Header("Combo Audios")]
    [SerializeField] private ComboAudioConfig comboAudioConfig;

    private void Awake() => Instance = Instance.SetSingleton(this);

    public void PlayMenuMusic() => themaMusicSource.Play();

    public void StopMenuMusic() => themaMusicSource.Stop();

    public void StopGlobalSound() => globalAudioSource.Stop();

    public void PlayClickSound() => globalAudioSource.PlayOneShot(defaultClickSound);

    public void PlayGlobalSound(AudioClip clip) => globalAudioSource.PlayOneShot(clip);

    public void PlayComboSound(int x)
    {
        if (comboAudioConfig == null)
            return;

        AudioClip clip = comboAudioConfig.GetRandomClip(x);
        if (clip == null)
            return;

        PlayGlobalSound(clip);
    }
}
