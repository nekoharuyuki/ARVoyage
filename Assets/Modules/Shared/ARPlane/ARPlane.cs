using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Functionality for ARPlanes recognized by ARDK's ARPlaneManager
    /// These planes are collected and accessible via the ARPlaneHelper
    /// </summary>
    public class ARPlane : MonoBehaviour
    {
        public static AppEvent<ARPlane> PlaneCreated = new AppEvent<ARPlane>();
        public static AppEvent<ARPlane> PlaneDestroyed = new AppEvent<ARPlane>();

        [SerializeField] Renderer _renderer;

        private void Start()
        {
            // Wait until Start to invoke this so ARPlaneHelper will be ready to receive it
            PlaneCreated.Invoke(this);
        }

        public void Show(bool show)
        {
            _renderer.enabled = show;
        }

        private void OnDestroy()
        {
            PlaneDestroyed.Invoke(this);
        }
    }
}
