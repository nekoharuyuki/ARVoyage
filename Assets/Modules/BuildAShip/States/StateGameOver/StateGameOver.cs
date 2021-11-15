using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// The final State in BuildAShip, displaying a success message,
    /// and button options to either restart the demo, or return to Map.
    /// Its next state (set via inspector) is StateScanning (if player chooses Restart),
    /// otherwise LevelSwitcher ReturnToMap() is called.
    /// </summary>
    public class StateGameOver : MonoBehaviour
    {
        private BuildAShipManager buildAShipManager;
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
        private Fader fader;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            fader = SceneLookup.Get<Fader>();
            buildAShipManager = SceneLookup.Get<BuildAShipManager>();
            audioManager = SceneLookup.Get<AudioManager>();
        }


        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Save journey progress
            SaveUtil.SaveBadgeUnlocked(Level.BuildAShip);
            SaveUtil.SaveAirshipUnlocked();

            // Subscribe to events
            BuildAShipEvents.EventRestartButton.AddListener(OnEventRestartButton);

            // SFX
            audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Winner_Fanfare);

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            running = true;
        }

        void OnDisable()
        {
            // Unsubscribe from events
            BuildAShipEvents.EventRestartButton.RemoveListener(OnEventRestartButton);
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
                buildAShipManager.RestartGame();

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
            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

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
