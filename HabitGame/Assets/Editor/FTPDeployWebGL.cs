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
//        FTP_CERT_SHA256=
//        FTP_USER=your_username
//        FTP_PASS=your_password
//        FTP_BASE_PATH=/builds/
//        FTP_SLUG_ML=26fb1a7345514a4ae729
//        FTP_SLUG_MR=ca0c3a8454c653f57ab9
//        FTP_SLUG_EL=e71408556147d1f4a022
//        FTP_SLUG_ER=4e868a6f2c029521d9e4
//
//     FTP_CERT_SHA256 is optional. Only set it if the server's TLS
//     certificate doesn't chain to a publicly trusted root (e.g. a
//     shared-hosting cert missing its intermediate chain - this shows up
//     as an "Authentication failed" / "not trusted" error even though the
//     certificate itself is legitimate). Get the SHA-256 fingerprint from
//     an FTP client like WinSCP or FileZilla the first time it warns about
//     the certificate (look for a "Show certificate" / details button),
//     paste it in exactly as shown (colons are fine, they're stripped
//     automatically), and the connection will only be trusted if the
//     server presents that *exact* certificate - anything else (including
//     a substituted certificate from an attacker) is still rejected. If
//     the certificate is ever renewed on the server, this value needs
//     updating too. Leave it blank to use normal certificate validation.
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
//     right place before trusting a real deploy - if it landed somewhere
//     unexpected, your server may not follow that convention, and
//     FTP_BASE_PATH needs the fuller path instead (e.g.
//     "/httpdocs/f8622112/builds/").
//
//     There are 4 conditions: ML/MR = moderate dosage (a single game
//     session, no day 2/3), EL/ER = extensive dosage (3 days), each
//     crossed with L/R bias. FTP_SLUG_ML / FTP_SLUG_MR / FTP_SLUG_EL /
//     FTP_SLUG_ER are the non-guessable per-condition folder names the web
//     app uses (see CONDITION_SLUGS in
//     WebServer/React-TS-Frontend/src/data/conditions.ts). If those ever
//     change, update them here too. The Test target doesn't need a slug -
//     it always uploads to FTP_BASE_PATH + "test/".
//
//  2) Add ".env" to your .gitignore so credentials never get committed
//     (already done in this project's .gitignore).
//
//  3) In Unity, use Tools > WebGL FTP Deploy > Target > and pick one of:
//     Moderate L, Moderate R, Extensive L - Day 1/2/3, Extensive R - Day
//     1/2/3, or Test. This selection is remembered between builds until
//     changed.
//
//  4) Auto-deploy is OFF BY DEFAULT (per machine, via Tools > WebGL FTP
//     Deploy > Enable Auto-Deploy) so a routine build never silently
//     overwrites something already on the FTP server. Turn it on
//     deliberately before a build you actually want uploaded, and
//     consider turning it back off afterwards.
//
//  5) Build WebGL as normal (File > Build Settings > Build). When the
//     build finishes, if auto-deploy is on, it uploads to the selected
//     target's folder.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class FTPDeployWebGL : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string TargetKey = "FTPDeployWebGL_Target";
    private const string EnableKey = "FTPDeployWebGL_Enabled";
    private const string DefaultTarget = "ML";
    // Off by default so a build never silently overwrites something on the
    // FTP server unless auto-deploy is deliberately turned on first.
    private const bool DefaultEnabled = false;

    // ---------------- Menu: choose target ----------------
    // Target ids: "ML"/"MR" (moderate, single build each), "EL1".."EL3" /
    // "ER1".."ER3" (extensive, bias + day), or "TEST".

    [MenuItem("Tools/WebGL FTP Deploy/Target/Moderate L", false, 1)]
    private static void SetTargetML() => SetTarget("ML");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Moderate R", false, 2)]
    private static void SetTargetMR() => SetTarget("MR");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive L - Day 1", false, 3)]
    private static void SetTargetEL1() => SetTarget("EL1");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive L - Day 2", false, 4)]
    private static void SetTargetEL2() => SetTarget("EL2");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive L - Day 3", false, 5)]
    private static void SetTargetEL3() => SetTarget("EL3");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive R - Day 1", false, 6)]
    private static void SetTargetER1() => SetTarget("ER1");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive R - Day 2", false, 7)]
    private static void SetTargetER2() => SetTarget("ER2");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive R - Day 3", false, 8)]
    private static void SetTargetER3() => SetTarget("ER3");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Test", false, 9)]
    private static void SetTargetTest() => SetTarget("TEST");

    [MenuItem("Tools/WebGL FTP Deploy/Target/Moderate L", true)]
    private static bool ValidateML() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Moderate L", GetTarget() == "ML"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Moderate R", true)]
    private static bool ValidateMR() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Moderate R", GetTarget() == "MR"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive L - Day 1", true)]
    private static bool ValidateEL1() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Extensive L - Day 1", GetTarget() == "EL1"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive L - Day 2", true)]
    private static bool ValidateEL2() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Extensive L - Day 2", GetTarget() == "EL2"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive L - Day 3", true)]
    private static bool ValidateEL3() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Extensive L - Day 3", GetTarget() == "EL3"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive R - Day 1", true)]
    private static bool ValidateER1() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Extensive R - Day 1", GetTarget() == "ER1"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive R - Day 2", true)]
    private static bool ValidateER2() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Extensive R - Day 2", GetTarget() == "ER2"); return true; }

    [MenuItem("Tools/WebGL FTP Deploy/Target/Extensive R - Day 3", true)]
    private static bool ValidateER3() { Menu.SetChecked("Tools/WebGL FTP Deploy/Target/Extensive R - Day 3", GetTarget() == "ER3"); return true; }

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
            case "ML": return "Moderate L";
            case "MR": return "Moderate R";
            case "EL1": return "Extensive L - Day 1";
            case "EL2": return "Extensive L - Day 2";
            case "EL3": return "Extensive L - Day 3";
            case "ER1": return "Extensive R - Day 1";
            case "ER2": return "Extensive R - Day 2";
            case "ER3": return "Extensive R - Day 3";
            case "TEST": return "Test";
            default: return target;
        }
    }

    // ---------------- Menu: toggle + info ----------------

    [MenuItem("Tools/WebGL FTP Deploy/Enable Auto-Deploy", false, 20)]
    private static void ToggleEnabled()
    {
        bool current = EditorPrefs.GetBool(EnableKey, DefaultEnabled);
        EditorPrefs.SetBool(EnableKey, !current);
        Debug.Log($"[FTPDeploy] Auto-deploy on build is now {(!current ? "ENABLED" : "DISABLED")}.");
    }

    [MenuItem("Tools/WebGL FTP Deploy/Enable Auto-Deploy", true)]
    private static bool ValidateToggleEnabled()
    {
        Menu.SetChecked("Tools/WebGL FTP Deploy/Enable Auto-Deploy", EditorPrefs.GetBool(EnableKey, DefaultEnabled));
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
        // Unconditional - if this line alone doesn't show up in the
        // Console after a build, OnPostprocessBuild isn't being invoked
        // at all (a Unity build-pipeline issue), not a logic issue below.
        Debug.Log(
            $"[FTPDeploy] OnPostprocessBuild called. Platform={report.summary.platform}, " +
            $"Result={report.summary.result}, EnabledPref={EditorPrefs.GetBool(EnableKey, DefaultEnabled)}, " +
            $"OutputPath={report.summary.outputPath}");

        if (!EditorPrefs.GetBool(EnableKey, DefaultEnabled))
        {
            Debug.Log("[FTPDeploy] Auto-deploy is disabled (Tools > WebGL FTP Deploy > Enable Auto-Deploy). Skipping upload.");
            return;
        }

        if (report.summary.platform != BuildTarget.WebGL)
        {
            Debug.Log($"[FTPDeploy] Build platform was {report.summary.platform}, not WebGL. Skipping upload.");
            return;
        }

        // report.summary.result is not reliably populated at this point in
        // the build pipeline (it can read Unknown on a build that actually
        // succeeded) - check for real output on disk instead of trusting it.
        if (!Directory.Exists(report.summary.outputPath) ||
            Directory.GetFileSystemEntries(report.summary.outputPath).Length == 0)
        {
            Debug.LogWarning(
                $"[FTPDeploy] No build output found at '{report.summary.outputPath}' " +
                $"(reported result: {report.summary.result}). Skipping upload.");
            return;
        }

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

        string buildPath = report.summary.outputPath;
        string pinnedFingerprint = GetEnvOrDefault(env, "FTP_CERT_SHA256", null);

        // ServerCertificateValidationCallback is process-wide, not scoped
        // to this connection, so it's saved and restored to avoid making
        // unrelated Editor HTTPS traffic (package manager, etc.) trust
        // only this one pinned certificate too.
        RemoteCertificateValidationCallback previousCallback = ServicePointManager.ServerCertificateValidationCallback;

        try
        {
            // Tolerate FTP_HOST being just an IP/hostname with no "ftp://"
            // in front, since that's exactly how most hosts hand out FTP
            // login details (no scheme shown) and it's an easy thing to
            // paste in literally.
            string hostBase = host.TrimEnd('/');
            if (!hostBase.Contains("://")) hostBase = "ftp://" + hostBase;

            // Only append the port if FTP_HOST doesn't already spell out a
            // non-default one (e.g. someone wrote "ftp://host:2121" directly).
            if (new Uri(hostBase).IsDefaultPort && port != "21")
                hostBase += ":" + port;

            string remoteUrl = hostBase + remotePath;

            if (useSsl)
            {
                EnsureTls12();

                if (!string.IsNullOrEmpty(pinnedFingerprint))
                {
                    string expected = NormalizeFingerprint(pinnedFingerprint);
                    ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        if (errors == SslPolicyErrors.None) return true;

                        string actual = ComputeSha256Fingerprint(certificate);
                        bool trusted = actual == expected;
                        if (!trusted)
                        {
                            Debug.LogWarning(
                                $"[FTPDeploy] Server certificate fingerprint {actual} did not match " +
                                $"FTP_CERT_SHA256 ({expected}). Rejecting connection.");
                        }
                        return trusted;
                    };
                    Debug.Log($"[FTPDeploy] Pinning TLS certificate to fingerprint {expected}.");
                }
            }

            EditorUtility.DisplayProgressBar("FTP Deploy", $"Uploading WebGL build ({GetTargetLabel(target)})...", 0f);
            Debug.Log($"[FTPDeploy] Starting upload of '{buildPath}' to {remoteUrl} (SSL: {useSsl})");
            UploadDirectory(buildPath, remoteUrl, user, pass, useSsl);
            Debug.Log($"[FTPDeploy] Upload complete -> {remoteUrl}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FTPDeploy] Upload failed:\n{DescribeException(e)}");
        }
        finally
        {
            ServicePointManager.ServerCertificateValidationCallback = previousCallback;
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

        if (target == "ML" || target == "MR")
        {
            string bias = target.Substring(1, 1); // "L" or "R"
            string slug = RequireEnv(env, "FTP_SLUG_M" + bias);
            if (slug == null) return null;

            // Moderate is a single session, always stored as day1 on disk.
            return basePath + slug + "/day1/";
        }

        // Extensive targets: "EL1".."EL3" / "ER1".."ER3"
        string extBias = target.Substring(1, 1); // "L" or "R"
        string day = target.Substring(2);         // "1", "2", "3"
        string extSlug = RequireEnv(env, "FTP_SLUG_E" + extBias);
        if (extSlug == null) return null;

        return basePath + extSlug + "/day" + day + "/";
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

    // Unity's Mono/.NET runtime doesn't always negotiate TLS 1.2 by default
    // for FtpWebRequest, which many modern FTPS servers require and will
    // otherwise reject the handshake for (surfacing as a vague
    // "Authentication failed" error with no obviously TLS-related wording).
    private static void EnsureTls12()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
        catch
        {
            // Runtime doesn't define Tls12 - nothing we can do, fall through
            // to whatever the default protocol negotiation does.
        }
    }

    // Walks the full InnerException chain so the real cause isn't hidden
    // behind a generic wrapper message like "Authentication failed, see
    // inner exception."
    private static string DescribeException(Exception e)
    {
        var lines = new List<string>();
        Exception current = e;
        while (current != null)
        {
            lines.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }
        return string.Join("\n  -> caused by: ", lines);
    }

    // Lowercase hex digits only, so "e0:4f:13:..." (as shown by most FTP
    // clients) and a bare hex string both compare equal.
    private static string NormalizeFingerprint(string fingerprint)
    {
        var sb = new StringBuilder();
        foreach (char c in fingerprint)
        {
            if (Uri.IsHexDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static string ComputeSha256Fingerprint(X509Certificate certificate)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(certificate.GetRawCertData());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
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
