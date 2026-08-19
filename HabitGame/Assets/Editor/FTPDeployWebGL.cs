// Assets/Editor/FTPDeployWebGL.cs
//
// Automatically uploads WebGL builds to FTP after every successful build.
//
// SETUP:
//  1) Create a file named ".env" in your PROJECT ROOT (same folder that
//     contains "Assets", "ProjectSettings", etc - NOT inside Assets).
//     Copy .env.example next to it and fill in real values from the FTP
//     login details you were given:
//
//        FTP_HOST=ftp://xx.xxx.xxx.xxx
//        FTP_PORT=xx
//        FTP_USE_SSL=true
//        FTP_USER=your_username
//        FTP_PASS=your_password
//        FTP_BASE_PATH=/builds/
//        FTP_SLUG_L=e71408556147d1f4a022
//        FTP_SLUG_R=4e868a6f2c029521d9e4
//
//     FTP_HOST is the plain FTP host (an IP address is fine) from your
//     login details - NOT the site's https:// URL, those are different
//     things here. FTP_PORT is whatever port number you were given
//     (defaults to 21 if left out). FTP_USE_SSL should stay "true" if
//     your login details say "TLS/SSL Explicit encryption" (this is what
//     most university/shared hosting requires) - only set it to "false"
//     if you were told to connect with plain unencrypted FTP.
//
//     FTP_BASE_PATH is the "builds" folder that the condition folders (and
//     the test folder) live under. It's resolved relative to whatever
//     directory your FTP account lands in on login (its "Home" - per your
//     login details that's already .../f8622112), so "/builds/" is
//     normally correct even though the site's Home is nested a few
//     folders deep. Do a build with Target > Test first and check with an
//     FTP client (or by visiting the Test URL) that it landed in the
//     right place before trusting a real day/bias deploy - if it landed
//     somewhere unexpected, your server may not follow that convention,
//     and FTP_BASE_PATH needs the fuller path instead (e.g.
//     "/httpdocs/f8622112/builds/").
//
//     FTP_SLUG_L / FTP_SLUG_R are the non-guessable per-condition folder
//     names the web app uses (see CONDITION_SLUGS in
//     WebServer/React-TS-Frontend/src/data/conditions.ts). If those ever
//     change, update them here too. The Test target doesn't need a slug -
//     it always uploads to FTP_BASE_PATH + "test/".
//
//  2) Add ".env" to your .gitignore so credentials never get committed
//     (already done in this project's .gitignore).
//
//  3) In Unity, use Tools > WebGL FTP Deploy > Target > and pick one of:
//     L - Day 1/2/3, R - Day 1/2/3, or Test. This selection is remembered
//     between builds until changed.
//
//  4) Build WebGL as normal (File > Build Settings > Build). When the
//     build finishes, it auto-uploads to the selected target's folder.

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

    private const string TargetKey = "FTPDeployWebGL_Target";
    private const string EnableKey = "FTPDeployWebGL_Enabled";
    private const string DefaultTarget = "L1";

    // ---------------- Menu: choose target ----------------
    // Target ids: "L1".."L3" / "R1".."R3" (bias + day), or "TEST".

    [MenuItem("Tools/WebGL FTP Deploy/Target/L - Day 1", false, 1)]
    private static void SetTargetL1() => SetTarget("L1");

    [MenuItem("Tools/WebGL FTP Deploy/Target/L - Day 2", false, 2)]
    private static void SetTargetL2() => SetTarget("L2");

    [MenuItem("Tools/WebGL FTP Deploy/Target/L - Day 3", false, 3)]
    private static void SetTargetL3() => SetTarget("L3");

    [MenuItem("Tools/WebGL FTP Deploy/Target/R - Day 1", false, 4)]
    private static void SetTargetR1() => SetTarget("R1");

    [MenuItem("Tools/WebGL FTP Deploy/Target/R - Day 2", false, 5)]
    private static void SetTargetR2() => SetTarget("R2");

    [MenuItem("Tools/WebGL FTP Deploy/Target/R - Day 3", false, 6)]
    private static void SetTargetR3() => SetTarget("R3");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Test", false, 7)]
    private static void SetTargetTest() => SetTarget("TEST");

    [MenuItem("Tools/WebGL FTP Deploy/Target/L - Day 1", true)]
    private static bool ValidateL1() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/L - Day 1", GetTarget() == "L1"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/L - Day 2", true)]
    private static bool ValidateL2() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/L - Day 2", GetTarget() == "L2"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/L - Day 3", true)]
    private static bool ValidateL3() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/L - Day 3", GetTarget() == "L3"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/R - Day 1", true)]
    private static bool ValidateR1() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/R - Day 1", GetTarget() == "R1"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/R - Day 2", true)]
    private static bool ValidateR2() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/R - Day 2", GetTarget() == "R2"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/R - Day 3", true)]
    private static bool ValidateR3() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/R - Day 3", GetTarget() == "R3"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Test", true)]
    private static bool ValidateTest() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Test", GetTarget() == "TEST"); return true; }

    private static void SetTarget(string target)
    {
        EditorPrefs.SetString(TargetKey, target);
        Debug.Log($"[FTPDeploy] Target set to '{GetTargetLabel(target)}'. This will be used on the next WebGL build.");
    }

    private static string GetTarget() => EditorPrefs.GetString(TargetKey, DefaultTarget);

    private static string GetTargetLabel(string target)
    {
        switch (target)
        {
            case "L1": return "L - Day 1";
            case "L2": return "L - Day 2";
            case "L3": return "L - Day 3";
            case "R1": return "R - Day 1";
            case "R2": return "R - Day 2";
            case "R3": return "R - Day 3";
            case "TEST": return "Test";
            default: return target;
        }
    }

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
        Debug.Log($"[FTPDeploy] Current target: {GetTargetLabel(GetTarget())}.");
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
        if (!basePath.EndsWith("/")) basePath += "/";

        string port = GetEnvOrDefault(env, "FTP_PORT", "21");
        bool useSsl = ParseBool(GetEnvOrDefault(env, "FTP_USE_SSL", "true"), true);

        string target = GetTarget();
        string remotePath = ComputeRemotePath(env, basePath, target);
        if (remotePath == null) return;

        string hostBase = host.TrimEnd('/');
        // Only append the port if FTP_HOST doesn't already spell out a
        // non-default one (e.g. someone wrote "ftp://host:2121" directly).
        if (new Uri(hostBase).IsDefaultPort && port != "21")
            hostBase += ":" + port;

        string remoteUrl = hostBase + remotePath;
        string buildPath = report.summary.outputPath;

        try
        {
            EditorUtility.DisplayProgressBar("FTP Deploy", $"Uploading WebGL build ({GetTargetLabel(target)})...", 0f);
            Debug.Log($"[FTPDeploy] Starting upload of '{buildPath}' to {remoteUrl} (SSL: {useSsl})");
            UploadDirectory(buildPath, remoteUrl, user, pass, useSsl);
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

    // basePath already ends with "/". Returns a path (also ending in "/")
    // relative to the FTP account root, or null if a required .env value
    // (e.g. a condition slug) is missing.
    private static string ComputeRemotePath(Dictionary<string, string> env, string basePath, string target)
    {
        if (target == "TEST")
        {
            return basePath + "test/";
        }

        string bias = target.Substring(0, 1); // "L" or "R"
        string day = target.Substring(1);      // "1", "2", "3"
        string slugKey = bias == "R" ? "FTP_SLUG_R" : "FTP_SLUG_L";
        string slug = RequireEnv(env, slugKey);
        if (slug == null) return null;

        return basePath + slug + "/day" + day + "/";
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

    private static string GetEnvOrDefault(Dictionary<string, string> env, string key, string defaultValue)
    {
        return env.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    private static bool ParseBool(string value, bool defaultValue)
    {
        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }

    // ---------------- FTP upload ----------------

    private static void UploadDirectory(string localPath, string remoteUrl, string user, string pass, bool useSsl)
    {
        CreateRemoteDirectoryRecursive(remoteUrl, user, pass, useSsl);

        string[] files = Directory.GetFiles(localPath);
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar("FTP Deploy", $"Uploading {Path.GetFileName(file)}", (float)i / files.Length);
            UploadFile(file, remoteUrl + Path.GetFileName(file), user, pass, useSsl);
        }

        foreach (string dir in Directory.GetDirectories(localPath))
        {
            string dirName = Path.GetFileName(dir);
            UploadDirectory(dir, remoteUrl + dirName + "/", user, pass, useSsl);
        }
    }

    // Creates every path segment down to remoteUrl (relative to the FTP
    // account's own root), in case the target subfolder doesn't already
    // exist on the server yet. MakeDirectory on a folder that already
    // exists just fails harmlessly, so this is safe to re-run every build.
    private static void CreateRemoteDirectoryRecursive(string remoteUrl, string user, string pass, bool useSsl)
    {
        Uri uri = new Uri(remoteUrl);
        string[] segments = uri.AbsolutePath.Trim('/').Split('/');

        string current = $"{uri.Scheme}://{uri.Host}" + (uri.IsDefaultPort ? "" : $":{uri.Port}") + "/";
        foreach (string segment in segments)
        {
            if (string.IsNullOrEmpty(segment)) continue;
            current += segment + "/";
            CreateRemoteDirectory(current, user, pass, useSsl);
        }
    }

    private static void CreateRemoteDirectory(string remoteUrl, string user, string pass, bool useSsl)
    {
        try
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteUrl);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(user, pass);
            request.EnableSsl = useSsl;
            using (var response = (FtpWebResponse)request.GetResponse()) { }
        }
        catch (WebException)
        {
            // Directory likely already exists - safe to ignore.
        }
    }

    private static void UploadFile(string localFile, string remoteUrl, string user, string pass, bool useSsl)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteUrl);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(user, pass);
        request.EnableSsl = useSsl;
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
