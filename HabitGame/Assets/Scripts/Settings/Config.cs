using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// This is the script where all the players data is stored, saved and read
/// </summary>

[Serializable]
public class Config
{
    [DllImport("__Internal")]
    private static extern void SyncFiles();

    // Link to the JSLib plugin
    [DllImport("__Internal")]
    private static extern void DownloadFile(string filename, string content);

    public bool TutorialFinished;
    public bool SpikeTutorialShown;
    public bool BossTutorialShown;
    public bool ChestTutorialShown;

    public bool FinishedAllBosses;
    public int CurrentSpikeDificulty;
    public ChestSide ChestSide;
    public int Day;
    public List<LevelData> LevelsData;

    private static string SaveFilenName()
    {
        string saveFile = Path.Combine(Application.persistentDataPath, "save.json");
        return saveFile;
    }

    public static void Save(Config config)
    {
        string saveFile = SaveFilenName();
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(saveFile, json);

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif
    }

    public static Config Load()
    {
        string saveFile = SaveFilenName();
        if (!File.Exists(saveFile))
        {
            Save(new Config());
        }
        string jsonText = File.ReadAllText(saveFile);
        Config config = JsonUtility.FromJson<Config>(jsonText);

        Debug.Log($"JSON CONFIG \n {jsonText}");

#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles();
#endif

        return config;
    }

    public static void Download(Config config)
    {
        string saveFile = SaveFilenName();
        string json = JsonUtility.ToJson(config, true);

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFile(saveFile, json);
#else
        // Fallback for Editor: Just print to console or save to Desktop
        Debug.Log("Download triggered. Content: " + json);
#endif
    }
}
