using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// State in Map that controls display and behavior of the Niantic airship.
    /// When player has unlocked all badges, the airship flies away and the player is presented with a thank you message.
    /// </summary>
    public class StateAirship : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private bool isStartState = false;
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject thankYouGUI;
        [SerializeField] private GameObject resetProgressGUI;

        private Fader fader;

        private Airship airship;
        private MapActor yetiActor;
        private BadgeManager badgeManager;


        void Awake()
        {
            gameObject.SetActive(isStartState);

            airship = SceneLookup.Get<Airship>();
            yetiActor = SceneLookup.Get<MapActor>();
            badgeManager = SceneLookup.Get<BadgeManager>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            MapEvents.EventResetProgressRequestButton.AddListener(OnEventResetProgressRequestButton);
            MapEvents.EventResetProgressConfirmButton.AddListener(OnEventResetProgressConfirmButton);
            MapEvents.EventResetProgressCancelButton.AddListener(OnEventResetProgressCancelButton);
            MapEvents.EventBackToMapButton.AddListener(OnEventBackToMapButton);

            // Be sure GUIs are hidden
            thankYouGUI.SetActive(false);
            resetProgressGUI.SetActive(false);

            // Start the coroutine that runs the stages of this state
            StartCoroutine(AirshipStagesRoutine());

            running = true;
        }


        void OnDisable()
        {
            MapEvents.EventResetProgressRequestButton.RemoveListener(OnEventResetProgressRequestButton);
            MapEvents.EventResetProgressConfirmButton.RemoveListener(OnEventResetProgressConfirmButton);
            MapEvents.EventResetProgressCancelButton.RemoveListener(OnEventResetProgressCancelButton);
            MapEvents.EventBackToMapButton.RemoveListener(OnEventBackToMapButton);
        }

        private IEnumerator AirshipStagesRoutine()
        {
            // If airship is unlocked but not yet built, build it and wait for completion
            if (SaveUtil.IsAirshipUnlocked() && !SaveUtil.IsAirshipBuilt())
            {
                Debug.Log(name + " building airship");
                yield return airship.Build();
            }

            // If airship is built and all badges are earned, trigger the airship depart
            if (SaveUtil.IsAirshipBuilt() && !SaveUtil.IsAirshipDeparted() &&
                badgeManager.AreAllBadgesPresented())
            {
                Debug.Log(name + " airship departing");

                // hide yeti
                yield return yetiActor.BubbleScaleDown();

                // wait a moment
                yield return new WaitForSeconds(.5f);

                // trigger and wait for airship to depart
                yield return airship.Depart();
            }

            // If airship is departed, but the thank you hasn't completed, trigger thankYou GUI
            if (SaveUtil.IsAirshipDeparted() && !SaveUtil.IsThankYouCompleted())
            {
                // Fade in GUI
                StartCoroutine(DemoUtil.FadeInGUI(thankYouGUI, fader));
                SaveUtil.SaveThankYouCompleted();
            }

            // else move on to next state
            else
            {
                exitState = nextState;
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
        }

        // Switch from thankYouGUI to resetProgressGUI
        private void OnEventResetProgressRequestButton()
        {
            thankYouGUI.SetActive(false);
            resetProgressGUI.SetActive(true);
        }

        // Move on to next state
        private void OnEventBackToMapButton()
        {
            // Save that the thank you process is completed
            SaveUtil.SaveThankYouCompleted();

            yetiActor.BubbleScaleUp();
            exitState = nextState;
        }

        // Clear all save data and reload the map scene
        private void OnEventResetProgressConfirmButton()
        {
            SaveUtil.Clear();
            SceneLookup.Get<LevelSwitcher>().LoadLevel(Level.Map, fadeOutBeforeLoad: true);
        }

        // Switch from resetProgressGUI back to thankYouGUI
        private void OnEventResetProgressCancelButton()
        {
            resetProgressGUI.SetActive(false);
            thankYouGUI.SetActive(true);
        }

        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Fade out GUI if needed
            if (thankYouGUI.activeInHierarchy)
            {
                yield return StartCoroutine(DemoUtil.FadeOutGUI(thankYouGUI, fader));
            }
            if (resetProgressGUI.activeInHierarchy)
            {
                yield return StartCoroutine(DemoUtil.FadeOutGUI(resetProgressGUI, fader));
            }

            nextState.SetActive(true);
            gameObject.SetActive(false);

            yield break;
        }
    }

}
