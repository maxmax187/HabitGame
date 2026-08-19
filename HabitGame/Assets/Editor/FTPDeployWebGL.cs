// Assets/Editor/FTPDeployWebGL.cs
//
// Automatically uploads WebGL builds to FTP after every successful build.
//
// SETUP:
//  1) Create a file named ".env" in your PROJECT ROOT (same folder that
//     contains "Assets", "ProjectSettings", etc - NOT inside Assets).
//     Copy .env.example next to it and fill in real values:
//
//        FTP_HOST=ftp://yourserver.com
//        FTP_USER=your_username
//        FTP_PASS=your_password
//        FTP_BASE_PATH=/builds/
//        FTP_SLUG_L=e71408556147d1f4a022
//        FTP_SLUG_R=4e868a6f2c029521d9e4
//
//     FTP_BASE_PATH is the "builds" folder that the two condition folders
//     live under, written RELATIVE TO YOUR FTP ACCOUNT'S OWN ROOT - if
//     logging in with an FTP client already drops you inside the site
//     folder (e.g. you see api/, builds/, index.html right away), leave
//     this as "/builds/". Only prefix it with the site's URL folder (e.g.
//     "/f8622112/builds/") if your FTP root is further up the tree than
//     that.
//
//     FTP_SLUG_L / FTP_SLUG_R are the non-guessable per-condition folder
//     names the web app uses (see CONDITION_SLUGS in
//     WebServer/React-TS-Frontend/src/data/conditions.ts). If those ever
//     change, update them here too.
//
//  2) Add ".env" to your .gitignore so credentials never get committed
//     (already done in this project's .gitignore).
//
//  3) In Unity, use Tools > WebGL FTP Deploy > Day > pick Day 1 / Day 2 /
//     Day 3, and Tools > WebGL FTP Deploy > Bias > pick L / R. Both
//     selections are remembered between builds until changed.
//
//  4) Build WebGL as normal (File > Build Settings > Build). When the
//     build finishes, it auto-uploads to:
//        FTP_BASE_PATH + <slug for selected bias> + "/day" + <selected day> + "/"
//     e.g. .../builds/e71408.../day2/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class FTPDeployWebGL : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string DayKey = "FTPDeployWebGL_Day";
    private const string BiasKey = "FTPDeployWebGL_Bias";
    private const string EnableKey = "FTPDeployWebGL_Enabled";

    // ---------------- Menu: choose day ----------------

    [MenuItem("Tools/WebGL FTP Deploy/Day/Day 1", false, 1)]
    private static void SetDay1() => SetDay(1);

    [MenuItem("Tools/WebGL FTP Deploy/Day/Day 2", false, 2)]
    private static void SetDay2() => SetDay(2);

    [MenuItem("Tools/WebGL FTP Deploy/Day/Day 3", false, 3)]
    private static void SetDay3() => SetDay(3);

    [MenuItem("Tools/WebGL FTP Deploy/Day/Day 1", true)]
    private static bool ValidateDay1() { Menu.SetChecked("Tools/WebGL FTP Deploy/Day/Day 1", GetDay() == 1); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Day/Day 2", true)]
    private static bool ValidateDay2() { Menu.SetChecked("Tools/WebGL FTP Deploy/Day/Day 2", GetDay() == 2); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Day/Day 3", true)]
    private static bool ValidateDay3() { Menu.SetChecked("Tools/WebGL FTP Deploy/Day/Day 3", GetDay() == 3); return true; }

    private static void SetDay(int day)
    {
        EditorPrefs.SetInt(DayKey, day);
        Debug.Log($"[FTPDeploy] Day set to {day}. This will be used on the next WebGL build.");
    }

    private static int GetDay() => EditorPrefs.GetInt(DayKey, 1);

    // ---------------- Menu: choose bias/condition ----------------

    [MenuItem("Tools/WebGL FTP Deploy/Bias/L", false, 10)]
    private static void SetBiasL() => SetBias("L");

    [MenuItem("Tools/WebGL FTP Deploy/Bias/R", false, 11)]
    private static void SetBiasR() => SetBias("R");

    [MenuItem("Tools/WebGL FTP Deploy/Bias/L", true)]
    private static bool ValidateBiasL() { Menu.SetChecked("Tools/WebGL FTP Deploy/Bias/L", GetBias() == "L"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Bias/R", true)]
    private static bool ValidateBiasR() { Menu.SetChecked("Tools/WebGL FTP Deploy/Bias/R", GetBias() == "R"); return true; }

    private static void SetBias(string bias)
    {
        EditorPrefs.SetString(BiasKey, bias);
        Debug.Log($"[FTPDeploy] Bias set to '{bias}'. This will be used on the next WebGL build.");
    }

    private static string GetBias() => EditorPrefs.GetString(BiasKey, "L");

    // ---------------- Menu: toggle + info ----------------

    [MenuItem("Tools/WebGL FTP Deploy/Enable Auto-Deploy", false, 20)]
    private static void ToggleEnabled()
    {
        bool current = EditorPrefs.GetBool(EnableKey, true);
        EditorPrefs.SetBool(EnableKey, !current);
        Debug.Log($"[FTPDeploy] Auto-deploy on build is now {(!current ? "ENABLED" : "DISABLED")}.");
    }

    [MenuItem("Tools/WebGL FTP Deploy/Enable Auto-Deploy", true)]
    private static bool ValidateToggleEnabled()
    {
        Menu.SetChecked("Tools/WebGL FTP Deploy/Enable Auto-Deploy", EditorPrefs.GetBool(EnableKey, true));
        return true;
    }

    [MenuItem("Tools/WebGL FTP Deploy/Log Current Target", false, 30)]
    private static void LogCurrentTarget()
    {
        Debug.Log($"[FTPDeploy] Current target: Day {GetDay()}, Bias {GetBias()}.");
    }

    // ---------------- Build hook ----------------

    public void OnPostprocessBuild(BuildReport report)
    {
        if (!EditorPrefs.GetBool(EnableKey, true)) return;
        if (report.summary.platform != BuildTarget.WebGL) return;
        if (report.summary.result != BuildResult.Succeeded) return;

        Dictionary<string, string> env;
        try
        {
            env = LoadEnvFile();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FTPDeploy] Could not read .env file: {e.Message}. Skipping auto-deploy.");
            return;
        }

        string host = RequireEnv(env, "FTP_HOST");
        string user = RequireEnv(env, "FTP_USER");
        string pass = RequireEnv(env, "FTP_PASS");
        string basePath = RequireEnv(env, "FTP_BASE_PATH");
        if (host == null || user == null || pass == null || basePath == null) return;

        string bias = GetBias();
        string slugKey = bias == "R" ? "FTP_SLUG_R" : "FTP_SLUG_L";
        string slug = RequireEnv(env, slugKey);
        if (slug == null) return;

        if (!basePath.EndsWith("/")) basePath += "/";
        int day = GetDay();
        string remoteUrl = host.TrimEnd('/') + basePath + slug + "/day" + day + "/";

        string buildPath = report.summary.outputPath;

        try
        {
            EditorUtility.DisplayProgressBar("FTP Deploy", $"Uploading WebGL build (bias {bias}, day {day})...", 0f);
            Debug.Log($"[FTPDeploy] Starting upload of '{buildPath}' to {remoteUrl}");
            UploadDirectory(buildPath, remoteUrl, user, pass);
            Debug.Log($"[FTPDeploy] Upload complete -> {remoteUrl}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FTPDeploy] Upload failed: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ---------------- .env parsing ----------------

    private static Dictionary<string, string> LoadEnvFile()
    {
        // Project root = one level up from Assets/
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string envPath = Path.Combine(projectRoot, ".env");

        if (!File.Exists(envPath))
            throw new FileNotFoundException($".env file not found at {envPath}");

        var result = new Dictionary<string, string>();
        foreach (string rawLine in File.ReadAllLines(envPath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim();

            // strip optional surrounding quotes
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[value.Length - 1] == value[0])
                value = value.Substring(1, value.Length - 2);

            result[key] = value;
        }
        return result;
    }

    private static string RequireEnv(Dictionary<string, string> env, string key)
    {
        if (env.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            return value;

        Debug.LogError($"[FTPDeploy] Missing '{key}' in .env file. Skipping auto-deploy.");
        return null;
    }

    // ---------------- FTP upload ----------------

    private static void UploadDirectory(string localPath, string remoteUrl, string user, string pass)
    {
        CreateRemoteDirectoryRecursive(remoteUrl, user, pass);

        string[] files = Directory.GetFiles(localPath);
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar("FTP Deploy", $"Uploading {Path.GetFileName(file)}", (float)i / files.Length);
            UploadFile(file, remoteUrl + Path.GetFileName(file), user, pass);
        }

        foreach (string dir in Directory.GetDirectories(localPath))
        {
            string dirName = Path.GetFileName(dir);
            UploadDirectory(dir, remoteUrl + dirName + "/", user, pass);
        }
    }

    // Creates every path segment down to remoteUrl (relative to the FTP
    // account's own root), in case the builds/<slug>/day<N> subfolders
    // don't already exist on the server yet. MakeDirectory on a folder
    // that already exists just fails harmlessly, so this is safe to
    // re-run on every build.
    private static void CreateRemoteDirectoryRecursive(string remoteUrl, string user, string pass)
    {
        Uri uri = new Uri(remoteUrl);
        string[] segments = uri.AbsolutePath.Trim('/').Split('/');

        string current = $"{uri.Scheme}://{uri.Host}" + (uri.IsDefaultPort ? "" : $":{uri.Port}") + "/";
        foreach (string segment in segments)
        {
            if (string.IsNullOrEmpty(segment)) continue;
            current += segment + "/";
            CreateRemoteDirectory(current, user, pass);
        }
    }

    private static void CreateRemoteDirectory(string remoteUrl, string user, string pass)
    {
        try
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteUrl);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(user, pass);
            using (var response = (FtpWebResponse)request.GetResponse()) { }
        }
        catch (WebException)
        {
            // Directory likely already exists - safe to ignore.
        }
    }

    private static void UploadFile(string localFile, string remoteUrl, string user, string pass)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteUrl);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(user, pass);
        request.UseBinary = true;

        byte[] fileContents = File.ReadAllBytes(localFile);
        request.ContentLength = fileContents.Length;

        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(fileContents, 0, fileContents.Length);
        }

        using (var response = (FtpWebResponse)request.GetResponse()) { }
    }
}
