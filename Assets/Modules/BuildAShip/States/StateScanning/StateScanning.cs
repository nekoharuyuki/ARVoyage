using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// State in BuildAShip that waits for the player to scan the environment, to find an AR ground plane.
    /// It polls for the camera reticle having hit an AR ground plane.
    /// Its next state (set via inspector) is StatePlacement.
    /// </summary>
    public class StateScanning : MonoBehaviour
    {
        private BuildAShipManager buildAShipManager;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 1f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text scanningText;
        private Fader fader;

        protected float minScanningDuration = DemoUtil.minUIDisplayDuration;


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

            // Clear found-reticle flag, in case this state is restarting
            buildAShipManager.cameraReticle.everFoundSurfaceForReticle = false;

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // hide yeti
            buildAShipManager.yetiActor.gameObject.SetActive(false);

            running = true;
        }


        void Update()
        {
            if (!running) return;

            buildAShipManager.cameraReticle.UpdateReticle();

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }

            if (buildAShipManager.cameraReticle.everFoundSurfaceForReticle &&
                Time.time - timeStartedState > minScanningDuration)
            {
                exitState = nextState;
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            // Update managers

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

    }
}
