using System;
using System.Diagnostics;
using System.IO;
using Codecks.Editor;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CICD.Editor
{
    public enum VersionBump
    {
        Patch,
        Minor,
        Major
    }

    /// <summary>
    /// Unity build pipeline with Odin, Codecks, Itch, version asset,
    /// WebGL browser tagging, and main-scene prioritization.
    /// Now supports choosing patch/minor/major from the Build menu.
    /// </summary>
    public class BuildPipelineTool : SerializedScriptableObject
    {
        // ---------------- GENERAL SETTINGS ----------------
        [Title("General Settings")] [FolderPath(AbsolutePath = false)] [SerializeField]
        private string buildRoot = "Builds";

        [FolderPath(AbsolutePath = false, RequireExistingPath = true)] [LabelText("Scenes Folder")] [SerializeField]
        private string scenesFolder = "Assets/Scenes";

        [LabelText("Main Scene Name"), Tooltip("This scene will be placed first in the build scene list.")]
        [SerializeField]
        private string mainSceneName = "Main";

        [LabelText("Game Name")] public string gameName = "MyGame";

        // ---------------- CODECKS ----------------
        [Space(10), Title("Codecks Integration")] [LabelText("Enable Codecks Integration")]
        public bool useCodecks = true;

        [ShowIf("useCodecks"), LabelText("Access Key Env Var")]
        public string codecksEnvVar = "CODECKS_ACCESS_KEY";

        // ---------------- ITCH.IO ----------------
        [Space(10), Title("Itch.io Deployment")] [LabelText("Upload To Itch.io")]
        public bool uploadToItch = true;

        [ShowIf("uploadToItch"), LabelText("Itch Project (username/project)")]
        public string itchTarget = "yarin/mygame";

        [ShowIf("uploadToItch"), LabelText("Windows Channel")]
        [InfoBox("Itch 'channels' separate build variants (e.g., windows, webgl, demo, devtest).")]
        public string channelWindows = "windows";

        [ShowIf("uploadToItch"), LabelText("Also Build WebGL")]
        public bool buildWebGL = false;

        [ShowIf("@uploadToItch && buildWebGL"), LabelText("WebGL Channel")]
        public string channelWeb = "webgl";

        private const string AssetPath = "Assets/Editor/BuildPipelineSettings.asset";
        private const string VersionAssetPath = "Assets/Settings/VersionData.asset";

        // ---------------- MENU COMMANDS ----------------
        [MenuItem("Build/Create Build Pipeline Settings")]
        public static void CreateSettingsAsset()
        {
            Directory.CreateDirectory("Assets/Editor");
            var asset = CreateInstance<BuildPipelineTool>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Debug.Log($"✅ Created Build Pipeline Settings asset at {AssetPath}");
        }

        // patch build
        [MenuItem("Build/Build and Deploy PATCH")]
        public static void RunPatchBuild()
        {
            LoadOrCreate().Run(VersionBump.Patch);
        }

        // minor build
        [MenuItem("Build/Build and Deploy MINOR")]
        public static void RunMinorBuild()
        {
            LoadOrCreate().Run(VersionBump.Minor);
        }

        // major build
        [MenuItem("Build/Build and Deploy MAJOR")]
        public static void RunMajorBuild()
        {
            LoadOrCreate().Run(VersionBump.Major);
        }

        // still keep Odin button for in-inspector run
        [Button(ButtonSizes.Large)]
        [GUIColor(0.3f, 0.9f, 0.5f)]
        [LabelText("▶ Run Build Pipeline (Patch)")]
        public void RunButton()
        {
            Run(VersionBump.Patch);
        }

        // ---------------- RUN ----------------
        public void Run(VersionBump bumpMode)
        {
            var versionData = LoadOrCreateVersionData();
            string version = versionData.Semantic;
            string gitHash = TryGetGitHash();
            string label = $"{version}-{gitHash}";
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
            string buildPath = Path.Combine(buildRoot, $"{label}-{timestamp}");
            Directory.CreateDirectory(buildPath);

            Log($"=== Starting Build Pipeline: {label} ===");

            if (useCodecks)
            {
                string accessKey = Environment.GetEnvironmentVariable(codecksEnvVar);
                if (!string.IsNullOrEmpty(accessKey))
                {
                    Log("Requesting Codecks token...");
                    CodecksTokenCreator.CreateAndSetNewToken(accessKey, label, success =>
                    {
                        if (!success)
                        {
                            LogError("❌ Failed to create Codecks token. Proceeding without per-build token.");
                            EditorApplication.delayCall += () => PerformBuilds(buildPath, label, versionData, bumpMode);
                            return;
                        }

                        Log("✅ Codecks token created. Proceeding with build...");
                        EditorApplication.delayCall += () => PerformBuilds(buildPath, label, versionData, bumpMode);
                    });
                    return; // async path
                }

                LogWarning($"⚠️ Env var {codecksEnvVar} not found. Skipping Codecks token.");
            }

            PerformBuilds(buildPath, label, versionData, bumpMode);
        }

        // ---------------- BUILD PROCESS ----------------
        private void PerformBuilds(string root, string label, VersionData versionData, VersionBump bumpMode)
        {
            try
            {
                string[] scenes = GetScenes();
                if (scenes.Length == 0)
                    throw new Exception($"No scenes found in {scenesFolder}");

                // WINDOWS
                string exePath = Path.Combine(root, $"{gameName}.exe");
                BuildForTarget(BuildTarget.StandaloneWindows64, scenes, exePath);

                if (uploadToItch && IsButlerAvailable())
                    DeployToItch(Path.GetDirectoryName(exePath), channelWindows, label, false);

                // WEBGL
                if (buildWebGL)
                {
                    string webPath = Path.Combine(root, "WebGLBuild");
                    BuildForTarget(BuildTarget.WebGL, scenes, webPath);

                    if (uploadToItch && IsButlerAvailable())
                        DeployToItch(webPath, channelWeb, label, true);
                }

                // re-fetch version asset in case Unity unloaded the previous reference
                var liveVersion = AssetDatabase.LoadAssetAtPath<VersionData>(VersionAssetPath);
                if (liveVersion == null)
                {
                    LogWarning("⚠️ VersionData asset missing after build reload, recreating.");
                    liveVersion = ScriptableObject.CreateInstance<VersionData>();
                    AssetDatabase.CreateAsset(liveVersion, VersionAssetPath);
                }

                switch (bumpMode)
                {
                    case VersionBump.Major:
                        liveVersion.IncrementMajor();
                        break;
                    case VersionBump.Minor:
                        liveVersion.IncrementMinor();
                        break;
                    default:
                        liveVersion.IncrementPatch();
                        break;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log($"🔢 Version incremented to {liveVersion.Semantic} ({bumpMode})");


                Log($"🔢 Version incremented to {versionData.Semantic} ({bumpMode})");
                Log("🎉 All builds complete!");
            }
            catch (Exception e)
            {
                LogError($"💥 Build step failed: {e}");
            }
        }

        // ---------------- HELPERS ----------------
        private string[] GetScenes()
        {
            if (!Directory.Exists(scenesFolder))
            {
                LogError($"Scenes folder not found: {scenesFolder}");
                return Array.Empty<string>();
            }

            string[] allScenes = Directory.GetFiles(scenesFolder, "*.unity", SearchOption.AllDirectories);
            for (int i = 0; i < allScenes.Length; i++)
                allScenes[i] = allScenes[i].Replace('\\', '/');

            if (allScenes.Length == 0)
                return allScenes;

            // find main scene (by name, case-insensitive)
            string mainScene = Array.Find(
                allScenes,
                s => System.IO.Path.GetFileNameWithoutExtension(s)
                    .Equals(mainSceneName, StringComparison.OrdinalIgnoreCase));

            if (mainScene == null)
            {
                LogError($"❌ Main scene '{mainSceneName}' not found in {scenesFolder}. Using directory order.");
                Log($"Found {allScenes.Length} scene(s) in {scenesFolder}");
                return allScenes;
            }

            // move main to front
            string[] ordered = new string[allScenes.Length];
            ordered[0] = mainScene;
            int idx = 1;
            foreach (var s in allScenes)
            {
                if (s == mainScene) continue;
                ordered[idx++] = s;
            }

            Log($"Found {ordered.Length} scene(s). Main: {System.IO.Path.GetFileName(mainScene)}");
            return ordered;
        }

        private void BuildForTarget(BuildTarget target, string[] scenes, string outputPath)
        {
            Log($"🛠 Building for {target}...");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.CompressWithLz4HC
            };

            var report = UnityEditor.BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception($"Build failed for {target}: {report.summary.result}");

            Log($"✅ {target} build succeeded ({report.summary.totalSize / (1024 * 1024)} MB)");
        }

        private void DeployToItch(string folder, string channel, string version, bool isWebGL)
        {
            Log($"🚀 Uploading to Itch.io ({channel})...");
            var htmlFlag = "";
            RunProcess("butler", $"push \"{folder}\" {itchTarget}:{channel} --userversion {version} {htmlFlag}");
            Log($"✅ Uploaded to Itch.io channel '{channel}'");
        }

        private static VersionData LoadOrCreateVersionData()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VersionData>(VersionAssetPath);
            if (asset == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(VersionAssetPath) ?? string.Empty);
                asset = ScriptableObject.CreateInstance<VersionData>();
                AssetDatabase.CreateAsset(asset, VersionAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"✅ Created new VersionData asset at {VersionAssetPath}");
            }

            return asset;
        }

        private static bool IsButlerAvailable()
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "butler",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                p.WaitForExit(1500);
                return p.ExitCode == 0;
            }
            catch
            {
                Debug.LogWarning("⚠️ Butler CLI not found. Itch.io upload will be skipped.");
                return false;
            }
        }

        private static void RunProcess(string exe, string args)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) Debug.Log(e.Data);
            };
            p.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) Debug.LogError(e.Data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
        }

        private static string TryGetGitHash()
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "rev-parse --short HEAD",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                string output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                return string.IsNullOrEmpty(output) ? "nogit" : output;
            }
            catch
            {
                return "nogit";
            }
        }

        private static void Log(string msg) => Debug.Log($"[BuildPipeline] {msg}");
        private static void LogWarning(string msg) => Debug.LogWarning($"[BuildPipeline] {msg}");
        private static void LogError(string msg) => Debug.LogError($"[BuildPipeline] {msg}");

        private static BuildPipelineTool LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BuildPipelineTool>(AssetPath);
            if (asset == null)
            {
                CreateSettingsAsset();
                asset = AssetDatabase.LoadAssetAtPath<BuildPipelineTool>(AssetPath);
            }

            return asset;
        }
    }
}