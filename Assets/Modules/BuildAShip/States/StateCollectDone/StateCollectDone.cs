using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// State in BuildAShip that displays a collection-success message.
    /// Its next state (set via inspector) is StateCollect if more resources are to be collected,
    /// or StateGameOver if all 3 resources have been collected.
    /// </summary>
    public class StateCollectDone : MonoBehaviour
    {
        private BuildAShipManager buildAShipManager;
        private AudioManager audioManager;

        [Header("State machine")]
        [SerializeField] private GameObject recollectState;
        [SerializeField] private GameObject nextState;
        public bool running { get; private set; } = false;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0f;
        private float defaultStateDuration = 4f;
        private float lastResourceStateDuration = 2f;


        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text guiText;
        private CanvasGroup guiCanvasGroup;
        private Fader fader;
        protected const float fadeDuration = 0f;

        private bool collectedAllResources = false;
        private bool playedCompletionSequence = false;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            buildAShipManager = SceneLookup.Get<BuildAShipManager>();
            fader = SceneLookup.Get<Fader>();
            audioManager = SceneLookup.Get<AudioManager>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            playedCompletionSequence = false;

            // Set GUI text to match resource just collected
            string collectedStr =
                buildAShipManager.resourcesToCollectOrder[buildAShipManager.numResourcesCollected].ResourceName +
                " collected!";
            if (guiText != null)
            {
                guiText.text = collectedStr;
            }

            // INCREMENT NUM RESOURCES COLLECTED
            ++buildAShipManager.numResourcesCollected;
            collectedAllResources = buildAShipManager.numResourcesCollected >= buildAShipManager.resourcesToCollectOrder.Length;

            // SFX
            audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Success_Magic);

            // Fade in GUI
            gui.SetActive(true);
            guiCanvasGroup = gui.GetComponent<CanvasGroup>();
            guiCanvasGroup.alpha = 0;
            fader.Fade(guiCanvasGroup, alpha: 1f, duration: fadeDuration, initialDelay: initialDelay);

            running = true;
        }


        void Update()
        {
            if (!running) return;

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }

            if (!playedCompletionSequence)
            {
                playedCompletionSequence = true;

                // Play the collect progress gauge completion sequence
                buildAShipManager.progressGauge.PlayCompletedSequence(visibleAtEnd: true);
            }

            if (Time.time - timeStartedState > (collectedAllResources ? lastResourceStateDuration : defaultStateDuration))
            {
                exitState = collectedAllResources ? nextState : recollectState;
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Hide the progress gauge
            yield return buildAShipManager.progressGauge.PlayResetSequence(visibleAtEnd: false, returnToFillStartFrame: true);
            buildAShipManager.progressGauge.gameObject.SetActive(false);

            // Fade out GUI
            yield return fader.Fade(guiCanvasGroup, alpha: 0f, duration: fadeDuration);

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
