using System;

[Serializable]
public class PlayerData
{
    public int TotalScore = 0;
    public int CurrentLevel = 1;

    public float SfxVolume = 1.0f;
    public float MusicVolume = 0.5f;
    public bool  IsMenuMusicEnabled = true;
    public bool  IsVibrationEnabled = true;

    public int Coins = 0;
}
