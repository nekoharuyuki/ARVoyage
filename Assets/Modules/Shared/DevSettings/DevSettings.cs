using UnityEngine;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Niantic.ARVoyage
{
    /// <summary>
    /// ScriptableObject class for storing DevSettings.
    /// This will be automatically created on Unity Editor load if it doesn't exist.
    /// These settings will only be loaded in editor and in development builds.
    /// </summary>
    public class DevSettings : ScriptableObject
    {
        private static string AssetName => "_" + typeof(DevSettings).Name;
        private static string Path => "Assets/Modules/Shared/DevSettings/Resources/" + AssetName + ".asset";

        // The DevSettings instance. This will be null in release builds on device
        private static DevSettings Instance;

        [Tooltip("Should we skip waits in the splash scene?")]
        [SerializeField] private bool skipSplashWait = false;
        public static bool SkipSplashWait => Instance != null && Instance.skipSplashWait;

        [Tooltip("Should we skip the AR warning?")]
        [SerializeField] public bool skipARWarning = false;
        public static bool SkipARWarning => Instance != null && Instance.skipARWarning;

        [Header("SnowballFight")]

        [Tooltip("In Snowball Fight we skip to state main with mock peers in editor?")]
        [SerializeField] public bool skipToSnowballFightMainInEditor = false;
        public static bool SkipToSnowballFightMainInEditor => Instance != null && Instance.skipToSnowballFightMainInEditor;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadSettings()
        {
            Instance = LoadDevSettings();
            if (Instance != null)
            {
                Instance.ProcessSettingsBeforeAwake();
            }
        }
#endif

        // Do any load-time processing of settings before Awake methods are called on components.
        // This method is called in editor and in development builds on device
        public void ProcessSettingsBeforeAwake()
        {
            if (DevSettings.SkipARWarning)
            {
                StateWarning.occurred = true;
            }

            // Process any additional platform-agnostic settings here

#if UNITY_EDITOR
            ProcessEditorOnlySettings();
#else
            ProcessNonEditorSettings();
#endif
        }

        // Only called in Unity Editor
        private void ProcessEditorOnlySettings()
        {
            // Process editor-only settings here if desired and clear this comment.
        }

        // Only called outisde of Unity Editor in development builds
        private void ProcessNonEditorSettings()
        {
            // Ensure this setting is false outside of editor
            skipToSnowballFightMainInEditor = false;
        }

        private static DevSettings LoadDevSettings()
        {
            return Resources.Load<DevSettings>(AssetName);
        }

        // On editor load, create the DevSettings SO if it doesn't exist
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void CreateDevSettings()
        {
            if (!File.Exists(Path))
            {
                DevSettings soAsset = ScriptableObject.CreateInstance<DevSettings>();

                AssetDatabase.CreateAsset(soAsset, Path);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("Created " + soAsset.name);
            }
        }
#endif
    }
}
