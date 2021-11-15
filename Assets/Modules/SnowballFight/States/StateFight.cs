using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// The main game State in SnowballFight, in which the player throws snowballs
    /// at enemy fireflies and other players. 
    ///
    /// (N.B.: other classes handle the networking for players and objects, e.g. SnowballFightManager,
    ///  PlayerManager, EnemyManager, NetworkedSnowballMaker, NetworkedSnowball.)
    ///
    /// This state begins by kicking off enemy spawning (host-only, for all players),
    /// and setting this player's SnowballMaker to active, which will display the 
    /// snowball toss UI button that holds/tosses 3D snowballs.
    /// The state waits until gameTimeAndScoreGUI's timer is done.
    /// Its next state (set via inspector) is StateGameOver.
    /// </summary>
    public class StateFight : MonoBehaviour
    {
        public static AppEvent Entered = new AppEvent();
        public static AppEvent Exited = new AppEvent();

        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private NetworkedSnowballMaker snowballMaker;
        [SerializeField] private GameObject hintBanner;
        [SerializeField] private TMPro.TMP_Text hintBannerText;
        [SerializeField] private GameTimeAndScore gameTimeAndScoreGUI;

        [SerializeField] private string scoreHintText = "Knock out the Fireflies to score!";
        private float nextHintPollTime = 0.5f;

        private ARNetworkingHelper arNetworkingHelper;
        private SnowballFightManager snowballFightManager;
        private PlayerManager playerManager;
        private AudioManager audioManager;
        private FrostOverlayEffect frostOverlayEffect;
        private EnemyManager enemyManager;
        private Fader fader;

        void Awake()
        {
            if (DevSettings.SkipToSnowballFightMainInEditor)
            {
                gameObject.SetActive(true);
            }
            else
            {
                // This is the not the first state, start off disabled
                gameObject.SetActive(false);
            }

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();
            playerManager = SceneLookup.Get<PlayerManager>();
            audioManager = SceneLookup.Get<AudioManager>();
            frostOverlayEffect = SceneLookup.Get<FrostOverlayEffect>();
            enemyManager = SceneLookup.Get<EnemyManager>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            if (DevSettings.SkipToSnowballFightMainInEditor)
            {
                StartCoroutine(SetupMockStateAndThenEnterRoutine());
            }
            else
            {
                EnterState();
            }
        }

        private void EnterState()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventLocalPlayerScoreChanged.AddListener(OnEventLocalPlayerScoreChanged);
            SnowballFightEvents.EventSnowballTossButton.AddListener(OnEventSnowballTossButton);
            SnowballFightEvents.EventLocalPlayerHit.AddListener(OnEventLocalPlayerHit);

            // Hide hint banner at first
            hintBanner.SetActive(false);

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader));

            snowballFightManager.InitializeRemotePlayersHUD();
            InitializeLocalPlayerHUD();

            // Start enemy spawning.
            // N.B.: Only the Host will do any spawning
            enemyManager.StartSpawning();

            running = true;

            Entered.Invoke();
        }

        // For development in editor
        private IEnumerator SetupMockStateAndThenEnterRoutine()
        {
            yield return null;

            arNetworkingHelper.InitAndHost();

            yield return new WaitUntil(() => arNetworkingHelper.IsLocalized && playerManager.IsPlayerNamed);

            playerManager.SetPlayerReady(true);

            snowballFightManager.StartGame();

            EnterState();
        }


        void Update()
        {
            if (!running) return;

            // Show/hide initial hint
            if (Time.time > nextHintPollTime)
            {
                nextHintPollTime = Time.time + 0.5f;

                if (playerManager.player.Score == 0 && !hintBanner.activeSelf)
                {
                    hintBannerText.text = scoreHintText;
                    hintBanner.SetActive(true);
                }

                if (playerManager.player.Score > 0 && hintBanner.activeSelf)
                {
                    hintBanner.SetActive(false);
                }
            }

            // Check for exit condition
            if (snowballFightManager.IsGameOver)
            {
                exitState = nextState;
                Exit(exitState);
                return;
            }

            if (gameTimeAndScoreGUI.done)
            {
                if (arNetworkingHelper.IsHost)
                {
                    snowballFightManager.EndGame();
                }
            }
        }

        void OnDisable()
        {
            // Unsubscribe from events
            SnowballFightEvents.EventLocalPlayerScoreChanged.RemoveListener(OnEventLocalPlayerScoreChanged);
            SnowballFightEvents.EventSnowballTossButton.RemoveListener(OnEventSnowballTossButton);
        }


        private void OnEventSnowballTossButton()
        {
            snowballMaker.TossSnowball();
        }

        private void OnEventLocalPlayerScoreChanged(int score)
        {
            gameTimeAndScoreGUI.SetScore(score);
        }

        private void OnEventLocalPlayerHit()
        {
            frostOverlayEffect.PulseFrost();
        }

        private void InitializeLocalPlayerHUD()
        {
            gameTimeAndScoreGUI.Init(SnowballFightManager.gameDuration, SnowballFightManager.nearGameEndDuration);
            snowballMaker.gameObject.SetActive(true);
        }

        private void Exit(GameObject nextState)
        {
            // Stop enemy spawning and expire all enemies.
            enemyManager.StopAndExpireAll();

            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            snowballMaker.Expire();
            snowballMaker.gameObject.SetActive(false);
            hintBanner.SetActive(false);

            // Fadeout GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);

            Exited.Invoke();
        }
    }
}
