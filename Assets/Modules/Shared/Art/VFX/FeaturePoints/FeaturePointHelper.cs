using System.Collections.Generic;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Animate particles for 3D feature points.
    /// </summary>
    public class FeaturePointHelper : MonoBehaviour, ISceneDependency
    {
        // AR config.
        [SerializeField] private Camera arCamera;
        private IARSession arSession;

        // Particle setup.
        [SerializeField] private FeaturePointParticle particlePrefab;
        [SerializeField] private float distanceBetweenPoints = 0.2f;
        [SerializeField] private float nearCullThreshold = 1f;
        [SerializeField] Transform mockTransform;

        // Collections.
        private HashSet<Vector3> featurePoints = new HashSet<Vector3>();
        private List<FeaturePointParticle> particlesPooled = new List<FeaturePointParticle>();
        private List<FeaturePointParticle> particlesActive = new List<FeaturePointParticle>();

        private int maxParticles = 1024;
        private int maxFeaturePoints = 4096;

        // Animation.
        private AnimationCurve scaleCurve = new AnimationCurve();

        public bool Tracking { get; set; } = false;
        public bool Spawning { get; set; } = false;

        private void Start()
        {
            scaleCurve.AddKey(0, 0);
            scaleCurve.AddKey(.5f, .1f);
            scaleCurve.AddKey(1, 0);

            for (int i = 0; i < maxParticles; i++)
            {
                FeaturePointParticle particle = Instantiate(particlePrefab, Vector3.zero, Quaternion.identity, transform);
                particle.gameObject.SetActive(false);
                particlesPooled.Add(particle);
            }

            ARSessionFactory.SessionInitialized += OnSessionInitialized;
        }

        private void OnDestroy()
        {
            ARSessionFactory.SessionInitialized -= OnSessionInitialized;

            if (arSession != null) arSession.FrameUpdated -= OnFrameUpdated;

            foreach (var panel in particlesActive) Destroy(panel);
            foreach (var panel in particlesPooled) Destroy(panel);

            particlesActive.Clear();
            particlesPooled.Clear();

            featurePoints.Clear();
        }

        private void Update()
        {

#if UNITY_EDITOR
            List<Vector3> mockPoints = new List<Vector3>();
            for (int i = 0; i < 300; i++)
            {
                mockPoints.Add(Random.onUnitSphere + mockTransform.position);
            }
            ProcessPoints(mockPoints);
#endif

            // Cull points that are no longer visible
            featurePoints.RemoveWhere((point) =>
            {
                return !CheckBounds(point);
            });

            // Spawn new particles from current point set
            // with a fixed probability.
            if (Spawning)
            {
                foreach (Vector3 point in featurePoints)
                {
                    if (Random.value > .98f && particlesPooled.Count > 0)
                    {
                        FeaturePointParticle featurePointParticle = particlesPooled[0];
                        particlesPooled.RemoveAt(0);

                        featurePointParticle.transform.position = point;
                        featurePointParticle.transform.localScale = Vector3.zero;
                        featurePointParticle.startTime = Time.time;
                        featurePointParticle.gameObject.SetActive(true);

                        particlesActive.Add(featurePointParticle);
                    }
                }
            }

            // Animate current particles.
            for (int i = particlesActive.Count - 1; i >= 0; i--)
            {
                FeaturePointParticle featurePointParticle = particlesActive[i];
                float t = (Time.time - featurePointParticle.startTime) / 1f;

                if (t <= 1)
                {
                    featurePointParticle.transform.localScale = Vector3.one * scaleCurve.Evaluate(t);
                    featurePointParticle.transform.LookAt(Camera.main.transform.position);
                }
                else
                {
                    particlesActive.RemoveAt(i);
                    featurePointParticle.gameObject.SetActive(false);
                    particlesPooled.Add(featurePointParticle);
                }
            }
        }

        private void OnSessionInitialized(AnyARSessionInitializedArgs args)
        {
            var oldSession = arSession;
            if (oldSession != null)
            {
                oldSession.FrameUpdated -= OnFrameUpdated;
            }

            var newSession = args.Session;
            arSession = newSession;

            newSession.FrameUpdated += OnFrameUpdated;
        }

        private void OnFrameUpdated(FrameUpdatedArgs args)
        {
            if (Tracking)
            {
                var frame = args.Frame;
                if (frame.RawFeaturePoints == null)
                {
                    return;
                }
                var points = frame.RawFeaturePoints.Points;
                ProcessPoints(points);
            }
        }

        private void ProcessPoints(IList<Vector3> points)
        {
            if (featurePoints.Count < maxFeaturePoints)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    // Make sure we don't already know about this point and that it
                    // isn't too close to an existing point.
                    if (!featurePoints.Contains(points[i]) && CheckDistance(points[i]))
                    {
                        featurePoints.Add(points[i]);
                    }
                }
            }
        }

        private bool CheckBounds(Vector3 position)
        {
            // Points too close to the camera are invalid.
            float cameraDistanceSquared = (position - arCamera.transform.position).sqrMagnitude;
            if (cameraDistanceSquared < nearCullThreshold * nearCullThreshold) return false;

            // Points not in the viewport are invalid.
            Vector3 point = arCamera.WorldToViewportPoint(position);
            if (point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) return false;

            return true;
        }

        private bool CheckDistance(Vector3 position)
        {
            // Points too close to existing points are invalid.
            foreach (Vector3 point in featurePoints)
            {
                float distanceSquared = (position - point).sqrMagnitude;
                if (distanceSquared < distanceBetweenPoints * distanceBetweenPoints) return false;
            }

            return true;
        }

#if UNITY_EDITOR && FALSE
        private void OnGUI()
        {
            if (GUILayout.Button("Tracking On")) Tracking = true;
            if (GUILayout.Button("Tracking Off")) Tracking = false;
            if (GUILayout.Button("Spawning On")) Spawning = true;
            if (GUILayout.Button("Spawning Off")) Spawning = false;
        }
#endif

    }
}
