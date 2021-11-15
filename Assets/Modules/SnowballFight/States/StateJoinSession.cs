using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Joiner-only state presenting GUI asking player to enter the 4-letter join code 
    /// for a new multiplayer SnowballFight hosted by another player.
    /// Next state (set via inspector) is either StateInstructions, or return to StateHostOrJoin if back button is pressed.
    /// </summary>
    public class StateJoinSession : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        [SerializeField] private GameObject prevState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_InputField sessionCodeInputField;
        [SerializeField] private Button joinButton;

        // Fade variables
        private Fader fader;
        private float initialDelay = 0f;

        private ARNetworkingHelper arNetworkingHelper;

        private string sessionCode = "";

        private bool initAndJoinAfterGuiFadeOut = false;

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
            SnowballFightEvents.EventSessionJoinCodeInputChanged.AddListener(OnEventSessionJoinCodeInputChanged);
            SnowballFightEvents.EventNextButton.AddListener(OnEventNextButton);
            SnowballFightEvents.EventBackButton.AddListener(OnEventBackButton);

            sessionCode = arNetworkingHelper.SessionId;
            sessionCodeInputField.text = sessionCode;

            joinButton.interactable = sessionCode.Length == arNetworkingHelper.sessionIdLength
                // if in editor, bypass checking the sessionCode
                || Application.isEditor;

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
            SnowballFightEvents.EventSessionJoinCodeInputChanged.RemoveListener(OnEventSessionJoinCodeInputChanged);
            SnowballFightEvents.EventNextButton.RemoveListener(OnEventNextButton);
            SnowballFightEvents.EventBackButton.RemoveListener(OnEventBackButton);
        }

        private void OnEventSessionJoinCodeInputChanged(string sessionCode)
        {
            this.sessionCode = sessionCode;
            joinButton.interactable = sessionCode.Length == arNetworkingHelper.sessionIdLength;
        }

        private void OnEventNextButton()
        {
            exitState = nextState;
            // If going to the next state, set the flag to trigger the networking session init and join once the GUI has faded out
            initAndJoinAfterGuiFadeOut = true;
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

            // Wait to call InitAndJoin until the GUI has faded out since it can cause a small hitch
            // This is always bypassed in editor
            if (initAndJoinAfterGuiFadeOut && !Application.isEditor)
            {
                arNetworkingHelper.InitAndJoin(sessionCode);
            }

            nextState.SetActive(true);
            thisState.SetActive(false);
        }
    }
}
