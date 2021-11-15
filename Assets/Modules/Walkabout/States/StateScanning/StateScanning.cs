using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// State in Walkabout that turns on gameboard scanning, 
    /// and instructs the player to scan the environment.
    /// It waits until enough gameboard has been created that the yeti could be placed on it.
    /// Its next state (set via inspector) is StatePlacement.
    /// </summary>
    public class StateScanning : MonoBehaviour
    {
        private string[] scanningTextStr = { "Scan the ground!", "Keep scanning!" };

        private WalkaboutManager walkaboutManager;
        private GameboardHelper gameboardHelper;


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
        protected const float fadeInDuration = 0.5f;
        protected const float fadeOutDuration = 0f;

        private float scanningHUDUpdatePeriod = 0.5f;
        Coroutine scanningPollRoutine = null;
        protected float secsTillKeepScanning = 5f;
        protected float minScanningDuration = DemoUtil.minUIDisplayDuration;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            walkaboutManager = SceneLookup.Get<WalkaboutManager>();
            gameboardHelper = SceneLookup.Get<GameboardHelper>();
            fader = SceneLookup.Get<Fader>();

            walkaboutManager.cameraReticle.UpdateRaycastVectorOffset();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, fadeDuration: fadeInDuration, initialDelay: initialDelay));

            // Init manager state
            walkaboutManager.yetiAndSnowball.gameObject.SetActive(false);
            gameboardHelper.SetIsScanning(true);

            // Inform the gameboard to select the current surface based on the player's position
            gameboardHelper.SetSurfaceSelectionModeCameraForward();

            // Periodically update HUD with scanning results
            scanningPollRoutine = StartCoroutine(ScanningPollRoutine());

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

            // Update managers
            walkaboutManager.cameraReticle.UpdateReticle();
        }


        // poll for the latest node count from Gameboard
        private IEnumerator ScanningPollRoutine()
        {
            while (true)
            {
                int numGameboardNodes = gameboardHelper.GetNumGameboardNodes();

                // Is there an inner node on the gameboard, to place yeti onto?
                if (gameboardHelper.DoesGameboardInnerNodeExist() &&
                    Time.time - timeStartedState > minScanningDuration)
                {
                    // DONE - ready to exit this state to the next state
                    exitState = nextState;
                    Debug.Log(thisState + " beginning transition to " + exitState);
                    yield break;
                }

                // update HUD
                int whichMessage = (Time.time - timeStartedState > secsTillKeepScanning) ? 1 : 0;
                string str = scanningTextStr[whichMessage]; // + "\n" + numGameboardNodes;
                scanningText.text = str;

                // wait till next poll time
                yield return new WaitForSeconds(scanningHUDUpdatePeriod);
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            // Stop checking for IsMinimumScanningDone
            if (scanningPollRoutine != null)
            {
                StopCoroutine(scanningPollRoutine);
            }

            StartCoroutine(ExitRoutine(nextState));
        }


        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader, fadeDuration: fadeOutDuration));

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
