using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Host-only state presenting GUI confirming host knows they are the sole player -
    ///  needed in case host doesn't wait for joiners to join.
    /// Next state (set via inspector) is StateCountdown, 
    ///  unless back button is pressed to return to StateWaiting.
    /// </summary>
    public class StateSoloConfirm : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        [SerializeField] private GameObject prevState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        // Exit state will be determined by the user's input: Confirm / Back
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;

        // Fade variables
        private Fader fader;
        private float initialDelay = 0f;

        private SnowballFightManager snowballFightManager;

        void Awake()
        {
            // This is not the first state, start off disabled
            gameObject.SetActive(false);

            snowballFightManager = SceneLookup.Get<SnowballFightManager>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventConfirmButton.AddListener(OnEventConfirmButton);
            SnowballFightEvents.EventBackButton.AddListener(OnEventBackButton);
            SnowballFightEvents.EventGameStart.AddListener(OnEventGameStart);

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

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
        }

        void OnDisable()
        {
            // Unsubscribe from events
            SnowballFightEvents.EventConfirmButton.RemoveListener(OnEventConfirmButton);
            SnowballFightEvents.EventBackButton.RemoveListener(OnEventBackButton);
            SnowballFightEvents.EventGameStart.RemoveListener(OnEventGameStart);
        }


        private void OnEventConfirmButton()
        {
            // Confirmed -- START GAME
            // this will trigger OnEventGameStart below
            snowballFightManager.StartGame();
        }

        private void OnEventGameStart()
        {
            exitState = nextState;
        }

        private void OnEventBackButton()
        {
            exitState = prevState;
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
    }
}
