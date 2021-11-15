using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// State in Map that displays the Captain Doty intro animation if it has never been played.
    /// </summary>
    public class StateYetiIntro : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private bool isStartState = true;
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;

        private BadgeManager badgeManager;
        private MapActor yetiActor;
        private Fader fader;

        void Awake()
        {
            gameObject.SetActive(isStartState);

            badgeManager = SceneLookup.Get<BadgeManager>();
            fader = SceneLookup.Get<Fader>();
            yetiActor = SceneLookup.Get<MapActor>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // show achieved badges
            badgeManager.DisplayBadgeRowButtons(true);

            running = true;
        }

        private IEnumerator Start()
        {
            // Ensure the scene has faded in
            yield return new WaitUntil(() => fader.IsSceneFadedIn);

            // If the map intro has ever completed, start the yeti in the right position
            if (SaveUtil.HasMapIntroEverCompleted())
            {
                // Jump the yeti to the last level played if there is one
                Level lastLevelPlayed = SaveUtil.GetLastLevelPlayed();
                if (lastLevelPlayed != Level.None)
                {
                    yetiActor.JumpToLevel(lastLevelPlayed);
                }

                // scale the yeti up
                yetiActor.BubbleScaleUp();

                // play a poof one frame delayed to allow any jump to finish
                yetiActor.PlayPoof(1);

                exitState = nextState;
            }

            // Otherwise play the intro, then exit
            else
            {
                yetiActor.PlayIntro(() =>
                {
                    exitState = nextState;
                });
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

        void OnDisable()
        {
            // Unsubscribe from events
        }

        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Go to the next state
            nextState.SetActive(true);

            // Deactivate this state
            gameObject.SetActive(false);

            yield break;
        }
    }
}
