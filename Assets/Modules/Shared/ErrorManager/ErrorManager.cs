using UnityEngine;
using UnityEngine.UI;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Display an fullscreen error GUI, or error banner overlay, 
    /// with a given error message. GUI has button to restart the scene.
    ///
    /// Also can display a fullscreen warning GUI used when the app
    /// has returned from being backgrounded, with restart and cancel buttons.
    ///
    /// Also can display a fullscreen overlay with stack trace text from an app exception.
    /// </summary>
    public class ErrorManager : MonoBehaviour, ISceneDependency
    {
        [SerializeField] private GameObject fullscreenBackdrop;

        // Fullscreen overlay - error GUI with dynamic text and restart-scene button
        [SerializeField] private GameObject errorGUI;
        [SerializeField] private TMPro.TMP_Text errorText;
        [SerializeField] private Button errorRestartButton;

        // Fullscreen overlay - warning GUI, restart-scene button and cancel-warning button
        [SerializeField] private GameObject backgroundedGUI;
        [SerializeField] private Button backgroundedRestartButton;
        [SerializeField] private Button backgroundedCancelButton;

        // Small error banner
        [SerializeField] private GameObject errorBanner;
        [SerializeField] private TMPro.TMP_Text errorBannerText;

        // For development builds:
        // Fullscreen overlay - displays stack trace from an app exception
        [SerializeField] private TMPro.TMP_Text scriptExceptionText;

        private IARSession arSession;
        private float arSessionInitializedTime;

        private bool displayScriptExceptions = true;

        private bool autoHideErrorBanner;
        private float timeErrorBannerDisplayed = 0f;
        private const float showErrorBannerDuration = 5f;

        private Fader fader;


        private void Awake()
        {
            errorGUI.gameObject.SetActive(false);
            errorBanner.gameObject.SetActive(false);
            backgroundedGUI.gameObject.SetActive(false);
            fullscreenBackdrop.gameObject.SetActive(false);

#if !DEVELOPMENT_BUILD
            // Disable script exception display outside of dev builds
            displayScriptExceptions = false;
#endif

            if (displayScriptExceptions)
            {
                Application.logMessageReceived += OnLogMessageReceived_ShowScriptException;
            }

            fader = SceneLookup.Get<Fader>();

            ARSessionFactory.SessionInitialized += OnSessionInitialized;
        }

        private void OnSessionInitialized(AnyARSessionInitializedArgs args)
        {
            // Cache the ARSession for this scene
            this.arSession = args.Session;
            arSessionInitializedTime = Time.time;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived_ShowScriptException;
            ARSessionFactory.SessionInitialized -= OnSessionInitialized;
        }

        private void Update()
        {
            // hide error banner after a given duration
            if (errorBanner.gameObject.activeSelf &&
                autoHideErrorBanner &&
                Time.time - timeErrorBannerDisplayed >= showErrorBannerDuration)
            {
                HideErrorBanner();
            }
        }

        public void DisplayErrorGUI(string msg)
        {
            Debug.Log("DisplayErrorGUI: " + msg);
            fullscreenBackdrop.gameObject.SetActive(true);
            errorText.text = msg;
            StartCoroutine(DemoUtil.FadeInGUI(errorGUI, fader));
        }

        private void HideErrorGUI()
        {
            errorGUI.gameObject.SetActive(false);
            fullscreenBackdrop.gameObject.SetActive(false);
        }


        public void DisplayErrorBanner(string msg, bool autoHideErrorBanner = true)
        {
            Debug.Log("DisplayErrorBanner: " + msg);
            errorBannerText.text = msg;
            errorBanner.gameObject.SetActive(true);
            timeErrorBannerDisplayed = Time.time;
            this.autoHideErrorBanner = autoHideErrorBanner;
        }

        public void HideErrorBanner()
        {
            errorBanner.gameObject.SetActive(false);
            autoHideErrorBanner = false;
        }


        public void DisplayBackgroundedGUI()
        {
            Debug.Log("DisplayBackgroundedGUI");
            fullscreenBackdrop.gameObject.SetActive(true);
            StartCoroutine(DemoUtil.FadeInGUI(backgroundedGUI, fader));
        }

        private void HideBackgroundedGUI()
        {
            backgroundedGUI.gameObject.SetActive(false);
            fullscreenBackdrop.gameObject.SetActive(false);
        }

        public void OnRestartSceneButton()
        {
            Debug.Log("OnRestartSceneButton");
            SceneLookup.Get<LevelSwitcher>().ReloadCurrentLevel(fadeOutBeforeLoad: true);
        }

        public void OnBackToSceneButton()
        {
            Debug.Log("OnBackToSceneButton");
            HideBackgroundedGUI();
        }


        private void OnApplicationFocus(bool hasFocus)
        {
            // If app regains focus after ARSession is running and has existed for long enough to get past
            // permissions requests, show the backgrounded warning
            if (!Application.isEditor &&
                hasFocus && arSession != null && arSession.State == ARSessionState.Running &&
                Time.time - arSessionInitializedTime > 1f)
            {
                Debug.Log("OnApplicationFocus");
                DisplayBackgroundedGUI();
            }
        }

        // Fullscreen overlay for displaying stack track from an app exception
        private void OnLogMessageReceived_ShowScriptException(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception && scriptExceptionText != null)
            {
                // Prepend the exception and ensure the error text is active
                scriptExceptionText.text = condition + "\n\n" + stackTrace + "\n\n" + scriptExceptionText.text;
                scriptExceptionText.gameObject.SetActive(true);
            }
        }

    }
}

