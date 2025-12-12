using System;
using System.IO;

using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance { get; private set; }

    public PlayerData Data { get; private set; }

    private string savePath;
    private const string saveFileName = "userdatasave.json";

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
                string json = File.ReadAllText(savePath);

                Data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("User Data Loaded Successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Load Error Save Corrupted?: {e.Message}");
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

            File.WriteAllText(savePath, json);
            Debug.Log($"<color=green>Game Saved:</color> {savePath}");

            // Example: GPGSManager.SaveToCloud(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Save Error: {e.Message}");
        }
    }

    private void CreateNewSave()
    {
        Data = new PlayerData();
        SaveGame();
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted.");
        }

        CreateNewSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
