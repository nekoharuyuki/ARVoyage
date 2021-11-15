using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Host and joiners both use this state, after StateConnecting.
    /// Joiners have to wait for the Host to complete this state before this state runs for them.
    /// 
    /// If the Host, prompted by animated instructions, the Host has to find a visual landmark 
    /// to localize the session onto - a shared physical anchor.
    /// 
    /// If a Joiner, prompted by animated instructions, the Joiner has to find the same visual landmark
    /// the host localized onto.
    /// 
    /// Next state is (set via inspector) StateLocalizationComplete.
    /// </summary>
    public class StateLocalizing : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        private float minStateDuration = DemoUtil.minUIDisplayDuration;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text hintBannerText;
        [SerializeField] private string[] LocalizingHostHintText = { "Find a visual landmark!", "Move your device towards it!", "Scan the landmark side to side!" };
        [SerializeField] private string[] LocalizingJoinHintText = { "Find the same landmark as the Host!", "Move your device towards it!", "Scan the landmark side to side!" };
        private int hintCtr = 0;
        private const float hintDuration = 4f;
        private float timeDisplayedHint = 0f;

        private ARNetworkingHelper arNetworkingHelper;
        private PlayerManager playerManager;
        private FeaturePointHelper featurePointHelper;

        void Awake()
        {
            // This is the not first state, start off disabled
            gameObject.SetActive(false);

            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            playerManager = SceneLookup.Get<PlayerManager>();
            featurePointHelper = SceneLookup.Get<FeaturePointHelper>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            gui.SetActive(true);
            hintCtr = 0;
            hintBannerText.text = arNetworkingHelper.IsHost ?
                LocalizingHostHintText[hintCtr] :
                LocalizingJoinHintText[hintCtr];
            timeDisplayedHint = Time.time;

            featurePointHelper.Spawning = true;
            featurePointHelper.Tracking = true;

            running = true;
        }

        void Update()
        {
            if (!running) return;

            // Update cycling hint banner
            if (Time.time - timeDisplayedHint > hintDuration)
            {
                ++hintCtr;
                hintCtr %= arNetworkingHelper.IsHost ?
                    LocalizingHostHintText.Length :
                    LocalizingJoinHintText.Length;
                hintBannerText.text = arNetworkingHelper.IsHost ?
                    LocalizingHostHintText[hintCtr] :
                    LocalizingJoinHintText[hintCtr];
                timeDisplayedHint = Time.time;
            }

            // Check for exit condition
            if (((arNetworkingHelper.IsLocalized && playerManager.IsPlayerNamed)
                    // if in editor, bypass checking IsLocalized 
                    ) &&
                Time.time - timeStartedState >= minStateDuration)
            {
                exitState = nextState;
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

            featurePointHelper.Spawning = false;
            featurePointHelper.Tracking = false;

            nextState.SetActive(true);
            thisState.SetActive(false);
        }
    }
}
