using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// State in BuildAShip that prompts the player to collect an enviroment resource,
    /// grass, trees or sky. (After completing, this state is returned to, for a total of 
    /// 3 times through, one per resource.)
    ///
    /// A collect button and hint banner are activated on the HUD.
    /// When that resource is visually detected in the environment, and the collect button is held down,
    /// particles of that resource are vacuumed into the collect button's funnel.
    /// Once enough particles of that resource are collected, the state completes. 
    /// (The state can be exited early by pressing the "Skip" button in the debug menu.)
    /// Its next state (set via inspector) is StateCollectDone.
    /// </summary>
    public class StateCollect : MonoBehaviour
    {
        private BuildAShipManager buildAShipManager;
        private BuildAShipResourceRenderer buildAShipResourceRenderer;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        public bool running { get; private set; } = false;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text guiText;
        [SerializeField] private GameObject hintBanner;
        private CanvasGroup guiCanvasGroup;
        private Fader fader;
        protected const float fadeDuration = 0f;

        EnvResource envResourceToCollect;
        private int totalCollectCount = 0;
        private int latestActiveCollectCount = 0;
        private bool foundCollectable = false;
        private float timeFoundCollectable = 0f;
        private bool collecting = false;
        private const float percAlmostDoneCollecting = 0.65f;

        protected float minFindHintDuration = DemoUtil.minUIDisplayDuration;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            buildAShipManager = SceneLookup.Get<BuildAShipManager>();
            buildAShipResourceRenderer = SceneLookup.Get<BuildAShipResourceRenderer>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            BuildAShipEvents.EventCollectButtonHeld.AddListener(OnEventCollectButtonHeld);
            BuildAShipEvents.EventCollectButtonReleased.AddListener(OnEventCollectButtonReleased);

            // Get resource to collect
            envResourceToCollect = buildAShipManager.GetResourceToCollect();
            latestActiveCollectCount = 0;
            totalCollectCount = 0;
            collecting = false;

            // Display that resource
            buildAShipResourceRenderer.SetResource(envResourceToCollect);
            buildAShipResourceRenderer.SetVisible(true);

            // Set the gauge icon
            buildAShipManager.progressGauge.SetIcon(envResourceToCollect.ResourceSprite);

            // Fade in GUI
            hintBanner.SetActive(true);
            gui.SetActive(true);
            guiCanvasGroup = gui.GetComponent<CanvasGroup>();
            guiCanvasGroup.alpha = 0;
            fader.Fade(guiCanvasGroup, alpha: 1f, duration: fadeDuration, initialDelay: initialDelay);

            // Display collect button
            buildAShipManager.collectButton.gameObject.SetActive(true);

            // Display collect progress gauge
            buildAShipManager.progressGauge.gameObject.SetActive(true);
            UpdateCollectProgressGauge(forceUpdate: true);

            running = true;
        }


        void OnDisable()
        {
            // Unsubscribe from events
            BuildAShipEvents.EventCollectButtonHeld.RemoveListener(OnEventCollectButtonHeld);
            BuildAShipEvents.EventCollectButtonReleased.RemoveListener(OnEventCollectButtonReleased);
        }

        private void OnEventCollectButtonHeld()
        {
            Debug.Log("CollectButton held");

            // hide hint banner during vacuuming
            hintBanner.SetActive(false);

            collecting = true;
            buildAShipResourceRenderer.SetCollecting(collecting);
        }

        private void OnEventCollectButtonReleased()
        {
            Debug.Log("CollectButton released");

            // unhide hint banner during vacuuming
            hintBanner.SetActive(true);

            collecting = false;
            buildAShipResourceRenderer.SetCollecting(collecting);

            totalCollectCount += latestActiveCollectCount;
            latestActiveCollectCount = 0;
        }


        void Update()
        {
            if (!running) return;

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }

            // Remember each time we first find a collectable, 
            // to know how long to hold the Find hint
            if (buildAShipResourceRenderer.activeItemCount == 0)
            {
                foundCollectable = false;
            }
            else if (!foundCollectable)
            {
                foundCollectable = true;
                timeFoundCollectable = Time.time;
            }

            // Update HUD guidance text
            if (!foundCollectable ||
                Time.time - timeFoundCollectable < minFindHintDuration ||
                Time.time - timeStartedState < minFindHintDuration)
            {
                DisplayFindText();
            }
            else
            {
                DisplayCollectText();
            }

            UpdateCollectProgressGauge();

            // Done when we've collected enough
            if (GetAmountCollected() >= BuildAShipManager.numResourcesParticlesToCollect)
            {
                exitState = nextState;
            }
        }

        private void DisplayFindText()
        {
            string toCollectStr = "Find " + envResourceToCollect.ChannelName +
                                  " to collect " + envResourceToCollect.ResourceName + "!!";
            if (guiText != null)
            {
                guiText.text = toCollectStr;
            }
        }

        private void DisplayCollectText()
        {
            string toCollectStr = GetCollectProgress() < percAlmostDoneCollecting ?
                                    "Tap and hold to collect!" :
                                    "Almost there!";
            if (guiText != null)
            {
                guiText.text = toCollectStr;
            }
        }


        private int GetAmountCollected()
        {
            return collecting ? totalCollectCount + latestActiveCollectCount : totalCollectCount;
        }

        public void ExitStateEarly()
        {
            exitState = nextState;
        }


        private float GetCollectProgress()
        {
            return Mathf.Clamp01((float)GetAmountCollected() /
                                 (float)BuildAShipManager.numResourcesParticlesToCollect);
        }

        private void UpdateCollectProgressGauge(bool forceUpdate = false)
        {
            int activeCount = buildAShipResourceRenderer.collectedItemCount;
            if (activeCount != latestActiveCollectCount || forceUpdate)
            {
                latestActiveCollectCount = activeCount;

                buildAShipManager.progressGauge.FillToPercent(GetCollectProgress());
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Done collecting
            buildAShipResourceRenderer.SetCollecting(false);
            buildAShipResourceRenderer.SetVisible(false);

            // Hide collect button
            buildAShipManager.collectButton.gameObject.SetActive(false);

            // Fade out GUI
            yield return fader.Fade(guiCanvasGroup, alpha: 0f, duration: fadeDuration);

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
