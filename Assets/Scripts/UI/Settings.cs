using TMPro;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Settings : UIPanel
{
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle menuMusicToggle;

    [SerializeField] private TMP_Text sfxVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;

    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private Slider musicVolumeSlider;

    [SerializeField] private Button returnButton;

    private void OnEnable()
    {
        returnButton.onClick.AddListener(Return); 
        
        UserDataManager udm = UserDataManager.Instance;

        vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        menuMusicToggle.onValueChanged.AddListener(OnMenuMusicToggled);
        sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged); 

        if (udm == null || udm.Data == null)
            return;

        SetupUI(udm);
    }

    private void OnDisable()
    {
        returnButton.onClick.RemoveAllListeners();
        vibrationToggle.onValueChanged.RemoveAllListeners();
        menuMusicToggle.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
    }

    private void SetupUI(UserDataManager udm)
    {
        vibrationToggle.SetIsOnWithoutNotify(udm.Data.IsVibrationEnabled);
        menuMusicToggle.SetIsOnWithoutNotify(udm.Data.IsMenuMusicEnabled);

        sfxVolumeSlider.SetValueWithoutNotify(udm.Data.SfxVolume);
        sfxVolumeText.text = $"{(int)(udm.Data.SfxVolume * 100)}%";

        musicVolumeSlider.SetValueWithoutNotify(udm.Data.MusicVolume);
        musicVolumeText.text = $"{(int)(udm.Data.MusicVolume * 100)}%";
    }

    private void OnVibrationToggled(bool newValue) => UserDataManager.Instance.Data.IsVibrationEnabled = newValue;

    private void OnMenuMusicToggled(bool newValue)
    {
        EventManager.MenuMusicToggled(newValue);
        UserDataManager.Instance.Data.IsMenuMusicEnabled = newValue;
    }

    private void OnSfxVolumeChanged(float volume)
    {
        sfxVolumeText.text = $"{(int)(volume * 100)}%";
        UserDataManager.Instance.Data.SfxVolume = volume;
    }

    private void OnMusicVolumeChanged(float volume)
    {
        EventManager.MusicVolumeChanged(volume);
        musicVolumeText.text = $"{(int)(volume * 100)}%";
        UserDataManager.Instance.Data.MusicVolume = volume;
    }

    private void Return() 
    {
        UserDataManager.Instance.SaveGame();
        UIManager.Instance.GoBack();
    }
}
