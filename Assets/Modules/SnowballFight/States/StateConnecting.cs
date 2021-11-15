using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Host and joiners both use this state, after their Instructions state.
    /// For either, it succeeds when ARNetworkingHelper acknowledges a server connection to the new multiplayer session.
    /// Otherwise it calls the ErrorManager to display an error GUI, and option to restart the demo. Errors include:
    ///  Connection Timeout (server not responding)
    ///  Connection Rejected (for joiners, e.g. they entered the wrong join code)
    ///  Session Full (for joiners - there is a limit to number of players allowed in the game)
    ///  Host Left Session (for joiners)
    /// Next state (set via inspector) is StateLocalizing.
    /// </summary>
    public class StateConnecting : MonoBehaviour
    {
        private const string connectionRejectedError = "The Join Code you entered is not registered to a valid Session.\n\nPlease restart and try again!";
        private const string connectionSessionFullError = "This Session is full!\n\nPlease restart and Host a new Session, or Join again once a spot becomes available.";
        private const string connectionTimeoutError = "Your connection has timed out.\n\nPlease restart and try again.";
        private const string connectionHostLeftError = "The Host has left the Session.\n\nPlease restart and Host a new Session, or Join an already created one.";

        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        private float minStateDuration = DemoUtil.minUIDisplayDuration;
        private float timeoutDuration = 7f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;

        private ARNetworkingHelper arNetworkingHelper;
        private ErrorManager errorManager;

        void Awake()
        {
            // This is the not first state, start off disabled
            gameObject.SetActive(false);

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            errorManager = SceneLookup.Get<ErrorManager>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // Subscribe to events
            SnowballFightEvents.EventConnectionFailed.AddListener(OnEventConnectionFailed);

            gui.SetActive(true);

            running = true;
        }

        void Update()
        {
            if (!running) return;

            // Check for exit condition
            if (arNetworkingHelper.IsConnected && Time.time - timeStartedState >= minStateDuration)
            {
                // If the user tried to join an existing session but became a host, they have entered an incorrect session code.
                // This case must be handled before checking the acknowledge state, since a host will self-acknowledge
                if (arNetworkingHelper.Joined && arNetworkingHelper.IsHost)
                {
                    errorManager.DisplayErrorGUI(connectionRejectedError);
                    running = false;
                }

                // The host rejects players when the session is full
                else if (arNetworkingHelper.HostAcknowledgeState == ARNetworkingHelper.AcknowledgeMessageState.Rejected)
                {
                    errorManager.DisplayErrorGUI(connectionSessionFullError);
                    running = false;
                }

                else if (arNetworkingHelper.HostAcknowledgeState == ARNetworkingHelper.AcknowledgeMessageState.Acknowledged)
                {
                    Debug.Log("StateConnecting.Update: Connection acknowledged by host.");
                    exitState = nextState;
                }
            }

            // Check for timeout condition
            if (exitState == null && Time.time - timeStartedState >= timeoutDuration)
            {
                errorManager.DisplayErrorGUI(connectionTimeoutError);
                running = false;
            }

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }
        }

        void OnDisable()
        {
            // Unsubscribe from events
            SnowballFightEvents.EventConnectionFailed.RemoveListener(OnEventConnectionFailed);
        }


        private void OnEventConnectionFailed(uint errorCode)
        {
            SnowballFightEvents.EventConnectionFailed.RemoveListener(OnEventConnectionFailed);

            // display error GUI, disable this state
            errorManager.DisplayErrorGUI(connectionTimeoutError + "\nError code: " + errorCode);
            running = false;
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
