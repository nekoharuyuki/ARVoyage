using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// State in BuildAShip that displays a speech bubble hint next to yeti for a few seconds.
    /// Its next state (set via inspector) is StateCollect.
    /// </summary>
    public class StateYetiRequest : MonoBehaviour
    {
        private const float yetiRequestDuration = 6f;

        private BuildAShipManager buildAShipManager;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        private Fader fader;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            buildAShipManager = SceneLookup.Get<BuildAShipManager>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // Show yeti speech bubble
            buildAShipManager.yetiActor.SetSpeechBubbleVisible(true);

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

            if (Time.time - timeStartedState > yetiRequestDuration)
            {
                exitState = nextState;
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Hide yeti speech bubble
            buildAShipManager.yetiActor.SetSpeechBubbleVisible(false);

            // Hide yeti
            bool yetiDone = false;
            DemoUtil.DisplayWithBubbleScale(buildAShipManager.yetiActor.gameObject,
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

    }
}
