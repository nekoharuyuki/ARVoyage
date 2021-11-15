using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Attached to a small set of mock AR planes, useful for developing demos in editor,
    /// e.g. used as a example mock environment in the multiplayer SnowballFight demo.
    /// 
    /// Draws gizmo-dots on the camera-visible sections (grid elements) of the mock AR planes.
    /// </summary>
    public class MockMultiplayerArena : MonoBehaviour
    {
        private const float planeGridElementSize = 1f;

        [SerializeField] public List<ARPlane> mockARPlanes;
        private List<ARPlane> arPlanes = null;
        private ARPlaneHelper arPlaneHelper;

        [SerializeField] public List<GameObject> mockOtherPlayers;
        private List<GameObject> players;

        private List<List<Vector3>> arPlanesGridElements = null;
        private Vector3 itemPlacementPoint = Vector3.zero;
        private float nextSampleTime = 0f;


        void Awake()
        {
            arPlaneHelper = SceneLookup.Get<ARPlaneHelper>();

            // Now acquired via arPlaneHelper
            //arPlanes = new List<GameObject>(mockARPlanes);

            players = new List<GameObject>(mockOtherPlayers);
            players.Add(Camera.main.gameObject);
        }


        void Update()
        {
            if (Time.time > nextSampleTime)
            {
                nextSampleTime = Time.time + 1f;

                // Get latest list of AR planes
                arPlanes = arPlaneHelper.GetPlanes();

                // Get list of visible item placement positions
                arPlanesGridElements = DemoUtil.CalculateARPlanesGridElements(arPlanes, planeGridElementSize);
                List<Vector3> visibleGridElements =
                    //DemoUtil.FindCameraVisibleGridElements(arPlanesGridElements, Camera.main.transform);
                    DemoUtil.FindInFrontGridElements(arPlanesGridElements, Camera.main.transform);

                // randomly choose an item placement position from the list of candidates
                if (visibleGridElements.Count > 0)
                {
                    itemPlacementPoint = visibleGridElements[UnityEngine.Random.Range(0, visibleGridElements.Count - 1)];
                }
            }
        }


        // Draw all grid elements, color-coded for visibility,
        // and the latest item placement position
        private void OnDrawGizmos()
        {
            if (arPlanes == null || arPlanes.Count == 0) return;
            if (arPlanesGridElements == null) return;

            if (itemPlacementPoint != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(itemPlacementPoint + new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 0.1f, 0.1f));
            }

            int planeCtr = 0;
            List<Vector3> visibleGridElements =
                //DemoUtil.FindCameraVisibleGridElements(arPlanesGridElements, Camera.main.transform);
                DemoUtil.FindInFrontGridElements(arPlanesGridElements, Camera.main.transform);

            foreach (List<Vector3> arPlaneGridElements in arPlanesGridElements)
            {
                Color color;
                switch (planeCtr % 5)
                {
                    case 0: color = Color.green; break;
                    case 1: color = Color.blue; break;
                    case 2: color = Color.magenta; break;
                    case 3: color = Color.red; break;
                    case 4: color = Color.cyan; break;
                    default: color = Color.white; break;
                }

                foreach (Vector3 gridElement in arPlaneGridElements)
                {
                    Gizmos.color = visibleGridElements.Contains(gridElement) ? color : Color.grey;
                    Gizmos.DrawSphere(gridElement, 0.05f);
                }

                ++planeCtr;
            }
        }

    }
}
