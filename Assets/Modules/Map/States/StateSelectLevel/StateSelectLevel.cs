using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// State in Map that controls player level selection
    /// </summary>
    public class StateSelectLevel : MonoBehaviour
    {
        public static AppEvent<bool> SetMapWaypointsClickable = new AppEvent<bool>();

        // Inspector references to relevant objects
        [Header("State Machine")]
        [SerializeField] private bool isStartState = false;

        [Header("GUI")]
        [SerializeField] private GameObject levelWalkaboutGUI;
        [SerializeField] private GameObject levelSnowballTossGUI;
        [SerializeField] private GameObject levelSnowballFightGUI;
        [SerializeField] private GameObject levelBuildAShipGUI;
        private GameObject activeLevelGUI;

        [Header("World Space")]
        [SerializeField] private GameObject mapDotHintBubble;
        private bool needMapDotHint = false;
        private const float delayTillShowMapDotHintBubble = 2f;

        private MapActor yetiActor;

        // Fade variables
        private Fader fader;

        // Every state has a running bool that's true from OnEnable to Exit
        private bool running;

        private Level exitLevel = Level.None;

        private BadgeManager badgeManager;
        private Level chosenLevel = Level.None;
        private float timeChosenLevel = 0f;


        void Awake()
        {
            gameObject.SetActive(isStartState);

            badgeManager = SceneLookup.Get<BadgeManager>();
            fader = SceneLookup.Get<Fader>();
            yetiActor = SceneLookup.Get<MapActor>();
        }

        void OnEnable()
        {
            // Subscribe to events
            MapWaypoint.MapWaypointClicked.AddListener(OnMapWaypointClicked);
            MapEvents.EventLevelGoButton.AddListener(OnEventGoButton);
            MapEvents.EventLevelXCloseButton.AddListener(OnEventLevelXCloseButton);
            // If a badge button is pressed, be sure to close any level GUI that may be open
            MapEvents.EventBadge1Button.AddListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge2Button.AddListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge3Button.AddListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge4Button.AddListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge5Button.AddListener(OnEventLevelXCloseButton);

            // show achieved badges
            badgeManager.DisplayBadgeRowButtons(true);

            // Make map dots clickable upon entering this state
            SetMapWaypointsClickable.Invoke(true);

            // Spawn map dot button hint bubble
            needMapDotHint = !SaveUtil.IsMapDotHintBubbleCompleted();
            StartCoroutine(MapDotHintBubbleRoutine());

            running = true;
        }

        void OnDisable()
        {
            // Unsubscribe from events
            MapWaypoint.MapWaypointClicked.RemoveListener(OnMapWaypointClicked);
            MapEvents.EventLevelGoButton.RemoveListener(OnEventGoButton);
            MapEvents.EventLevelXCloseButton.RemoveListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge1Button.RemoveListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge2Button.RemoveListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge3Button.RemoveListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge4Button.RemoveListener(OnEventLevelXCloseButton);
            MapEvents.EventBadge5Button.RemoveListener(OnEventLevelXCloseButton);
        }

        void Update()
        {
            // Only process update if running
            if (running)
            {
                // Check for state exit
                if (exitLevel != Level.None)
                {
                    Exit();
                }
            }
        }

        private void OnMapWaypointClicked(MapWaypoint waypoint)
        {
            Debug.Log("OnMapWaypointClicked " + waypoint.name);

            // Start the yeti walking to the level
            chosenLevel = waypoint.level;
            yetiActor.WalkToLevel(chosenLevel);

            // Toggle the matching GUI
            ShowLevelGUI(levelWalkaboutGUI, waypoint.level == Level.Walkabout, immediateHide: true);
            ShowLevelGUI(levelSnowballTossGUI, waypoint.level == Level.SnowballToss, immediateHide: true);
            ShowLevelGUI(levelSnowballFightGUI, waypoint.level == Level.SnowballFight, immediateHide: true);
            ShowLevelGUI(levelBuildAShipGUI, waypoint.level == Level.BuildAShip, immediateHide: true);
            timeChosenLevel = Time.time;

            // Don't need map dot hint anymore
            if (needMapDotHint)
            {
                needMapDotHint = false;
                SaveUtil.SaveMapDotHintBubbleCompleted();
            }
        }

        private void ShowLevelGUI(GameObject gui, bool show, bool immediateHide = false)
        {
            if (show)
            {
                StartCoroutine(DemoUtil.FadeInGUI(gui, fader));
                activeLevelGUI = gui;
            }
            else if (gui.activeSelf)
            {
                if (immediateHide)
                {
                    gui.SetActive(false);
                }
                else
                {
                    StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));
                }
                activeLevelGUI = null;
            }
        }

        private IEnumerator MapDotHintBubbleRoutine()
        {
            // initially hidden
            mapDotHintBubble.gameObject.SetActive(false);

            float waitTill = Time.time + delayTillShowMapDotHintBubble;
            while (Time.time < waitTill) yield return null;

            // Bail if hint unneeded
            if (!needMapDotHint) yield break;

            // Animate it up
            //mapDotHintBubble.gameObject.SetActive(true);
            mapDotHintBubble.gameObject.transform.localScale = Vector3.zero;
            DemoUtil.DisplayWithBubbleScale(mapDotHintBubble, show: true);

            // Wait until hint unneeded
            while (needMapDotHint) yield return null;

            // Hide bubble
            if (levelSnowballFightGUI.gameObject.activeSelf ||
                levelBuildAShipGUI.gameObject.activeSelf)
            {
                mapDotHintBubble.gameObject.SetActive(false);
            }
            else
            {
                DemoUtil.DisplayWithBubbleScale(mapDotHintBubble, show: false);
            }
        }

        private void OnEventGoButton()
        {
            exitLevel = chosenLevel;

            if (activeLevelGUI != null)
            {
                StartCoroutine(DemoUtil.FadeOutGUI(activeLevelGUI, fader));
            }

            // Make map dots non-clickable once an exit level is selected
            SetMapWaypointsClickable.Invoke(false);

            // Immediately unsubscribe to prevent listening for clicks during exit
            OnDisable();
        }

        private void OnEventLevelXCloseButton()
        {
            ShowLevelGUI(levelWalkaboutGUI, false);
            ShowLevelGUI(levelSnowballTossGUI, false);
            ShowLevelGUI(levelSnowballFightGUI, false);
            ShowLevelGUI(levelBuildAShipGUI, false);
        }


        private void Exit()
        {
            running = false;

            StartCoroutine(ExitRoutine());
        }

        private IEnumerator ExitRoutine()
        {
            yield return null;

            // Save the level
            SaveUtil.SaveLastLevelPlayed(exitLevel);

            // Go to the exit level
            SceneLookup.Get<LevelSwitcher>().LoadLevel(exitLevel, fadeOutBeforeLoad: true);
        }
    }
}
