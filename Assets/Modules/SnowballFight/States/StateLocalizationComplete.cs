using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Host and joiners both use this state, after StateLocalizing.
    /// Display a short "localization success" message.
    /// 
    /// If the Host, next state (set via inspector) is StateSessionCreated, enabling joiners to advance past 
    ///  their StateJoinSession and onto StateConnecting and StateLocalizing.
    ///
    /// If a Joiner, next state (set via inspector)is StateWaiting, which will display current list of players (host and joiners).
    /// </summary>
    public class StateLocalizationComplete : MonoBehaviour
    {
        // Inspector references to relevant objects
        [Header("State Machine")]
        [SerializeField] private GameObject nextStateHost;
        [SerializeField] private GameObject nextStateJoin;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        private float minStateDuration = DemoUtil.minUIDisplayDuration;

        [Header("GUI")]
        [SerializeField] private GameObject gui;

        private ARNetworkingHelper arNetworkingHelper;

        void Awake()
        {
            // This is the not first state, start off disabled
            gameObject.SetActive(false);

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            gui.SetActive(true);

            running = true;
        }

        void Update()
        {
            if (!running) return;

            // Check for exit condition
            if (Time.time - timeStartedState >= minStateDuration)
            {
                if (arNetworkingHelper.IsHost)
                {
                    exitState = nextStateHost;
                }
                else
                {
                    exitState = nextStateJoin;
                }
            }

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }
        }

        private void Exit(GameObject nextState)
        {
            running = false;

            gui.SetActive(false);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }
    }
}
