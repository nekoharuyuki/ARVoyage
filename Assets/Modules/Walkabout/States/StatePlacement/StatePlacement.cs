using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// State in Walkabout that activates the camera reticle with ghosted yeti to be placed, 
    /// and activates a placement button on the HUD,
    /// and instructs the player to click the button to place the yeti on the gameboard.
    /// Its next state (set via inspector) is StateGrowSnowball.
    /// </summary>
    public class StatePlacement : MonoBehaviour
    {
        private WalkaboutManager walkaboutManager;
        private GameboardHelper gameboardHelper;

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

            walkaboutManager = SceneLookup.Get<WalkaboutManager>();
            gameboardHelper = SceneLookup.Get<GameboardHelper>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            WalkaboutEvents.EventPlacementButton.AddListener(OnEventPlacementButton);

            // Fade in GUI
            gui.SetActive(true);
            guiCanvasGroup = gui.GetComponent<CanvasGroup>();
            guiCanvasGroup.alpha = 0;
            fader.Fade(guiCanvasGroup, alpha: 1f, duration: fadeDuration, initialDelay: initialDelay);

            // Show Placement button and reticle
            walkaboutManager.placementButton.gameObject.SetActive(true);
            walkaboutManager.cameraReticle.gameObject.SetActive(true);

            running = true;
        }


        void OnDisable()
        {
            // Unsubscribe from events
            WalkaboutEvents.EventPlacementButton.RemoveListener(OnEventPlacementButton);
        }

        private void OnEventPlacementButton()
        {
            Debug.Log("PlacementButton pressed");

            walkaboutManager.CacheYetiGameboardPosition();

            // Once the yeti is placed, 
            // the gameboard should now select the surface that's under the actor's center point
            gameboardHelper.SetSurfaceSelectionModeActorDown(walkaboutManager.ActorCenterTransform);

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

            // Update managers
            walkaboutManager.cameraReticle.UpdateReticle();
            walkaboutManager.UpdateYetiInitialPlacement();
        }



        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // swap ghost yeti for opaque yeti
            walkaboutManager.UpdateYetiInitialPlacementDone();

            // hide placement button
            walkaboutManager.placementButton.gameObject.SetActive(false);

            // Fade out GUI
            yield return fader.Fade(guiCanvasGroup, alpha: 0f, duration: fadeDuration);

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
