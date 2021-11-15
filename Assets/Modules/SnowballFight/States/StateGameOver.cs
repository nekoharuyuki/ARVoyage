using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// The final State in SnowballFight, displaying a leaderboard of final scores,
    /// and button options to either restart the demo, or return to Map.
    /// Its next state (set via inspector) is StateWaiting (if player chooses Restart), 
    ///  reusing this same session code; otherwise LevelSwitcher ReturnToMap() is called.
    /// </summary>
    public class StateGameOver : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private GameObject yetiVictoryImage;
        [SerializeField] private GameObject yetiLoseImage;
        [SerializeField] private TMPro.TMP_Text titleText;
        [SerializeField] private PlayerListUI playerList;

        [SerializeField] private string titleVictoryTextStr = "You Won!";
        [SerializeField] private string titleTiedTextStr = "You Tied!";
        [SerializeField] private string titleLostTextStr = "Nice Try!";

        private ARNetworkingHelper arNetworkingHelper;
        private SnowballFightManager snowballFightManager;
        private PlayerManager playerManager;
        private AudioManager audioManager;
        private FrostOverlayEffect frostOverlayEffect;

        // Fade variables
        private Fader fader;
        private float initialDelay = 0.75f;

        void Awake()
        {
            // This is not the first state, start off disabled
            gameObject.SetActive(false);

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();
            playerManager = SceneLookup.Get<PlayerManager>();
            audioManager = SceneLookup.Get<AudioManager>();

            frostOverlayEffect = SceneLookup.Get<FrostOverlayEffect>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventRestartButton.AddListener(OnEventRestartButton);

            // Save journey progress
            SaveUtil.SaveBadgeUnlocked(Level.SnowballFight);

            // Am I the winner?
            List<PlayerData> playersSortedByScore;
            bool victory;
            bool tied;
            snowballFightManager.TallyFinalScores(out playersSortedByScore, out victory, out tied);

            // Set GUI image and text
            yetiVictoryImage.gameObject.SetActive(victory || tied);
            yetiLoseImage.gameObject.SetActive(!(victory || tied));
            if (victory) titleText.text = titleVictoryTextStr;
            else if (tied) titleText.text = titleTiedTextStr;
            else titleText.text = titleLostTextStr;

            // Populate player list GUI
            playerList.Refresh(playersSortedByScore, true);

            // SFX
            audioManager.PlayAudioNonSpatial((victory || tied) ? AudioKeys.SFX_Winner_Fanfare : AudioKeys.SFX_Loser_Fanfare);

            // Reset game
            if (arNetworkingHelper.IsHost)
            {
                snowballFightManager.ResetGame();
            }
            playerManager.SetPlayerReady(false);

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
            SnowballFightEvents.EventRestartButton.RemoveListener(OnEventRestartButton);
        }


        private void OnEventRestartButton()
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
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

        public void OnEventBackToMapButton()
        {
            Debug.Log("BackToMapButton pressed");

            StartCoroutine(BackToMapRoutine());
        }

        private IEnumerator BackToMapRoutine()
        {
            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            // Return to map
            SceneLookup.Get<LevelSwitcher>().ReturnToMap();
        }
    }
}
