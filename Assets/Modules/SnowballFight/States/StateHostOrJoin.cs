using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// State presenting GUI asking player if they want to Host or Join the multiplayer SnowballFight.
    /// Next state (set via inspector) is either StateHostInstructions or StateJoinSession.
    /// </summary>
    public class StateHostOrJoin : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private GameObject nextStateJoin;
        [SerializeField] private GameObject nextStateHost;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        // Exit state will be determined by the user's input: Host or Join
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private GameObject fullscreenBackdrop;

        // Fade variables
        private Fader fader;

        // This is the very first state, so wait for initial fade in to complete
        private float initialDelay = 0.75f;

        void Awake()
        {
            if (DevSettings.SkipToSnowballFightMainInEditor)
            {
                gameObject.SetActive(false);
            }
            else
            {
                // This is the first state, start off enabled
                gameObject.SetActive(true);
            }

            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventHostButton.AddListener(OnEventHostButton);
            SnowballFightEvents.EventJoinButton.AddListener(OnEventJoinButton);

            // Show fullscreen backdrop
            if (fullscreenBackdrop != null)
            {
                fullscreenBackdrop.gameObject.SetActive(true);
            }

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
            SnowballFightEvents.EventHostButton.RemoveListener(OnEventHostButton);
            SnowballFightEvents.EventJoinButton.RemoveListener(OnEventJoinButton);
        }


        private void OnEventHostButton()
        {
            exitState = nextStateHost;
        }

        private void OnEventJoinButton()
        {
            exitState = nextStateJoin;
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
