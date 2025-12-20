using System.Collections.Generic;

using MEC;

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

    [SerializeField] private AudioClip attemptGainedClip;

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
       
        EventManager.OnComboUsed += PlayComboSound;
        EventManager.OnAttemptGained += PlayAttempGainedSound;


        EventManager.OnUIVolumeChanged += SfxVolumeChanged;
        EventManager.OnMenuMusicToggled += MenuMusicToggled;
        EventManager.OnMusicVolumeChanged += MusicVolumeChanged;
        //EventManager.OnPressedUIElement += PlayClickSound;
       
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        EventManager.OnComboUsed -= PlayComboSound;
        EventManager.OnAttemptGained -= PlayAttempGainedSound;


        EventManager.OnUIVolumeChanged -= SfxVolumeChanged; 
        EventManager.OnMenuMusicToggled -= MenuMusicToggled;
        EventManager.OnMusicVolumeChanged -= MusicVolumeChanged;
        //EventManager.OnPressedUIElement -= PlayClickSound;
        
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
        if (themaMusicSource == null)
            return;

        themaMusicSource.Play();
        themaMusicSource.loop = true;
    }

    public void StopMenuMusic()
    {
        if (themaMusicSource != null)
            themaMusicSource.Stop();
    }

    private void MusicVolumeChanged(float newVolume)
    {
        if (themaMusicSource != null)
            themaMusicSource.volume = newVolume;
    }

    private void MenuMusicToggled(bool isEnabled)
    {
        if (!isEnabled)
            StopMenuMusic();
        else if (SceneManager.GetActiveScene().name == GameManager.menuSceneName)
            PlayMenuMusic();
    }

    public void StopGlobalSound() 
    {
        if (globalAudioSource != null)
            globalAudioSource.Stop(); 
    }

    public void PlayClickSound() 
    {
        if (globalAudioSource != null)
            globalAudioSource.PlayOneShot(defaultClickSound);
    }

    public void PlayGlobalSound(AudioClip clip, bool forcePlay = true)
    {
        if (globalAudioSource == null)
            return;

        if (forcePlay || !globalAudioSource.isPlaying)
            globalAudioSource.PlayOneShot(clip); 
    }

    private void SfxVolumeChanged(float newVolume) 
    {
        if (globalAudioSource != null)
            globalAudioSource.volume = newVolume;
    }
  
    private void PlayComboSound(string _, ComboTier _2, int x)
    {
        if (comboAudioConfig == null)
            return;

        AudioClip clip = comboAudioConfig.GetRandomClip(x);
        if (clip == null)
            return;

        PlayGlobalSound(clip);
    }

    private void PlayAttempGainedSound(int _) 
    {
        if (comboAudioConfig == null)
            return;

        AudioClip clip = attemptGainedClip;
        if (clip == null)
            return;

        PlayGlobalSound(clip);
    }
}
