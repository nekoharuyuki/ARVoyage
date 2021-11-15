using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Niantic.ARDKExamples.Gameboard;

namespace Niantic.ARVoyage.Walkabout
{
    public class Clipper : MonoBehaviour
    {
        [SerializeField] float waterLevel = 0;
        [SerializeField] float surfaceOffset = 0;
        [SerializeField] float surfaceElevation = 0;

        void Awake()
        {
            GameboardHelper.SurfaceUpdated.AddListener(OnSurfaceUpdated);
        }

        void OnDestroy()
        {
            GameboardHelper.SurfaceUpdated.RemoveListener(OnSurfaceUpdated);
        }

        public void OnSurfaceUpdated(Surface surface)
        {
            surfaceElevation = surface.Elevation;
        }

        void Update()
        {
            waterLevel = surfaceElevation + surfaceOffset;
            Shader.SetGlobalFloat("_WaterLevel", waterLevel);
        }
    }
}
