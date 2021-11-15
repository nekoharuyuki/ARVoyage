using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// State in Walkabout that activates the bottom half (base) of a snowman.
    /// Similar to StateGrowSnowball, the player can repeatedly set
    /// a locomotion destination target for the yeti to roll its snowball to.
    /// This state completes once the snowball gets very close to the snowman,
    /// when this state activates a fully built snowman.
    /// Its next state (set via inspector) is StateGameOver.
    /// </summary>
    public class StateBuildSnowman : MonoBehaviour
    {
        private string rollToSnowmanText = "Roll into the base to finish!";

        private WalkaboutManager walkaboutManager;
        private GameboardHelper gameboardHelper;
        private AudioManager audioManager;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        [SerializeField] private GameObject placementState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text guideText;
        private Fader fader;
        protected const float fadeInDuration = 0f;
        protected const float fadeOutDuration = 0.5f;

        private bool snowmanCreated = false;
        private bool buildingSnowman = false;
        private float timeStartedBuildingSnowman = 0f;

        private string invalidGameboardHint = null;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            walkaboutManager = SceneLookup.Get<WalkaboutManager>();
            gameboardHelper = SceneLookup.Get<GameboardHelper>();
            audioManager = SceneLookup.Get<AudioManager>();
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
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // Init HUD
            UpdateHUD();

            snowmanCreated = false;
            buildingSnowman = false;

            // Create snowman
            StartCoroutine(CreateSnowmanRoutine());

            // SFX for fullsize snowball
            audioManager.PlayAudioOnObject(AudioKeys.SFX_Snowball_SizeAchieved, walkaboutManager.yetiAndSnowball.gameObject);

            // Tell scene manager we're in this state
            walkaboutManager.inStateBuild = true;

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

            if (!walkaboutManager.cameraReticle.isTooCloseToGameObject)
            {
                bool setDest = walkaboutManager.SetYetiDestination();

                // If no destination could be set, show invalidGameboardHint
                invalidGameboardHint = setDest ? null : WalkaboutManager.invalidGameboardHint;
                UpdateHUD();
            }
        }


        private IEnumerator CreateSnowmanRoutine()
        {
            while (true)
            {
                // CreateSnowman will fail if there is no current surface
                snowmanCreated = walkaboutManager.CreateSnowman();
                if (snowmanCreated)
                {
                    // break out of routine once snowman is created
                    yield break;
                }

                // try again after a short delay
                float waitTill = Time.time + 0.5f;
                while (Time.time < waitTill) yield return null;
            }
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
            if (!buildingSnowman)
            {
                walkaboutManager.cameraReticle.UpdateReticle(
                    // Remove constraint of disallowing destinations close to yeti
                    //gameObjectForProximityCheck: walkaboutManager.yetiAndSnowball.gameObject,
                    //closeToGameObjectDist: WalkaboutManager.reticleCloseToYetiDist
                    );
                walkaboutManager.HandleDynamicGameboard(includeSnowman: snowmanCreated);
            }
            walkaboutManager.UpdateYetiLocomotion();
            walkaboutManager.UpdateSnowmanHoverVFX();

            // ----

            // BUILD COMPLETE SNOWMAN once yeti is very close to it
            if (!buildingSnowman && walkaboutManager.IsSnowballNearSnowman())
            {
                buildingSnowman = true;
                timeStartedBuildingSnowman = Time.time;

                // SFX for building snowman
                audioManager.PlayAudioOnObject(AudioKeys.SFX_SnowmanBuild, walkaboutManager.yetiAndSnowball.gameObject);

                // SFX for success
                audioManager.PlayAudioAtPosition(AudioKeys.SFX_Success_Magic, walkaboutManager.yetiAndSnowball.gameObject.transform.position);

                walkaboutManager.CompleteSnowman();

                // Hide Placement button and reticle
                walkaboutManager.placementButton.gameObject.SetActive(false);
                walkaboutManager.cameraReticle.gameObject.SetActive(false);

                // turn off scanning
                gameboardHelper.SetIsScanning(false);
            }

            // DONE a few seconds after the snowman is built
            if (buildingSnowman &&
                Time.time - timeStartedBuildingSnowman > 3f)
            {
                // Ready to exit this state to the next state
                exitState = nextState;
                Debug.Log(thisState + " beginning transition to " + exitState);
            }
        }


        private void UpdateHUD()
        {
            // invalid gameboard hint
            if (invalidGameboardHint != null)
            {
                guideText.text = invalidGameboardHint;
            }

            // roll to snownman hint
            else
            {
                guideText.text = rollToSnowmanText;
            }
        }

        public void RewindToPlacement()
        {
            walkaboutManager.yetiAndSnowball.Stop();
            exitState = placementState;
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Hide Placement button and gauge
            walkaboutManager.placementButton.gameObject.SetActive(false);
            walkaboutManager.progressGauge.gameObject.SetActive(false);

            // Tell scene manager we're leaving this state
            walkaboutManager.inStateBuild = false;

            // Fade out GUI
            // Already done when snowman was built
            //yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);

            yield break;
        }

    }
}
