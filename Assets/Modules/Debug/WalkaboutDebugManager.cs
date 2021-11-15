using Niantic.ARDK.AR.Awareness.Depth.Effects;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Extensions.Depth;
using Niantic.ARDK.Extensions.Meshing;
using Niantic.ARDK.Rendering;
using UnityEngine;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// Manages the debug functionality for the Walkabout scene
    /// Occlusion: turn ARDK depth occlusion effect on/off
    /// Visualize Raw depth: show the ARDK depth buffer visualization
    /// Visualize Disparity Overlay: show the ARDK disparity overlay
    /// Visualize Persistent Mesh: turn the visualization of the ARDK mesh on/off
    /// Clear Persistent Mesh: clear the currently recognized ARDK mesh
    /// Re-place Captain Doty: If captain Doty has been placed on a Gameboard surface, allows player to re-place them
    /// </summary>
    public class WalkaboutDebugManager : MonoBehaviour
    {
        // Used to cleanly map features to menu checkbox indices
        private enum CheckboxIndex
        {
            OcclusionEnabled = 0,
            VisualizeOcclusions,
            VisualizePersistentMesh,
        }
        
        // SDK
        ARMeshManager arMeshManager;
        ARCameraRenderingHelper arCameraRenderingHelper;
        ARDepthManager arDepthManager;

        // Demo
        WalkaboutManager walkaboutManager;

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

        [SerializeField]
        DebugMenuGUI debugMenuGUI;

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

        public bool VisualizePersistentMesh
        {
            get
            {
                return !arMeshManager.UseInvisibleMaterial;
            }

            set
            {
                arMeshManager.UseInvisibleMaterial = !value;
            }
        }

        void Start()
        {
            // SDK
            arMeshManager = FindObjectOfType<ARMeshManager>();
            arDepthManager = FindObjectOfType<ARDepthManager>();

            // Demo
            walkaboutManager = SceneLookup.Get<WalkaboutManager>();

            // Set initial checkbox values
            debugMenuGUI.SetChecked((int)CheckboxIndex.OcclusionEnabled, DepthOcclusionEnabled);
            debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizeOcclusions, VisualizeOcclusions);
            debugMenuGUI.SetChecked((int)CheckboxIndex.VisualizePersistentMesh, VisualizePersistentMesh);
        }


        void OnEnable()
        {
            // Subscribe to events
            DebugMenuGUI.EventDebugOption1Checkbox.AddListener(OnEventDebugOption1Checkbox);    // occlusion
            DebugMenuGUI.EventDebugOption2Checkbox.AddListener(OnEventDebugOption2Checkbox);    // visualize occlusion
            DebugMenuGUI.EventDebugOption3Checkbox.AddListener(OnEventDebugOption3Checkbox);    // persistent mesh
            DebugMenuGUI.EventDebugOption5Button.AddListener(OnEventDebugOption5Button);        // clear mesh
            DebugMenuGUI.EventDebugOption6Button.AddListener(OnEventDebugOption6Button);        // re-place doty
        }

        void OnDisable()
        {
            // Unsubscribe from events
            DebugMenuGUI.EventDebugOption1Checkbox.RemoveListener(OnEventDebugOption1Checkbox);
            DebugMenuGUI.EventDebugOption2Checkbox.RemoveListener(OnEventDebugOption2Checkbox);
            DebugMenuGUI.EventDebugOption3Checkbox.RemoveListener(OnEventDebugOption3Checkbox);
            DebugMenuGUI.EventDebugOption5Button.RemoveListener(OnEventDebugOption5Button);
            DebugMenuGUI.EventDebugOption6Button.RemoveListener(OnEventDebugOption6Button);
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


        // persistent mesh
        private void OnEventDebugOption3Checkbox()
        {
            Debug.Log("OnEventDebugOption3Checkbox");
            VisualizePersistentMesh = !VisualizePersistentMesh;
        }


        // clear mesh
        private void OnEventDebugOption5Button()
        {
            Debug.Log("OnEventDebugOption5Button");
            ClearPersistentMesh();
        }


        // re-place doty
        private void OnEventDebugOption6Button()
        {
            Debug.Log("OnEventDebugOption6Button");
            RePlaceDoty();
        }


        public void ClearPersistentMesh()
        {
            arMeshManager.ClearMeshObjects();
        }

        public void RePlaceDoty()
        {
            bool rePlaced = walkaboutManager.RePlaceDoty();
            if (rePlaced)
            {
                debugMenuGUI.HideGUI();
            }
        }

    }
}
