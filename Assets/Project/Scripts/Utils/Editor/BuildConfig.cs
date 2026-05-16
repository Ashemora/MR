#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Project.Scripts.Utils.Attributes;
using Project.Scripts.Utils.Buttons;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project.Scripts.Utils.Editor
{
    [CreateAssetMenu(fileName = "BuildConfig", menuName = "Configs/Build Config")]
    public class BuildConfig : ScriptableObject
    {
        [Header("Version")]
        [Tooltip("Версия (SemVer, например 0.2.1). При билде записывается в PlayerSettings.bundleVersion. Кнопка Get Versions подтянет текущее значение из Player Settings.")]
        [SerializeField] private string _version = "0.0.1";

        [Tooltip("Build number (Android bundleVersionCode / iOS buildNumber). Должен расти при каждом релизе. При билде записывается в Player Settings.")]
        [SerializeField] private int _buildNumber = 1;

        [Button(nameof(GetVersions), drawField: false)]
        [SerializeField] private bool _getVersionsButton;

        [Button(nameof(SetVersions), drawField: false)]
        [SerializeField] private bool _setVersionsButton;


        [Header("Defines")]
        [Tooltip("Define-символ для внутренней сборки (читы, debug-оверлеи, тестовые тулзы). Активен только в dev-билде.")]
        [SerializeField] private string _devDefine = "DEV";

        [Tooltip("Define-символ для production-сборки (боевые ключи аналитики, реальный IAP, прод-эндпоинты). Активен только в prod-билде.")]
        [SerializeField] private string _productionDefine = "PRODUCTION";

        [Header("Output")]
        [Tooltip("Папка для dev-билдов (относительно корня проекта).")]
        [SerializeField] private string _devOutputDir = "Builds/Dev";

        [Tooltip("Папка для production-билдов (относительно корня проекта).")]
        [SerializeField] private string _prodOutputDir = "Builds/Prod";

        [Header("Options")]
        [Tooltip("Включать ли флаг Development Build для dev-сборки (профайлер, deep-логи, script debugging).")]
        [SerializeField] private bool _devUseDevelopmentFlag = true;

        [Tooltip("Запрашивать подтверждение перед production-билдом, чтобы не собрать его случайно.")]
        [SerializeField] private bool _confirmProduction = true;

        [Tooltip("Открывать папку с билдом в проводнике после успешной сборки.")]
        [SerializeField] private bool _revealOutputAfterBuild = true;


        [Header("Build")]
        [Button(nameof(BuildDev), drawField: false)]
        [SerializeField] private bool _buildDevButton;

        [Button(nameof(BuildProduction), drawField: false)]
        [SerializeField] private bool _buildProductionButton;


        [Header("Last Build")]
        [Tooltip("Результат последней сборки (Succeeded / Failed / Cancelled).")]
        [ReadOnly] [SerializeField] private string _lastBuildResult;

        [Tooltip("Тип последней сборки (Dev / Production).")]
        [ReadOnly] [SerializeField] private string _lastBuildKind;

        [Tooltip("Платформа последней сборки.")]
        [ReadOnly] [SerializeField] private string _lastBuildTarget;

        [Tooltip("Время последней сборки.")]
        [ReadOnly] [SerializeField] private string _lastBuildTime;

        [Tooltip("Длительность последней сборки.")]
        [ReadOnly] [SerializeField] private string _lastBuildDuration;

        [Tooltip("Размер последнего билда.")]
        [ReadOnly] [SerializeField] private string _lastBuildSize;

        [Tooltip("Путь к последнему собранному файлу.")]
        [ReadOnly] [SerializeField] private string _lastBuildPath;

        [Button(nameof(OpenLastBuildFolder), drawField: false)]
        [SerializeField] private bool _openLastBuildFolderButton;


        private void OnEnable()
        {
            PullVersionsFromPlayerSettings();
        }


        private void GetVersions()
        {
            PullVersionsFromPlayerSettings();
            Debug.Log($"Got versions from Player Settings: {_version} (build {_buildNumber})");
        }

        private void SetVersions()
        {
            ApplyVersionsToPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"Set versions in Player Settings: {_version} (build {_buildNumber})");
        }

        private void BuildDev()
        {
            RunBuild("Dev", "dev", _devDefine, removeDefine: _productionDefine, _devUseDevelopmentFlag, _devOutputDir, androidAppBundle: false);
        }

        private void BuildProduction()
        {
            if (_confirmProduction)
            {
                var ok = EditorUtility.DisplayDialog(
                    "Build Production",
                    "Собрать production-билд?\nЧиты будут отключены, флаг Development снят.",
                    "Собрать",
                    "Отмена");
                if (false == ok)
                    return;
            }

            RunBuild("Production", "prod", _productionDefine, removeDefine: _devDefine, development: false, outputDir: _prodOutputDir, androidAppBundle: true);
        }

        private void OpenLastBuildFolder()
        {
            if (string.IsNullOrEmpty(_lastBuildPath))
            {
                Debug.LogWarning("No last build path recorded.");

                return;
            }

            EditorUtility.RevealInFinder(_lastBuildPath);
        }

        private void RunBuild(string kind, string channel, string addDefine, string removeDefine, bool development, string outputDir, bool androidAppBundle)
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(activeTarget);
            var nbt = NamedBuildTarget.FromBuildTargetGroup(targetGroup);

            var originalDefines = PlayerSettings.GetScriptingDefineSymbols(nbt);
            var originalAppBundle = EditorUserBuildSettings.buildAppBundle;

            ApplyVersionsToPlayerSettings();

            var newDefines = BuildDefines(originalDefines, addDefine, removeDefine);
            PlayerSettings.SetScriptingDefineSymbols(nbt, newDefines);
            if (activeTarget == BuildTarget.Android)
                EditorUserBuildSettings.buildAppBundle = androidAppBundle;
            AssetDatabase.SaveAssets();

            try
            {
                Directory.CreateDirectory(outputDir);

                var outputPath = Path.Combine(outputDir, GetExecutableName(activeTarget, channel, androidAppBundle))
                    .Replace('\\', '/');
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes
                        .Where(s => s.enabled)
                        .Select(s => s.path)
                        .ToArray(),
                    locationPathName = outputPath,
                    target = activeTarget,
                    targetGroup = targetGroup,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                };

                var report = BuildPipeline.BuildPlayer(options);
                RecordLastBuild(kind, activeTarget, report, outputPath);

                Debug.Log($"Build finished: {report.summary.result} ({FormatBytes(report.summary.totalSize)}) at {outputPath}");

                if (_revealOutputAfterBuild && report.summary.result == BuildResult.Succeeded)
                    EditorUtility.RevealInFinder(outputPath);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(nbt, originalDefines);
                if (activeTarget == BuildTarget.Android)
                    EditorUserBuildSettings.buildAppBundle = originalAppBundle;
                AssetDatabase.SaveAssets();
            }
        }

        private void RecordLastBuild(string kind, BuildTarget target, BuildReport report, string outputPath)
        {
            _lastBuildKind = kind;
            _lastBuildTarget = target.ToString();
            _lastBuildResult = report.summary.result.ToString();
            _lastBuildTime = report.summary.buildEndedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            _lastBuildDuration = FormatDuration(report.summary.totalTime);
            _lastBuildSize = FormatBytes(report.summary.totalSize);
            _lastBuildPath = outputPath;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        private void PullVersionsFromPlayerSettings()
        {
            var version = PlayerSettings.bundleVersion;
            var buildNumber = PlayerSettings.Android.bundleVersionCode;

            var changed = false;
            if (_version != version)
            {
                _version = version;
                changed = true;
            }
            if (_buildNumber != buildNumber)
            {
                _buildNumber = buildNumber;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }

        private void ApplyVersionsToPlayerSettings()
        {
            PlayerSettings.bundleVersion = _version;
            PlayerSettings.Android.bundleVersionCode = _buildNumber;
            PlayerSettings.iOS.buildNumber = _buildNumber.ToString();
        }

        private static string BuildDefines(string original, string addDefine, string removeDefine)
        {
            var list = (original ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => false == string.IsNullOrEmpty(d))
                .ToList();

            if (false == string.IsNullOrEmpty(removeDefine))
                list.RemoveAll(d => d == removeDefine);

            if (false == string.IsNullOrEmpty(addDefine) && false == list.Contains(addDefine))
                list.Add(addDefine);

            return string.Join(";", list);
        }

        private static string GetExecutableName(BuildTarget target, string channel, bool androidAppBundle)
        {
            var productName = PlayerSettings.productName;
            var version = PlayerSettings.bundleVersion;
            var buildNumber = GetBuildNumber(target);
            var baseName = $"{productName}_{version}_{channel}.{buildNumber}";
            switch (target)
            {
                case BuildTarget.Android:
                    return androidAppBundle ? $"{baseName}.aab" : $"{baseName}.apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return $"{baseName}.exe";
                case BuildTarget.iOS:
                    return baseName;
                case BuildTarget.StandaloneOSX:
                    return $"{baseName}.app";
                case BuildTarget.StandaloneLinux64:
                    return baseName;
                default:
                    return baseName;
            }
        }

        private static string GetBuildNumber(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return PlayerSettings.Android.bundleVersionCode.ToString();
                case BuildTarget.iOS:
                    return PlayerSettings.iOS.buildNumber;
                default:
                    return PlayerSettings.Android.bundleVersionCode.ToString();
            }
        }

        private static string FormatBytes(ulong bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024UL * 1024UL)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024UL * 1024UL * 1024UL)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";

            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1.0)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
            if (duration.TotalMinutes >= 1.0)
                return $"{duration.Minutes}m {duration.Seconds}s";

            return $"{duration.Seconds}.{duration.Milliseconds:D3}s";
        }
    }
}
#endif