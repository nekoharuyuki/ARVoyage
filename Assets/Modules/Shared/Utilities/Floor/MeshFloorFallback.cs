using Niantic.ARDK.Extensions.Meshing;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Fallback mesh floor plane for demo scenes.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(BoxCollider))]
    public class MeshFloorFallback : MonoBehaviour
    {
        [SerializeField] ARMeshManager meshUpdater;

        private MeshRenderer meshRenderer;
        private BoxCollider boxCollider;

        private Vector3 floorPosition = new Vector3(0, Mathf.Infinity, 0);

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

        // Inverted convenience property to match ARMeshManager.UseInvisibleMaterial when called from inspector
        public bool FloorInvisible
        {
            get => !FloorVisible;
            set => FloorVisible = !value;
        }

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            boxCollider = GetComponent<BoxCollider>();

            if (meshUpdater != null) meshUpdater.MeshObjectsUpdated += RecalculateFloor;
        }

        void OnDestroy()
        {
            if (meshUpdater != null) meshUpdater.MeshObjectsUpdated -= RecalculateFloor;
        }

        void ShowFloor(bool visible)
        {
            meshRenderer.enabled = visible;
        }

        void RecalculateFloor(MeshObjectsUpdatedArgs args)
        {
            foreach (GameObject colliderGameObject in args.CollidersUpdated)
            {
                Collider collider = colliderGameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    if (collider.bounds.min.y < floorPosition.y)
                    {
                        // Update to the new lowest poisition.
                        floorPosition.y = collider.bounds.min.y;
                        transform.position = floorPosition;

                        // Enable the renderer/collider as needed.
                        meshRenderer.enabled = floorVisible;
                        boxCollider.enabled = true;

                        Debug.Log("MeshFloorFallback.RecalculateFloor: floorPosition.y:" + floorPosition.y);
                    }
                }
            }
        }
    }
}