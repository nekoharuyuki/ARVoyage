using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Universal state used by all demos, displaying a GUI with warning text about using AR.
    /// Its next state is set via inspector, custom to that demo.
    /// </summary>
    public class StateWarning : MonoBehaviour
    {
        // static bool for whether this state (shared across demos) has occurred or not
        public static bool occurred = false;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private GameObject fullscreenBackdrop;

        // Fade variables
        private Fader fader;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            DemoEvents.EventWarningOkButton.AddListener(OnEventOkButton);

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader));

            occurred = true;

            running = true;
        }

        void OnDisable()
        {
            // Unsubscribe from events
            DemoEvents.EventWarningOkButton.RemoveListener(OnEventOkButton);
        }

        private void OnEventOkButton()
        {
            Debug.Log("OkButton pressed");

            // DONE - ready to exit this state to the next state
            exitState = nextState;
            Debug.Log(thisState + " beginning transition to " + exitState);
        }

        void Update()
        {
            if (!running) return;

            if (exitState != null)
            {
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
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            // Hide fullscreen backdrop
            if (fullscreenBackdrop != null)
            {
                fullscreenBackdrop.gameObject.SetActive(false);
            }

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }
    }
}
