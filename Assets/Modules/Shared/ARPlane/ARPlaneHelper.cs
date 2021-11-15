using UnityEngine;
using System.Collections.Generic;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Helper for accessing and utilizing the current collection of ARPlanes in the session
    /// </summary>
    public class ARPlaneHelper : MonoBehaviour, ISceneDependency
    {
        public static AppEvent<ARPlaneHelper> PlanesChanged = new AppEvent<ARPlaneHelper>();

        [Tooltip("Should ARPlanes be shown in the scene?")]
        [SerializeField] private bool showPlanes = false;

        [Tooltip("Should ARPlanes gameObjects be active in the scene?")]
        [SerializeField] private bool planeObjectsActive = true;

        private HashSet<ARPlane> planes = new HashSet<ARPlane>();

        public int NumPlanes => planes.Count;

        /// <summary>
        /// Get a copy of the the current planes
        /// </summary>
        public List<ARPlane> GetPlanes()
        {
            return new List<ARPlane>(planes);
        }

        /// <summary>
        /// Should AR planes be rendered?
        /// </summary>
        public void SetShowPlanes(bool showPlanes)
        {
            if (this.showPlanes != showPlanes)
            {
                this.showPlanes = showPlanes;

                foreach (ARPlane plane in planes)
                {
                    plane.Show(showPlanes);
                }
            }
        }

        /// <summary>
        /// Should the AR plane objects be active?
        /// </summary>
        public void SetPlaneObjectsActive(bool planeObjectsActive)
        {
            if (this.planeObjectsActive != planeObjectsActive)
            {
                this.planeObjectsActive = planeObjectsActive;

                foreach (ARPlane plane in planes)
                {
                    plane.gameObject.SetActive(planeObjectsActive);
                }
            }
        }

        private void Awake()
        {
            // Listen to plane created and destroy events for the lifetime of this helper
            ARPlane.PlaneCreated.AddListener(OnPlaneCreated);
            ARPlane.PlaneDestroyed.AddListener(OnPlaneDestroyed);
        }

        private void OnPlaneCreated(ARPlane plane)
        {
            // Track the plane
            planes.Add(plane);
            Debug.Log("OnPlaneCreated: " + plane.name);

            // Set initial plane state
            plane.Show(showPlanes);
            plane.gameObject.SetActive(planeObjectsActive);

            PlanesChanged.Invoke(this);
        }

        private void OnPlaneDestroyed(ARPlane plane)
        {
            Debug.Log("OnPlaneDestroyed: " + plane.name);
            planes.Remove(plane);

            PlanesChanged.Invoke(this);
        }

        private void OnDestroy()
        {
            ARPlane.PlaneCreated.RemoveListener(OnPlaneCreated);
            ARPlane.PlaneDestroyed.RemoveListener(OnPlaneDestroyed);
        }

        // Comment in to test
        // void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.S))
        //     {
        //         SetShowPlanes(!showPlanes);
        //     }

        //     if (Input.GetKeyDown(KeyCode.A))
        //     {
        //         SetPlaneObjectsActive(!planeObjectsActive);
        //     }
        // }
    }
}
