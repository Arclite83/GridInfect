using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GridInfect.EditorTools
{
    // Mobile builds from one place (docs/UNITY_SETUP.md §8). Everything a
    // build needs that is not already in ProjectSettings is applied here
    // from code, so a fresh clone builds the same way in the editor menu
    // and headless:
    //
    //   Unity -batchmode -quit -projectPath unity -executeMethod GridInfect.EditorTools.MobileBuild.Android
    //   Unity -batchmode -quit -projectPath unity -executeMethod GridInfect.EditorTools.MobileBuild.AndroidApk
    //   Unity -batchmode -quit -projectPath unity -executeMethod GridInfect.EditorTools.MobileBuild.IOS
    //
    // The scene: the game boots from RuntimeInitializeOnLoadMethod and needs
    // no scene content, but a player needs one scene in the list, so an
    // empty Main.unity is created and listed if it is missing. The icons:
    // the monogram exports under Art/Icon, assigned to every icon slot the
    // installed platform modules expose (adaptive: background + foreground).
    // The signing key for Android comes from the environment, never from
    // the repo (see .gitignore): GI_KEYSTORE, GI_KEYSTORE_PASS, GI_KEYALIAS,
    // GI_KEYALIAS_PASS. Without them the build is debug-signed, which is
    // fine for a local install and not for Play.
    public static class MobileBuild
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string IconDir = "Assets/_Project/Art/Icon";
        const string IconMain = IconDir + "/icon_1024.png";
        const string IconAdaptiveFg = IconDir + "/icon_adaptive_fg_432.png";
        const string IconAdaptiveBg = IconDir + "/icon_adaptive_bg_432.png";
        const string OutDir = "Builds";   // under unity/, git-ignored

        // R-1203, resolved 2026-09-11: the 2014 package could not be reused
        // (its signing keystore is gone, and a published package name is not
        // reusable for a new listing), so this is the remaster's own id on
        // both stores. Created as a draft on Play; register it on App Store
        // Connect before the iOS follow.
        const string AppId = "com.bloodhoundstudios.gridinfect.app";

        [MenuItem("Grid Infect/Build/Android (AAB for Play)")]
        public static void Android() => BuildAndroid(appBundle: true);

        [MenuItem("Grid Infect/Build/Android (APK for a device)")]
        public static void AndroidApk() => BuildAndroid(appBundle: false);

        [MenuItem("Grid Infect/Build/iOS (Xcode project)")]
        public static void IOS()
        {
            Prepare();
            ApplyIos();
            Run(BuildTarget.iOS, Path.Combine(OutDir, "ios"));
        }

        [MenuItem("Grid Infect/Apply player settings and icons")]
        public static void ApplyOnly()
        {
            Prepare();
            ApplyAndroid();
            ApplyIos();
            AssetDatabase.SaveAssets();
            Debug.Log("[build] player settings and icons applied");
        }

        // ---- settings ----

        static void Prepare()
        {
            EnsureScene();
            PlayerSettings.companyName = "Bloodhound Studios";
            PlayerSettings.productName = "Grid Infect";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);
            ApplyIcons(NamedBuildTarget.Unknown);
        }

        static void ApplyAndroid()
        {
            var target = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // DEPENDENCIES §8
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;         // Play mandate (R-1201)
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
            ApplyIcons(target);

            string keystore = Environment.GetEnvironmentVariable("GI_KEYSTORE");
            if (!string.IsNullOrEmpty(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("GI_KEYSTORE_PASS") ?? "";
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("GI_KEYALIAS") ?? "";
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("GI_KEYALIAS_PASS") ?? "";
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("[build] GI_KEYSTORE not set: debug-signed, not uploadable to Play");
            }
        }

        static void ApplyIos()
        {
            var target = NamedBuildTarget.iOS;
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";                         // DEPENDENCIES §8
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
            ApplyIcons(target);
        }

        // Every icon kind the platform module exposes gets the monogram;
        // adaptive kinds get the background layer under the foreground. The
        // kinds are enumerated rather than named so this compiles without
        // the Android or iOS module installed.
        static void ApplyIcons(NamedBuildTarget target)
        {
            var main = AssetDatabase.LoadAssetAtPath<Texture2D>(IconMain);
            if (main == null)
            {
                Debug.LogWarning($"[build] {IconMain} missing: icons not applied");
                return;
            }
            if (target == NamedBuildTarget.Unknown)
            {
                PlayerSettings.SetIcons(target, new[] { main }, IconKind.Any);
                return;
            }
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAdaptiveFg);
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAdaptiveBg);
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(target))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(target, kind);
                bool adaptive = kind.ToString().IndexOf("Adaptive", StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (PlatformIcon icon in icons)
                {
                    if (adaptive && icon.maxLayerCount >= 2 && fg != null && bg != null)
                    {
                        icon.SetTextures(bg, fg);
                    }
                    else
                    {
                        icon.SetTexture(main, 0);
                    }
                }
                PlayerSettings.SetPlatformIcons(target, kind, icons);
            }
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log($"[build] created {ScenePath}");
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ---- build ----

        static void BuildAndroid(bool appBundle)
        {
            Prepare();
            ApplyAndroid();
            EditorUserBuildSettings.buildAppBundle = appBundle;
            Run(BuildTarget.Android, Path.Combine(OutDir, appBundle ? "gridinfect.aab" : "gridinfect.apk"));
        }

        static void Run(BuildTarget target, string location)
        {
            Directory.CreateDirectory(OutDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[build] {target} -> {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0} s)");
                return;
            }
            string message = $"[build] {target} failed: {summary.result}, {summary.totalErrors} errors";
            Debug.LogError(message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw new BuildFailedException(message);
        }
    }
}
