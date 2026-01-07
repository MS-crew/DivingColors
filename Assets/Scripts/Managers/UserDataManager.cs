using System;
using System.IO;

using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance { get; private set; }

    public PlayerData Data { get; private set; }

    private string savePath;
    private const string saveFileName = "userdatasave.json";
    private const string encryptionKey = "ElmaArmutPaça5Salam_Tesla3416";

    [SerializeField] private bool useEncryption = true;

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);

        LoadData();
    }

    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string file = File.ReadAllText(savePath);
                string json = string.Empty;

                if (useEncryption)
                {
                    try
                    {
                        json = EncryptDecrypt(file);
                    }
                    catch
                    {
                        json = file;
                    }
                }
                else
                {
                    json = file;
                }

                Data = JsonUtility.FromJson<PlayerData>(json);
                if (Data == null)
                {
                    Debug.LogWarning("Save Corrupted, Creating new.");
                    CreateNewSave();
                }

                /// test için levele atlama
                //Data.CurrentLevel = 100;
            }
            catch (Exception e)
            {
                Debug.LogError($"Save Corrupted: {e.Message}");
                CreateNewSave();
            }
        }
        else
        {
            Debug.Log("No save file found. Creating new.");
            CreateNewSave();
        }
    }

    public void SaveGame()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            string content = useEncryption ? EncryptDecrypt(json) : json;

            File.WriteAllText(savePath, content);
            Debug.Log($"Game Saved: {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save Error: {e.Message}");
        }
    }

    private string EncryptDecrypt(string data)
    {
        string modifiedData = "";
        for (int i = 0; i < data.Length; i++)
        {
            modifiedData += (char)(data[i] ^ encryptionKey[i % encryptionKey.Length]);
        }

        return modifiedData;
    }

    private void CreateNewSave()
    {
        Data = new PlayerData();
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGame();
    }

    private void OnApplicationQuit() => SaveGame();
}
