using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    public struct SnowballBurstParticle
    {
        public Rigidbody rigidbody;
        public float startTime;
        public float endTime;
        public Vector3 startScale;
    }

    /// <summary>
    /// Instantiates 3D particle objects via Snowball class when a snowball bursts.
    /// Scale decays (melts) over time.
    /// </summary>
    public class SnowballBurst : MonoBehaviour
    {
        [SerializeField] float meltDuration = 12;
        [SerializeField] bool applyForce = true;
        [SerializeField] bool randomizeScale = true;
        [SerializeField] GameObject secondaryParticles;

        private List<SnowballBurstParticle> particles = new List<SnowballBurstParticle>();

        void OnDrawGizmosSelected()
        {
            // Draw a yellow sphere at the transform's position
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, .025f);
        }

        void Start()
        {
            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rigidbody in rigidbodies)
            {
                rigidbody.velocity = Vector3.zero;

                // Configure particle.
                float random = Random.Range(.25f, 1f);

                SnowballBurstParticle particleData;
                particleData.rigidbody = rigidbody;
                particleData.startTime = Time.time;
                particleData.endTime = Time.time + (random * meltDuration);
                particleData.startScale = (randomizeScale) ?
                    Vector3.one * random * .85f :
                    rigidbody.transform.localScale;

                particles.Add(particleData);

                // Apply force.
                if (applyForce)
                {
                    Vector3 launchVector = new Vector3(
                        Random.Range(-1, 1),
                        Random.Range(2, 4),
                        Random.Range(-1, 1)
                    );
                    rigidbody.AddForce(launchVector, ForceMode.Impulse);
                    rigidbody.AddTorque(Vector3.up * 5);
                }
            }
        }

        void Update()
        {
            // Scale decays (melts) over time
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                SnowballBurstParticle particleData = particles[i];

                float t = (Time.time - particleData.startTime) /
                          (particleData.endTime - particleData.startTime);

                particleData.rigidbody.transform.localScale =
                    particleData.startScale * (1.0f - t);

                if (t >= 1)
                {
                    Destroy(particleData.rigidbody.gameObject);
                    particles.Remove(particleData);
                }
            }

            if (particles.Count == 0) Destroy(gameObject);
        }

        public void TriggerSecondaryParticles()
        {
            secondaryParticles?.SetActive(true);
        }
    }
}