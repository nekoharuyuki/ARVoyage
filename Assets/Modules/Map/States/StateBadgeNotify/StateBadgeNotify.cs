using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// State in Map that controls display and behavior of player's unlocked badges
    /// </summary>
    public class StateBadgeNotify : MonoBehaviour
    {

        [Header("State Machine")]
        [SerializeField] private bool isStartState = true;
        [SerializeField] private GameObject nextState;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        private CanvasGroup guiCanvasGroup;

        private GameObject exitState;

        private bool running;

        // Fade variables
        private Fader fader;

        private BadgeManager badgeManager;
        Level badgeLevel = Level.None;


        void Awake()
        {
            gameObject.SetActive(isStartState);

            badgeManager = SceneLookup.Get<BadgeManager>();
            fader = SceneLookup.Get<Fader>();
        }


        void OnEnable()
        {
            // determine what badge to present
            badgeLevel = badgeManager.GetNextBadgeToPresent();

            // Bail if no badge to display
            if (badgeLevel == Level.None)
            {
                exitState = nextState;
                running = true;
                return;
            }

            // display the badge UI
            bool shown = badgeManager.DisplayBadgeAchievedGUI(badgeLevel, playSFX: true);

            // bail if badge to display not found
            if (!shown)
            {
                Debug.LogError("StateBadgeNotify invalid badge level " + badgeLevel);
                exitState = nextState;
                running = true;
                return;
            }

            // Subscribe to events
            MapEvents.EventBadgeOkButton.AddListener(OnEventOkButton);

            // Activate GUI
            gui.SetActive(true);
            guiCanvasGroup = gui.GetComponent<CanvasGroup>();
            guiCanvasGroup.alpha = 1f;

            running = true;
        }

        void Update()
        {
            // Only process update if running
            if (running)
            {
                // Check for state exit
                if (exitState != null)
                {
                    Exit(exitState);
                }
            }
        }

        void OnDisable()
        {
            // Unsubscribe from events
            MapEvents.EventBadgeOkButton.RemoveListener(OnEventOkButton);
        }


        private void OnEventOkButton()
        {
            // remember this badge was presented
            SaveUtil.SaveBadgeNotificationPresented(badgeLevel);

            // Update display of achieved badges, animating the new badgeLevel
            badgeManager.DisplayBadgeRowButtons(true, badgeLevel);

            // Display another badge if needed, or go to next state
            ChooseNextState();
        }


        private void ChooseNextState()
        {
            // Check if the airship badge should now be unlocked
            badgeManager.CheckForAirshipBadgeUnlock();

            // if there is another badge notification to do, 
            badgeLevel = badgeManager.GetNextBadgeToPresent();
            bool displayAnotherBadge = badgeLevel != Level.None;

            // then display it, and remain in this state
            if (displayAnotherBadge)
            {
                displayAnotherBadge = badgeManager.DisplayBadgeAchievedGUI(badgeLevel, playSFX: true);
            }

            // otherwise go to the next state
            else
            {
                exitState = nextState;
            }
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }


        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Fade out GUI if needed
            if (gui.activeInHierarchy)
            {
                yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));
            }

            // Allow players to click the badge buttons to re-display the notification once done displaying new notifications
            badgeManager.SetBadgeButtonsClickable(true);

            // Activate the next state
            nextState.SetActive(true);

            // Deactivate this state
            gameObject.SetActive(false);
        }
    }
}
