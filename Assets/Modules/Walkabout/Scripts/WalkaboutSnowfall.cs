
using UnityEngine;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// Snowfall particle effects for Walkabout demo 
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class WalkaboutSnowfall : MonoBehaviour
    {
        private WalkaboutActor yetiAndSnowball;
        private ParticleSystem.EmissionModule emissionModule;

        void Awake()
        {
            yetiAndSnowball = SceneLookup.Get<WalkaboutManager>().yetiAndSnowball;

            ParticleSystem snowParticleSystem = GetComponent<ParticleSystem>();
            emissionModule = snowParticleSystem.emission;
            emissionModule.enabled = false;
        }

        void Update()
        {
            // Constrain to snowing above Yeti's position
            transform.position = yetiAndSnowball.transform.position;

            // Enable if Yeti is active and is not transparent
            emissionModule.enabled = yetiAndSnowball.gameObject.activeInHierarchy && !yetiAndSnowball.IsTransparent;
        }
    }
}
