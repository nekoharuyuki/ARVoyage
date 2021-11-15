using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Inherits from StateInstructions, customized to call ARNetworkingHelper InitAndHost().
    /// If the AR Warning GUI has never been shown, its next state (set via inspector) is StateWarning.
    /// Otherwise next state (set via inspector) is StateConnecting, 
    /// or return to StateHostOrJoin if back button is pressed.
    /// </summary>
    public class StateHostInstructions : StateInstructions
    {
        [SerializeField] private GameObject prevState;

        protected override void OnEnable()
        {
            // Subscribe to events
            SnowballFightEvents.EventBackButton.AddListener(OnEventBackButton);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            // Unsubscribe from events
            SnowballFightEvents.EventBackButton.RemoveListener(OnEventBackButton);
            GuiFadedOutDuringExit.RemoveListener(OnGuiFadedOut);
            base.OnDisable();
        }

        protected override void OnEventStartButton()
        {
            // Start listening for the GUI to fade out on exit to the next state
            GuiFadedOutDuringExit.AddListener(OnGuiFadedOut);
            base.OnEventStartButton();
        }

        private void OnGuiFadedOut()
        {
            // Start AR Networking initialization once the GUI has faded out, since it can
            // cause a small hitch
            SceneLookup.Get<ARNetworkingHelper>().InitAndHost();
        }

        private void OnEventBackButton()
        {
            exitState = prevState;
        }
    }
}
