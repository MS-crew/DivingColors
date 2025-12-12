using System.Collections;
using System.Collections.Generic;

using MEC;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Awake() 
    { 
        Instance = Instance.SetSingleton(this);

        themaMusicSource.loop = true;
        themaMusicSource.clip = menuMusic;

        Timing.RunCoroutine(SetVolumes());
    }

    private IEnumerator<float> SetVolumes()
    {
        yield return Timing.WaitUntilTrue(() => UserDataManager.Instance?.Data != null);

        globalAudioSource.volume = UserDataManager.Instance.Data.SfxVolume;
        themaMusicSource.volume = UserDataManager.Instance.Data.MusicVolume;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventManager.OnUIVolumeChanged += SfxVolumeChanged;
        EventManager.OnMusicVolumeChanged += MusicVolumeChanged;
        //EventManager.OnPressedUIElement += PlayClickSound;
        EventManager.OnMenuMusicToggled += MenuMusicToggled;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventManager.OnUIVolumeChanged -= SfxVolumeChanged;
        EventManager.OnMusicVolumeChanged -= MusicVolumeChanged;
        //EventManager.OnPressedUIElement -= PlayClickSound;
        EventManager.OnMenuMusicToggled -= MenuMusicToggled;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameManager.menuSceneName)
            return;

        if (UserDataManager.Instance.Data == null)
        {
            themaMusicSource.Play();
            return;
        }

        if (UserDataManager.Instance.Data.IsMenuMusicEnabled)
            PlayMenuMusic();
    }

    public void PlayMenuMusic()
    {
        if (themaMusicSource != null && UserDataManager.Instance.Data.IsMenuMusicEnabled)
        {
            themaMusicSource.Play();
        }
    }

    public void StopMenuMusic()
    {
        if (themaMusicSource != null)
            themaMusicSource.Stop();
    }

    private void MenuMusicToggled(bool isEnabled)
    {
        if (!isEnabled)
            StopMenuMusic();
        else if (SceneManager.GetActiveScene().name == GameManager.menuSceneName)
            PlayMenuMusic();
    }

    private void SfxVolumeChanged(float newVolume) => globalAudioSource.volume = newVolume;

    private void MusicVolumeChanged(float newVolume) => themaMusicSource.volume = newVolume;

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
