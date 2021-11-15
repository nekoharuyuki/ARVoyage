using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Helper class to change scene lighting based on ARDK light estimation.
    /// </summary>
    public class LightEstimationHelper : MonoBehaviour, ISceneDependency
    {
        [SerializeField] Light directionalLight;
        private float baseIntensity;

#if UNITY_EDITOR
        [SerializeField] [Range(0, 1000)] float debugIntensity = 1000;
#endif

        private Color baseAmbientSkyColor;
        private Color baseAmbientEquatorColor;
        private Color baseAmbientGroundColor;

        private IARSession arSession;

        private void Start()
        {
            ARSessionFactory.SessionInitialized += OnSessionInitialized;

            baseIntensity = directionalLight.intensity;

            baseAmbientSkyColor = RenderSettings.ambientSkyColor;
            baseAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            baseAmbientGroundColor = RenderSettings.ambientGroundColor;
        }

        private void Update()
        {
#if UNITY_EDITOR
            SetLightIntensity(debugIntensity);
#endif
        }

        private void OnDestroy()
        {
            ARSessionFactory.SessionInitialized -= OnSessionInitialized;

            if (arSession != null) arSession.FrameUpdated -= OnFrameUpdated;
        }

        private void OnSessionInitialized(AnyARSessionInitializedArgs args)
        {
            IARSession oldSession = arSession;
            if (oldSession != null)
            {
                oldSession.FrameUpdated -= OnFrameUpdated;
            }

            IARSession newSession = args.Session;
            arSession = newSession;

            newSession.FrameUpdated += OnFrameUpdated;
        }

        private void OnFrameUpdated(FrameUpdatedArgs args)
        {
            IARFrame frame = args.Frame;
            IARLightEstimate lightEstimate = frame.LightEstimate;

            if (lightEstimate != null)
            {
                float intensity = lightEstimate.AmbientIntensity;
                SetLightIntensity(intensity);
            }
        }

        private void SetLightIntensity(float intensity)
        {
            float normalizedIntensity;
                
#if UNITY_IOS
            normalizedIntensity = Mathf.Clamp01(intensity / 1000f);
#elif UNITY_ANDROID
            normalizedIntensity = Mathf.Sqrt(Mathf.Clamp01(intensity + 0.8f * intensity));
#endif
            
            directionalLight.intensity = normalizedIntensity * baseIntensity;

            RenderSettings.ambientSkyColor = baseAmbientSkyColor * normalizedIntensity;
            RenderSettings.ambientEquatorColor = baseAmbientEquatorColor * normalizedIntensity;
            RenderSettings.ambientGroundColor = baseAmbientGroundColor * normalizedIntensity;
        }
    }
}