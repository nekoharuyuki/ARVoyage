using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// State in BuildAShip that activates a ghosted yeti to be placed, and a placement button on the HUD.
    /// It instructs the player to click the button to place the yeti on the AR ground plane.
    /// Its next state (set via inspector) is StateYetiRequest.
    /// </summary>
    public class StatePlacement : MonoBehaviour
    {
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
        [SerializeField] private GameObject placementButton;
        private CanvasGroup guiCanvasGroup;
        private Fader fader;
        protected const float fadeDuration = 0f;


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

            // Subscribe to events
            BuildAShipEvents.EventLocationPlacementButton.AddListener(OnEventLocationPlacementButton);

            // Fade in GUI
            gui.SetActive(true);
            guiCanvasGroup = gui.GetComponent<CanvasGroup>();
            guiCanvasGroup.alpha = 0;
            fader.Fade(guiCanvasGroup, alpha: 1f, duration: fadeDuration, initialDelay: initialDelay);

            // Show reticle
            buildAShipManager.cameraReticle.gameObject.SetActive(true);

            running = true;
        }


        void OnDisable()
        {
            // Unsubscribe from events
            BuildAShipEvents.EventLocationPlacementButton.RemoveListener(OnEventLocationPlacementButton);
        }


        private void OnEventLocationPlacementButton()
        {
            Debug.Log("LocationPlacementButton pressed");

            // DONE - ready to exit this state to the next state
            exitState = nextState;
            Debug.Log(thisState + " beginning transition to " + exitState);
        }


        void Update()
        {
            if (!running) return;

            buildAShipManager.cameraReticle.UpdateReticle();
            buildAShipManager.UpdateYetiInitialPlacement();

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
            // swap ghost yeti for opaque yeti
            buildAShipManager.UpdateYetiInitialPlacementDone();

            // Hide reticle
            buildAShipManager.cameraReticle.gameObject.SetActive(false);

            // Fade out GUI
            yield return fader.Fade(guiCanvasGroup, alpha: 0f, duration: fadeDuration);

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
