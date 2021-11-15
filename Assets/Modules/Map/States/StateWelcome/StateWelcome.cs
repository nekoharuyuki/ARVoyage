using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// State in Map that displays initial instructions. This state only runs once per execution.
    /// This state also manages checking for device AR capability. If the device doesn't have AR capability, the player
    /// is not able to proceed.
    /// This state also manages preloading the ARDK feature models used in this project. If the player is not
    /// able to download the features, they are not able to proceed.
    /// </summary>
    public class StateWelcome : MonoBehaviour
    {
        private const float RetryDownloadMessageDisplayTime = 2f;
        private const float TimeBetweenDownloadProgressChangeUntilTimeout = 5f;

        private const string ErrorTextCapabilityCheck = "Your device is not supported!";
        private const string ErrorTextConnectionRequired = "Internet connection needed!";
        private const string ErrorTextReconnecting = "Trying to reconnect...";

        // Track whether this state has ever run. It runs only once per execution.
        private static bool stateRanThisExecution = false;

        // Inspector reference to the scene's CapabilityChecker to confirm the device is capable of AR
        [SerializeField] private CapabilityChecker capabilityChecker;

        // Inspector references to relevant objects
        [Header("State Machine")]
        [SerializeField] private bool isStartState = true;
        [SerializeField] private GameObject nextState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private GameObject fullscreenBackdrop;
        [SerializeField] private GameObject settingsButton;
        [SerializeField] private Button startButton;

        // Inspector references and variables for managing ARDK feature download
        [SerializeField] private FeaturePreloadManager featurePreloadManager;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI loadPercentText;

        private bool finishedFeatureDownload;
        private bool capabilityCheckSucceeded;

        private GameObject exitState;

        // Every state has a running bool that's true from OnEnable to Exit
        private bool running;

        // Fade variables
        private Fader fader;
        private float initialDelay = 0.75f;

        private BadgeManager badgeManager;
        private ErrorManager errorManager;

        void Awake()
        {
            gameObject.SetActive(isStartState);

            badgeManager = SceneLookup.Get<BadgeManager>();
            errorManager = SceneLookup.Get<ErrorManager>();

            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            // If this state ran during this execution, exit immediately
            if (stateRanThisExecution)
            {
                // Keep the gui disabled and exit
                gui.SetActive(false);
                exitState = nextState;
                running = true;
                return;
            }

            // Show fullscreen backdrop
            fullscreenBackdrop.gameObject.SetActive(true);

            // Hide settings button
            settingsButton.gameObject.SetActive(false);

            // Hide badge row
            badgeManager.DisplayBadgeRowButtons(false);

            // Hide the feature download UI
            loadingText.enabled = false;
            loadPercentText.enabled = false;

            // Set Start button non-interactable (but active/visible) until launch downloads are complete
            startButton.interactable = false;

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // Confirm this device is capable of running ARDK
            if (capabilityChecker.HasSucceeded)
            {
                OnCapabilityCheckerSuccess();
            }
            else
            {
                capabilityChecker.Success.AddListener(OnCapabilityCheckerSuccess);
                capabilityChecker.Failure.AddListener(OnCapabilityCheckerFailure);
            }

            // Set the static bool so this state won't run again this execution
            stateRanThisExecution = true;

            running = true;
        }

        void Update()
        {
            if (running)
            {
                // Once the capability check and model download have completed, activate the start button if needed
                if (!startButton.interactable && capabilityChecker.HasSucceeded && finishedFeatureDownload)
                {
                    startButton.interactable = true;
                    DemoEvents.EventStartButton.AddListener(OnEventStartButton);
                }

                // Check for state exit
                if (exitState != null)
                {
                    Exit(exitState);
                }
            }
        }

        void OnDisable()
        {
            // Unsubscribe from events
            DemoEvents.EventStartButton.RemoveListener(OnEventStartButton);
            capabilityChecker.Failure.RemoveListener(OnCapabilityCheckerFailure);
            capabilityChecker.Success.RemoveListener(OnCapabilityCheckerSuccess);
        }

        private void OnCapabilityCheckerSuccess()
        {
            capabilityChecker.Failure.RemoveListener(OnCapabilityCheckerFailure);
            capabilityChecker.Success.RemoveListener(OnCapabilityCheckerSuccess);

            // Once the capability check succeeds, begin downloading the feature models
            StartCoroutine(FeatureDownloadRoutine());
        }

        private void OnCapabilityCheckerFailure(CapabilityChecker.FailureReason failureReason)
        {
            capabilityChecker.Failure.RemoveListener(OnCapabilityCheckerFailure);
            capabilityChecker.Success.RemoveListener(OnCapabilityCheckerSuccess);

            // Permanently display the error banner
            errorManager.DisplayErrorBanner(ErrorTextCapabilityCheck, autoHideErrorBanner: false);

            // Set running false and disable the start button to hang in this state
            startButton.interactable = false;
            running = false;
        }

        private IEnumerator FeatureDownloadRoutine()
        {
            // Initialize the featurePreloadManager. This is harmless if it's already initialized
            featurePreloadManager.Initialize();

            // If features are already downloaded, disable the loading text UI and break out
            if (featurePreloadManager.AreAllFeaturesDownloaded())
            {
                Debug.Log(name + " all features already donwnloaded - bypassing FeatureDownloadRoutine");
                finishedFeatureDownload = true;
                yield break;
            }

            // Otherwise, start the feature download
            Debug.Log(name + " beginning feature download");

            loadingText.enabled = true;
            loadPercentText.enabled = true;
            SetLoadingProgressText(0);

            // Listen for updates as the features download
            FeaturePreloadManager.PreloadProgressUpdatedArgs lastPreloadProgressArgs = null;

            ArdkEventHandler<FeaturePreloadManager.PreloadProgressUpdatedArgs> onPreloadProgressUpdated =
                (FeaturePreloadManager.PreloadProgressUpdatedArgs args) => { lastPreloadProgressArgs = args; };

            featurePreloadManager.ProgressUpdated += onPreloadProgressUpdated;

            float lastProgress = 0f;
            float lastProgressChangeTime = Time.time;

            // Begin the feature download
            featurePreloadManager.StartDownload();

            // Wait feature download to complete or timeout
            while (
                // There has either been no progress, or only incomplete progress 
                (lastPreloadProgressArgs == null || !lastPreloadProgressArgs.PreloadAttemptFinished) &&
                // AND it hasn't been too long since the last progress change
                Time.time - lastProgressChangeTime < TimeBetweenDownloadProgressChangeUntilTimeout)
            {
                float progress = lastPreloadProgressArgs == null ? 0 : lastPreloadProgressArgs.Progress;

                if (!Mathf.Approximately(progress, lastProgress))
                {
                    lastProgressChangeTime = Time.time;
                }

                lastProgress = progress;

                SetLoadingProgressText(progress);

                yield return null;
            }

            // Once finished, unsubscribe from future updates
            featurePreloadManager.ProgressUpdated -= onPreloadProgressUpdated;

            // Stop the download in case if it's still running
            featurePreloadManager.StopDownload();

            // It's a success if the attempt finished with no failures
            bool success = lastPreloadProgressArgs.PreloadAttemptFinished && lastPreloadProgressArgs.FailedPreloads.Count == 0;

            if (success)
            {
                Debug.Log(name + " feature download succeeded.");

                // Set progress to 100% and we're done
                SetLoadingProgressText(1f);

                finishedFeatureDownload = true;
            }
            else
            {
                Debug.Log(name + " feature download failed " + (lastPreloadProgressArgs.PreloadAttemptFinished ? " after completing." : " after timeout."));

                // Display the connection required banner
                errorManager.DisplayErrorBanner(ErrorTextConnectionRequired, autoHideErrorBanner: false);

                // Wait before displaying next message
                yield return new WaitForSeconds(RetryDownloadMessageDisplayTime);

                // Display the reconnecting banner
                errorManager.DisplayErrorBanner(ErrorTextReconnecting, autoHideErrorBanner: false);

                // Wait before retrying
                yield return new WaitForSeconds(RetryDownloadMessageDisplayTime);

                // Hide the banner
                errorManager.HideErrorBanner();

                Debug.Log(name + " retrying feature download.");

                // Run the download routine again
                StartCoroutine(FeatureDownloadRoutine());
            }
        }

        private void SetLoadingProgressText(float progress)
        {
            int progressPercent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100);
            loadPercentText.text = progressPercent + "%";
        }

        private void OnEventStartButton()
        {
            exitState = nextState;
        }

        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Fade out GUI if needed
            if (gui.activeInHierarchy)
            {
                yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));
            }

            // Hide fullscreen backdrop
            fullscreenBackdrop.gameObject.SetActive(false);

            // Show settings button
            settingsButton.gameObject.SetActive(true);

            // Show badge row
            badgeManager.DisplayBadgeRowButtons(true);

            // Activate the next state
            nextState.SetActive(true);

            // Deactivate this state
            gameObject.SetActive(false);
        }
    }
}
