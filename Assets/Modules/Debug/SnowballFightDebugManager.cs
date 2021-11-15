using Niantic.ARDK.AR.Awareness.Depth.Effects;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Extensions.Depth;
using Niantic.ARDK.Rendering;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Manages the debug functionality for the SnowballFight scene. Only available during StateFight.
    /// Feature Point Visualizer: turn the AR feature point visualization on/off
    /// Player Indicators: show colliders for other peers
    /// Occlusion: turn ARDK depth occlusion effect on/off
    /// Visualize Raw depth: show the ARDK depth buffer visualization
    /// Visualize Disparity Overlay: show the ARDK disparity overlay
    /// </summary>
    public class SnowballFightDebugManager : MonoBehaviour
    {
        // Used to cleanly map features to menu checkbox indices
        private enum CheckboxIndex
        {
            VisualizeFeaturePoints = 0,
            VisualizePlayerIndicators,
            OcclusionEnabled,
            VisualizeOcclusions,
        }
        
        // SDK
        ARDepthManager arDepthManager;
        ARCameraRenderingHelper arCameraRenderingHelper;

        // Demo
        SnowballFightManager snowballFightManager;
        FeaturePointHelper featurePointHelper;
        
        private bool visualizeOcclusions = false;
        public bool VisualizeOcclusions
        {
            get
            {
                return visualizeOcclusions;
            }
            set
            {
                visualizeOcclusions = value;
                arDepthManager.ToggleDebugVisualization(visualizeOcclusions);
            }
        }

        public bool DepthOcclusionEnabled
        {
            get
            {
                return arDepthManager.OcclusionTechnique != ARDepthManager.OcclusionMode.None;
            }
            set
            {
                if (value)
                    arDepthManager.OcclusionTechnique = ARDepthManager.OcclusionMode.Auto;
                else
                    arDepthManager.OcclusionTechnique = ARDepthManager.OcclusionMode.None;
            }
        }

        public bool FeaturePointsVisible
        {
            get
            {
                return featurePointHelper.Tracking &&
                       featurePointHelper.Spawning;
            }
            set
            {
                featurePointHelper.Tracking = value;
                featurePointHelper.Spawning = value;
            }
        }

        public bool PlayerCollidersVisible
        {
            get
            {
                return snowballFightManager.PlayerCollidersVisible;
            }
            set
            {
                snowballFightManager.PlayerCollidersVisible = value;
            }
        }

        // cached default values
        private bool featurePointsVisibleDefaultValue;
        private bool playerCollidersVisibleDefaultValue;
        private bool depthOcclusionEnabledDefaultValue;
        private bool visualizeOcclusionsDefaultValue;
        private bool visualizeRawDepthDefaultValue;
        private bool visualizeDisparityOverlayDefaultValue;

        [SerializeField] DebugMenuGUI debugMenuGUI;
        [SerializeField] DebugMenuButton debugMenuButton;


        void Start()
        {
            // SDK
            arDepthManager = FindObjectOfType<ARDepthManager>();

            // Demo
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();
            featurePointHelper = SceneLookup.Get<FeaturePointHelper>();

            featurePointsVisibleDefaultValue = FeaturePointsVisible;
            playerCollidersVisibleDefaultValue = PlayerCollidersVisible;
            depthOcclusionEnabledDefaultValue = DepthOcclusionEnabled;
            visualizeOcclusionsDefaultValue = VisualizeOcclusions;
        }

        void OnEnable()
        {
            // Subscribe to events
            DebugMenuGUI.EventDebugOption1Checkbox.AddListener(OnEventDebugOption1Checkbox);    // feature points
            DebugMenuGUI.EventDebugOption2Checkbox.AddListener(OnEventDebugOption2Checkbox);    // player indicators
            DebugMenuGUI.EventDebugOption3Checkbox.AddListener(OnEventDebugOption3Checkbox);    // occlusion
            DebugMenuGUI.EventDebugOption4Checkbox.AddListener(OnEventDebugOption4Checkbox);    // visualize occlusion

            StateFight.Entered.AddListener(OnStateFightEntered);
            StateFight.Exited.AddListener(OnStateFightExited);
        }

        void OnDisable()
        {
            // Unsubscribe from events
            DebugMenuGUI.EventDebugOption1Checkbox.RemoveListener(OnEventDebugOption1Checkbox);
            DebugMenuGUI.EventDebugOption2Checkbox.RemoveListener(OnEventDebugOption2Checkbox);
            DebugMenuGUI.EventDebugOption3Checkbox.RemoveListener(OnEventDebugOption3Checkbox);
            DebugMenuGUI.EventDebugOption4Checkbox.RemoveListener(OnEventDebugOption4Checkbox);

            StateFight.Entered.RemoveListener(OnStateFightEntered);
            StateFight.Exited.RemoveListener(OnStateFightExited);
        }

        private void ResetToDefaultValues()
        {
            // Set properties to default values
            FeaturePointsVisible = featurePointsVisibleDefaultValue;
            PlayerCollidersVisible = playerCollidersVisibleDefaultValue;
            DepthOcclusionEnabled = depthOcclusionEnabledDefaultValue;
            VisualizeOcclusions = visualizeOcclusionsDefaultValue;

            // Set checkboxes to values
            debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizeFeaturePoints, FeaturePointsVisible);
            debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizePlayerIndicators, PlayerCollidersVisible);
            debugMenuGUI.SetChecked((int)CheckboxIndex.OcclusionEnabled, DepthOcclusionEnabled);
            debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizeOcclusions, VisualizeOcclusions);
        }

        private void OnStateFightEntered()
        {
            // When entering the fight state, begin with the debug menu hidden and show the button, and reset to default values
            debugMenuButton.ShowDebugMenuGUI(false);
            debugMenuButton.gameObject.SetActive(true);
            ResetToDefaultValues();
        }

        private void OnStateFightExited()
        {
            // When exiting the fight state, hide the debug menu and button, and reset to default values
            debugMenuButton.ShowDebugMenuGUI(false);
            debugMenuButton.gameObject.SetActive(false);
            ResetToDefaultValues();
        }

        // feature points
        private void OnEventDebugOption1Checkbox()
        {
            Debug.Log("OnEventDebugOption1Checkbox");
            FeaturePointsVisible = !FeaturePointsVisible;
        }

        // player indicators
        private void OnEventDebugOption2Checkbox()
        {
            Debug.Log("OnEventDebugOption2Checkbox");
            PlayerCollidersVisible = !PlayerCollidersVisible;
        }


        // occlusion
        private void OnEventDebugOption3Checkbox()
        {
            Debug.Log("OnEventDebugOption3Checkbox");
            DepthOcclusionEnabled = !DepthOcclusionEnabled;
            
            // If occlusion is now off, be sure to set the visualizer off
            if (!DepthOcclusionEnabled && VisualizeOcclusions)
            {
                VisualizeOcclusions = false;
                debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizeOcclusions, false);
            }
        }


        // raw depth
        private void OnEventDebugOption4Checkbox()
        {
            Debug.Log("OnEventDebugOption4Checkbox");
            VisualizeOcclusions = !VisualizeOcclusions;

            // If we're visualizing occlusions, be sure occlusions are on
            if (VisualizeOcclusions && !DepthOcclusionEnabled)
            {
                DepthOcclusionEnabled = true;
                debugMenuGUI.SetChecked((int)CheckboxIndex.OcclusionEnabled, true);
            }
        }
    }
}
