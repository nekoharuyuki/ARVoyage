using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// The final State in Walkabout, displaying the final step count,
    /// and button options to either restart the demo, or return to Map.
    /// Its next state (set via inspector) is StateScanning (if player chooses Restart),
    /// otherwise LevelSwitcher ReturnToMap() is called.
    /// </summary>
    public class StateGameOver : MonoBehaviour
    {
        protected string victoryTextStr = "Great Job!\n\n";
        protected string tryAgainTextStr = "Try Again!\n\n";

        private WalkaboutManager walkaboutManager;
        private AudioManager audioManager;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0.75f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private GameObject yetiVictoryImage;
        [SerializeField] private GameObject yetiLoseImage;
        [SerializeField] private TMPro.TMP_Text titleText;
        [SerializeField] private TMPro.TMP_Text stepsText;
        private Fader fader;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            fader = SceneLookup.Get<Fader>();
            walkaboutManager = SceneLookup.Get<WalkaboutManager>();
            audioManager = SceneLookup.Get<AudioManager>();
        }


        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Save journey progress
            SaveUtil.SaveBadgeUnlocked(Level.Walkabout);

            // victory?
            int stepCount = walkaboutManager.GetNumYetiSteps();
            bool victory = stepCount >= WalkaboutManager.minVictorySteps;


            // Subscribe to events
            WalkaboutEvents.EventRestartButton.AddListener(OnEventRestartButton);

            // Set GUI image and text
            yetiVictoryImage.gameObject.SetActive(victory);
            yetiLoseImage.gameObject.SetActive(!victory);
            titleText.text = victory ? victoryTextStr : tryAgainTextStr;
            stepsText.text = stepCount.ToString() + " Steps!";

            // SFX
            audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Winner_Fanfare);

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            running = true;
        }

        void OnDisable()
        {
            // Unsubscribe from events
            WalkaboutEvents.EventRestartButton.RemoveListener(OnEventRestartButton);
        }

        private void OnEventRestartButton()
        {
            Debug.Log("RestartButton pressed");

            // DONE - ready to exit this state to the next state
            exitState = nextState;
            Debug.Log(thisState + " beginning transition to " + exitState);
        }


        void Update()
        {
            if (!running) return;

            if (exitState != null)
            {
                // Update managers
                walkaboutManager.RestartGame();

                Exit(exitState);
                return;
            }
        }

        private void Exit(GameObject nextState)
        {
            running = false;
            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Hide yeti
            bool yetiDone = false;
            DemoUtil.DisplayWithBubbleScale(walkaboutManager.yetiAndSnowball.gameObject,
                                            show: false,
                                            onComplete: () => yetiDone = true);

            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            // Wait for yeti bubble-scale animation to complete
            while (!yetiDone) yield return null;

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }


        public void OnEventBackToMapButton()
        {
            Debug.Log("BackToMapButton pressed");

            StartCoroutine(BackToMapRoutine());
        }

        private IEnumerator BackToMapRoutine()
        {
            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            // Return to map
            SceneLookup.Get<LevelSwitcher>().ReturnToMap();
        }
    }
}
