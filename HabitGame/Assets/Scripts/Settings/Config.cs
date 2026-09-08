using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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

    // First field on purpose - JsonUtility serializes in declaration order,
    // and this is meant to be the first thing visible when opening the file.
    public string Email;

    public bool TutorialFinished;
    public bool SpikeTutorialShown;
    public bool BossTutorialShown;
    public bool ChestTutorialShown;

    public bool TutorialIntroShown;
    public bool TrainingIntroShown;
    public bool TestIntroShown;

    public bool FinishedAllBosses;
    public int CurrentSpikeDificulty;
    public ChestSide ChestSide;
    public int Day;
    public List<LevelData> LevelsData;

    // public bool TutorialIntroShown;
    // public bool TrainingIntroShown;
    // public bool TestIntroShown;
    public bool MinigameHowToShown;
    public bool HasOpenedBefore; // Home Screen Helper

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

    // Sentinel value used whenever the participant email can't be read
    // from the page URL - Download must never be blocked by this, it
    // should always still produce a file.
    private const string UnknownEmail = "UNKNOWN";

    // Never fails - falls back to UnknownEmail if there's no page URL
    // (e.g. the Editor) or no ?email= on it.
    private static string GetEmailFromUrlOrUnknown()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Dictionary<string, string> queryParams = ParseQueryString(Application.absoluteURL);
        if (queryParams.TryGetValue("email", out string email) && !string.IsNullOrEmpty(email))
        {
            return email;
        }
#endif
        return UnknownEmail;
    }

    public static void Download(Config config)
    {
        string saveFile = SaveFilenName();
        config.Email = GetEmailFromUrlOrUnknown();
        string json = JsonUtility.ToJson(config, true);

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFile(saveFile, json);
#else
        // Fallback for Editor: Just print to console or save to Desktop
        Debug.Log("Download triggered. Content: " + json);
#endif
    }

    // Wraps the config together with the participant email and day so the
    // whole thing serializes via JsonUtility into exactly the JSON shape
    // submit-data.php expects: {"email":"...","day":N,"data":{...}}
    [Serializable]
    private struct SubmitPayload
    {
        public string email;
        public int day;
        public Config data;
    }

    /// <summary>
    /// Sends the config to the server instead of downloading it locally.
    /// Reads the participant email and day straight off the page URL
    /// (set by the web wrapper as ?email=...&day=N on the iframe src), and
    /// posts to <site>/api/submit-data.php - the same origin the game is
    /// served from, so no CORS setup is needed.
    /// Only works in an actual WebGL build (needs a real browser page URL);
    /// in the Editor it just logs what would have been sent.
    /// </summary>
    public static void Submit(Config config, Action<bool, string> onComplete = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string pageUrl = Application.absoluteURL;
        Dictionary<string, string> queryParams = ParseQueryString(pageUrl);

        if (!queryParams.TryGetValue("email", out string email) || string.IsNullOrEmpty(email))
        {
            string msg = "Missing 'email' in page URL: " + pageUrl;
            Debug.LogError("[Config] Submit failed: " + msg);
            onComplete?.Invoke(false, msg);
            return;
        }

        // Embed the same email in the JSON content, not just the outer
        // {email, day, data} wrapper the server reads for routing.
        config.Email = email;

        if (!queryParams.TryGetValue("day", out string dayString) || !int.TryParse(dayString, out int day))
        {
            string msg = "Missing/invalid 'day' in page URL: " + pageUrl;
            Debug.LogError("[Config] Submit failed: " + msg);
            onComplete?.Invoke(false, msg);
            return;
        }

        string apiBase = GetApiBaseUrl(pageUrl);
        if (apiBase == null)
        {
            string msg = "Could not determine API URL from page URL: " + pageUrl;
            Debug.LogError("[Config] Submit failed: " + msg);
            onComplete?.Invoke(false, msg);
            return;
        }

        var payload = new SubmitPayload { email = email, day = day, data = config };
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        var request = new UnityWebRequest(apiBase + "/submit-data.php", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 30;

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += _ =>
        {
            bool ok = request.result == UnityWebRequest.Result.Success;
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (ok)
            {
                Debug.Log("[Config] Submit succeeded: " + responseText);
            }
            else
            {
                Debug.LogError("[Config] Submit failed: " + request.error + " - " + responseText);
            }

            onComplete?.Invoke(ok, ok ? responseText : request.error);
            request.Dispose();
        };
#else
        Debug.Log("Submit triggered (Editor/non-WebGL - not actually sent). Would send: " + JsonUtility.ToJson(config, true));
        onComplete?.Invoke(false, "Submit only works in a real WebGL build (needs a browser page URL).");
#endif
    }

    // The game is always served at .../<site>/builds/<slug>/day<N>/, and
    // the API always lives at .../<site>/api/ - so the API base is
    // whatever comes before "/builds/" in the page URL, plus "/api".
    // This avoids hardcoding this deployment's specific site path.
    private static string GetApiBaseUrl(string pageUrl)
    {
        int buildsIndex = pageUrl.IndexOf("/builds/", StringComparison.Ordinal);
        if (buildsIndex < 0)
        {
            Debug.LogWarning("[Config] Could not find \"/builds/\" in page URL: " + pageUrl);
            return null;
        }
        return pageUrl.Substring(0, buildsIndex) + "/api";
    }

    private static Dictionary<string, string> ParseQueryString(string url)
    {
        var result = new Dictionary<string, string>();
        int queryIndex = url.IndexOf('?');
        if (queryIndex < 0) return result;

        string query = url.Substring(queryIndex + 1);
        foreach (string pair in query.Split('&'))
        {
            if (string.IsNullOrEmpty(pair)) continue;
            int eq = pair.IndexOf('=');
            string key = eq >= 0 ? pair.Substring(0, eq) : pair;
            string value = eq >= 0 ? Uri.UnescapeDataString(pair.Substring(eq + 1)) : "";
            result[Uri.UnescapeDataString(key)] = value;
        }
        return result;
    }
}
