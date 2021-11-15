using Niantic.ARDK.AR.Awareness.Depth.Effects;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Extensions.Depth;
using Niantic.ARDK.Rendering;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// Manages the debug functionality for the BuildAShip scene
    /// Occlusion: turn ARDK depth occlusion effect on/off
    /// Visualize Raw depth: show the ARDK depth buffer visualization
    /// Visualize Disparity Overlay: show the ARDK disparity overlay
    /// Visualize Raw Semantic: show the raw ARDK semantic texture visualization
    /// Skip Current Step: Skip the current resource collection step
    /// </summary>
    public class BuildAShipDebugManager : MonoBehaviour
    {
        // Used to cleanly map features to menu checkbox indices
        private enum CheckboxIndex
        {
            OcclusionEnabled = 0,
            VisualizeOcclusions,
            VisualizeRawSemantics,
        }
        
        // SDK
        ARDepthManager arDepthManager;
        ARCameraRenderingHelper arCameraRenderingHelper;

        // Demo
        BuildAShipManager buildAShipManager;
        BuildAShipResourceRenderer buildAShipResourceRenderer;

        [SerializeField] RawImage semanticSegmentationImage;

        [SerializeField] DebugMenuGUI debugMenuGUI;

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

        public bool VisualizeRawSemanticSegmentation
        {
            get
            {
                return semanticSegmentationImage.enabled;
            }
            set
            {
                if (value)
                {
                    semanticSegmentationImage.texture = buildAShipResourceRenderer.SemanticTexture;
                }
                semanticSegmentationImage.enabled = value;
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

        void Start()
        {
            // SDK
            arDepthManager = FindObjectOfType<ARDepthManager>();

            // Demo
            buildAShipManager = SceneLookup.Get<BuildAShipManager>();
            buildAShipResourceRenderer = SceneLookup.Get<BuildAShipResourceRenderer>();

            // Set initial checkbox values
            debugMenuGUI.SetChecked((int)CheckboxIndex.OcclusionEnabled, DepthOcclusionEnabled);
            debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizeOcclusions, VisualizeOcclusions);
            debugMenuGUI.SetChecked
            (
                (int)CheckboxIndex.VisualizeRawSemantics,
                VisualizeRawSemanticSegmentation
            );
        }

        void OnEnable()
        {
            // Subscribe to events
            DebugMenuGUI.EventDebugOption1Checkbox.AddListener(OnEventDebugOption1Checkbox);    // occlusion
            DebugMenuGUI.EventDebugOption2Checkbox.AddListener(OnEventDebugOption2Checkbox);    // visualize occlusion
            DebugMenuGUI.EventDebugOption3Checkbox.AddListener(OnEventDebugOption3Checkbox);    // semantic
            DebugMenuGUI.EventDebugOption5Button.AddListener(OnEventDebugOption5Button);        // skip
        }

        void OnDisable()
        {
            // Unsubscribe from events
            DebugMenuGUI.EventDebugOption1Checkbox.RemoveListener(OnEventDebugOption1Checkbox);
            DebugMenuGUI.EventDebugOption2Checkbox.RemoveListener(OnEventDebugOption2Checkbox);
            DebugMenuGUI.EventDebugOption3Checkbox.RemoveListener(OnEventDebugOption3Checkbox);
            DebugMenuGUI.EventDebugOption5Button.RemoveListener(OnEventDebugOption5Button);
        }


        void Update()
        {
            // force VisualizeRawSemanticSegmentation off if semantic segmentation resource renderer isn't visible
            // (but leave the checkbox value alone -- it can remain checked)
            if (!buildAShipResourceRenderer.Visible && VisualizeRawSemanticSegmentation)
            {
                VisualizeRawSemanticSegmentation = false;
            }

            // make sure VisualizeRawSemanticSegmentation matches checkbox when semantic segmentation resource renderer
            // is visible and it's processed a semantic segmentation texture
            int visSemanticIdx = (int)CheckboxIndex.VisualizeRawSemantics;
            if (buildAShipResourceRenderer.Visible &&
                buildAShipResourceRenderer.SemanticTextureProcessedSinceBecomingVisible &&
                VisualizeRawSemanticSegmentation != debugMenuGUI.GetChecked(visSemanticIdx))
            {
                VisualizeRawSemanticSegmentation = debugMenuGUI.GetChecked(visSemanticIdx);
            }
        }


        // occlusion
        private void OnEventDebugOption1Checkbox()
        {
            Debug.Log("OnEventDebugOption1Checkbox");
            DepthOcclusionEnabled = !DepthOcclusionEnabled;
            
            // If occlusion is now off, be sure to set the visualizer off
            if (!DepthOcclusionEnabled && VisualizeOcclusions)
            {
                VisualizeOcclusions = false;
                debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizeOcclusions, false);
            }
        }

        // visualize occlusion
        private void OnEventDebugOption2Checkbox()
        {
            Debug.Log("OnEventDebugOption2Checkbox");
            VisualizeOcclusions = !VisualizeOcclusions;

            // If we're visualizing occlusions, be sure occlusions are on
            if (VisualizeOcclusions && !DepthOcclusionEnabled)
            {
                DepthOcclusionEnabled = true;
                debugMenuGUI.SetChecked((int)CheckboxIndex.OcclusionEnabled, true);
            }
        }

        // disparity
        private void OnEventDebugOption3Checkbox()
        {
            Debug.Log("OnEventDebugOption3Checkbox");
            
            // Update loop maintains state of VisualizeRawSemanticSegmentation
        }


        // skip current step
        private void OnEventDebugOption5Button()
        {
            Debug.Log("OnEventDebugOption5Button");
            SkipCurrentStep();
        }

        public void SkipCurrentStep()
        {
            bool skipped = buildAShipManager.SkipCurrentStep();
            if (skipped)
            {
                debugMenuGUI.HideGUI();
            }
        }
    }
}
