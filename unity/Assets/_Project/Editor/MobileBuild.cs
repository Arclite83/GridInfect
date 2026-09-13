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
    // the tile exports under Art/Icon (gen-logo.mjs icon()), assigned to
    // every icon slot the installed platform modules expose (adaptive:
    // background + foreground, and the monochrome layer where the module
    // has a third slot for it).
    // The signing key for Android comes from the environment, never from
    // the repo (see .gitignore): GI_KEYSTORE, GI_KEYSTORE_PASS, GI_KEYALIAS,
    // GI_KEYALIAS_PASS. Without them an APK is debug-signed, which is fine
    // for a local install; an AAB refuses to build, since Play would refuse
    // it anyway. Each AAB build also takes the next versionCode
    // (NextVersionCode). tools/build-android.sh wraps the headless line.
    public static class MobileBuild
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string IconDir = "Assets/_Project/Art/Icon";
        const string IconMain = IconDir + "/icon_1024.png";
        const string IconAdaptiveFg = IconDir + "/icon_adaptive_fg_432.png";
        const string IconAdaptiveBg = IconDir + "/icon_adaptive_bg_432.png";
        const string IconAdaptiveMono = IconDir + "/icon_adaptive_mono_432.png";
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
            if (!Run(BuildTarget.iOS, Path.Combine(OutDir, "ios"))) Fail($"[build] {BuildTarget.iOS} failed");
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
        }

        // Play refuses an upload whose versionCode it has seen on any track,
        // so every AAB build takes a fresh one: GI_VERSION_CODE when set (CI
        // can pass a run number), otherwise the stored code plus one. The
        // bump is written to ProjectSettings.asset before signing is applied,
        // so that hunk is safe to commit and should be: the next build counts
        // on from it. An APK keeps the stored code; a sideload only has to be
        // no lower than what the device already has.
        static int NextVersionCode(bool appBundle)
        {
            string env = Environment.GetEnvironmentVariable("GI_VERSION_CODE");
            if (!string.IsNullOrEmpty(env))
            {
                if (!int.TryParse(env, out int forced) || forced <= 0)
                    throw new BuildFailedException($"[build] GI_VERSION_CODE '{env}' is not a positive integer");
                return forced;
            }
            int current = PlayerSettings.Android.bundleVersionCode;
            return appBundle ? current + 1 : current;
        }

        // The key from the environment, held in memory for the build only:
        // ClearSigning takes it back out before the settings are saved, so
        // the keystore path never lands in ProjectSettings.asset.
        static bool ApplySigning()
        {
            string keystore = Environment.GetEnvironmentVariable("GI_KEYSTORE");
            if (string.IsNullOrEmpty(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("[build] GI_KEYSTORE not set: debug-signed, not uploadable to Play");
                return false;
            }
            if (!File.Exists(keystore))
                throw new BuildFailedException($"[build] GI_KEYSTORE '{keystore}' does not exist");
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("GI_KEYSTORE_PASS") ?? "";
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("GI_KEYALIAS") ?? "";
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("GI_KEYALIAS_PASS") ?? "";
            return true;
        }

        static void ClearSigning()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasName = "";
            PlayerSettings.Android.keyaliasPass = "";
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

        // Every icon kind the platform module exposes gets the tile; adaptive
        // kinds get the background layer under the foreground, plus the
        // monochrome layer when the kind has three slots (themed icons). The
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
            var mono = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAdaptiveMono);
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(target))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(target, kind);
                bool adaptive = kind.ToString().IndexOf("Adaptive", StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (PlatformIcon icon in icons)
                {
                    if (adaptive && icon.maxLayerCount >= 3 && fg != null && bg != null && mono != null)
                    {
                        icon.SetTextures(bg, fg, mono);
                    }
                    else if (adaptive && icon.maxLayerCount >= 2 && fg != null && bg != null)
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
            int code = NextVersionCode(appBundle);
            PlayerSettings.Android.bundleVersionCode = code;
            AssetDatabase.SaveAssets();   // the bump, and nothing about the key
            Debug.Log($"[build] version {PlayerSettings.bundleVersion} ({code})");

            bool signed = ApplySigning();
            if (appBundle && !signed)
                throw new BuildFailedException("[build] an AAB for Play needs GI_KEYSTORE; the APK target is the unsigned one");
            EditorUserBuildSettings.buildAppBundle = appBundle;
            bool ok;
            try
            {
                ok = Run(BuildTarget.Android, Path.Combine(OutDir, appBundle ? "gridinfect.aab" : "gridinfect.apk"));
            }
            finally
            {
                ClearSigning();
                AssetDatabase.SaveAssets();
            }
            if (!ok) Fail($"[build] {BuildTarget.Android} failed");
        }

        static bool Run(BuildTarget target, string location)
        {
            Directory.CreateDirectory(OutDir);
            // GI_DEV=1: a development build. Logcat then carries full C#
            // stack traces with method names and the player's own log at
            // every level, which is what a crash on launch needs. Slower and
            // bigger; never for an upload.
            bool dev = Environment.GetEnvironmentVariable("GI_DEV") == "1";
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = location,
                target = target,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            if (dev) Debug.Log("[build] development build (GI_DEV=1)");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[build] {target} -> {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0} s)");
                return true;
            }
            Debug.LogError($"[build] {target} failed: {summary.result}, {summary.totalErrors} errors");
            return false;
        }

        // A failed build exits non-zero headless (the wrapper and CI key off
        // it) and throws in the editor, where an exit would kill the session.
        static void Fail(string message)
        {
            Debug.LogError(message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw new BuildFailedException(message);
        }
    }
}
