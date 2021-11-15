using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Host-only state that occurs after the host completes localization.
    /// Display a 4-letter join code, that all joiners will use in their StateJoinSession.
    /// Next state (set via inspector) is StateWaiting, which will display current list of players (host and joiners).
    /// </summary>
    public class StateSessionCreated : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text sessionCodeText;

        // Fade variables
        private Fader fader;
        private float initialDelay = 0f;

        private ARNetworkingHelper arNetworkingHelper;

        void Awake()
        {
            // This is not the first state, start off disabled
            gameObject.SetActive(false);

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();

            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventNextButton.AddListener(OnEventNextButton);

            sessionCodeText.text = arNetworkingHelper.SessionId;

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
            SnowballFightEvents.EventNextButton.RemoveListener(OnEventNextButton);
        }


        private void OnEventNextButton()
        {
            exitState = nextState;
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

            nextState.SetActive(true);
            thisState.SetActive(false);
        }
    }
}
