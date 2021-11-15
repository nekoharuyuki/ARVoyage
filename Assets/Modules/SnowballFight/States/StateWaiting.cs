using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Host and joiners both use this state, after either StateSessionCreated or StateLocalizationComplete.
    /// Display a GUI with a dynamic list of current players (both host and joiners).
    /// GUI has a Start button to enter the game.
    /// If Host, and the only player in the player list, next state is StateSoloConfirm - 
    ///  needed in case host doesn't wait for joiners to join.
    /// Otherwise, next state is StateCountdown.
    /// </summary>
    public class StateWaiting : MonoBehaviour
    {
        private const float TimeFromAllPlayersReadyToStartButtonActive = 2f;

        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        [SerializeField] private GameObject soloConfirmState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text sessionId;
        [SerializeField] private PlayerListUI playerList;
        [SerializeField] private GameObject hostHintText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMPro.TMP_Text startGameButtonText;

        [SerializeField] private string sessionIdTextStrFormat = "Join Code: {0}";
        [SerializeField] private string hostStartButtonTextStr = "Start Game";
        [SerializeField] private string joinStartButtonTextStr = "Waiting for Host...";

        // Fade variables
        private Fader fader;
        private float initialDelay = 0f;

        private ARNetworkingHelper arNetworkingHelper;
        private SnowballFightManager snowballFightManager;
        private PlayerManager playerManager;

        private bool nonHostInEditor = false;

        private float timeAllPlayersBecameReady = -1f;

        void Awake()
        {
            // This is not the first state, start off disabled
            gameObject.SetActive(false);

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();
            playerManager = SceneLookup.Get<PlayerManager>();

            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventStartButton.AddListener(OnEventStartButton);
            SnowballFightEvents.EventGameStart.AddListener(OnEventGameStart);

            // Set this player ready so they show up properly in the list
            // Make sure to call this AFTER subscribing to EventAreAllPlayersReadyChanged
            playerManager.SetPlayerReady(true);

            sessionId.text = string.Format(sessionIdTextStrFormat, arNetworkingHelper.SessionId);
            startGameButtonText.text = arNetworkingHelper.IsHost ? hostStartButtonTextStr : joinStartButtonTextStr;

            // for development in editor
            nonHostInEditor = !arNetworkingHelper.IsHost && Application.isEditor;

            // show hint to host
            hostHintText.SetActive(arNetworkingHelper.IsHost);

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

            // Update player list
            playerList.Refresh(snowballFightManager.Players.Values);

            // If this a fresh detection that all players are ready, update the time, otherwise reset it
            if (snowballFightManager.AllPlayersReady)
            {
                if (Mathf.Approximately(timeAllPlayersBecameReady, -1))
                {
                    timeAllPlayersBecameReady = Time.time;
                }
            }
            else
            {
                timeAllPlayersBecameReady = -1;
            }

            startGameButton.interactable =
                            (arNetworkingHelper.IsHost ||
                                // if non-host in editor, bypass "Waiting for Host..."
                                nonHostInEditor) &&
                            // All players are ready and they've been ready for long enough to allow the host to start
                            // this ensures every player has time to look at the player list
                            (snowballFightManager.AllPlayersReady &&
                                Time.time - timeAllPlayersBecameReady >= TimeFromAllPlayersReadyToStartButtonActive);
        }

        void OnDisable()
        {
            // Unsubscribe from events
            SnowballFightEvents.EventStartButton.RemoveListener(OnEventStartButton);
            SnowballFightEvents.EventGameStart.RemoveListener(OnEventGameStart);
        }


        private void OnEventStartButton()
        {
            // Sanity-check: only the host can start the game
            if (!arNetworkingHelper.IsHost)
            {
                return;
            }

            // If multiple players, START GAME
            if (snowballFightManager.Players.Count > 1)
            {
                // this will trigger OnEventGameStart below
                snowballFightManager.StartGame();
            }

            // else if one player, go to confirmation state
            else
            {
                exitState = soloConfirmState;
            }
        }

        private void OnEventGameStart()
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

            // Activate the next state
            nextState.SetActive(true);

            // Deactivate this state
            thisState.SetActive(false);
        }
    }
}
