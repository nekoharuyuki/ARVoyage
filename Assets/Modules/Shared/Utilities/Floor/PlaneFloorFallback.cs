using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Fallback AR floor plane for demo scenes.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(BoxCollider))]
    public class PlaneFloorFallback : MonoBehaviour
    {
        private IARSession arSession;

        private MeshRenderer meshRenderer;
        private BoxCollider boxCollider;

        private Vector3 floorPosition;

        [SerializeField] private bool floorVisible = true;
        public bool FloorVisible
        {
            get
            {
                return floorVisible;
            }
            set
            {
                ShowFloor(value);
                floorVisible = value;
            }
        }

        private void Awake()
        {
            // Find the plane helper and subscribe to events.
            ARPlaneHelper.PlanesChanged.AddListener(OnPlanesChanged);

            meshRenderer = GetComponent<MeshRenderer>();
            boxCollider = GetComponent<BoxCollider>();
        }

        private void OnDestroy()
        {
            // Unsubscribe on destroy.
            ARPlaneHelper.PlanesChanged.RemoveListener(OnPlanesChanged);
        }

        void ShowFloor(bool visible)
        {
            meshRenderer.enabled = visible;
        }

        private void OnPlanesChanged(ARPlaneHelper arPlaneHelper)
        {
            List<ARPlane> planes = arPlaneHelper.GetPlanes();

            // Find lower bounds.
            floorPosition.y = Mathf.Infinity;
            foreach (ARPlane plane in planes)
            {
                Vector3 planePosition = plane.transform.position;
                if (planePosition.y < floorPosition.y)
                {
                    floorPosition.y = planePosition.y;
                }
            }

            if (floorPosition.y < Mathf.Infinity)
            {
                transform.position = floorPosition;

                // Enable the renderer/collider as needed.
                meshRenderer.enabled = floorVisible;
                boxCollider.enabled = true;

                //Debug.LogFormat("PlaneFloorFallback.AnchorsChanged: FloorY: {0}", floorPosition.y);
            }
        }
    }
}