using UnityEngine;
using Niantic.ARDK.Configuration;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// Manages setting the default fixed timestamp for the project as well as the ARDK feature model urls 
    /// </summary>
    public class MapManager : MonoBehaviour, ISceneDependency
    {
        public const float DefaultFixedTimestep = .02f;
        public const string DbowUrl = "https://bowvocab.eng.nianticlabs.com/dbow_b50_l3.bin";
        public const string ContextAwarenessUrl = "https://storage.googleapis.com/niantic-production-models/multidepth_v0.6.1e-all_trunc.bin";

        void Awake()
        {
            // When loading into map, restore the default fixed timestep since this may be set per scene
            Time.fixedDeltaTime = DefaultFixedTimestep;

            // When the map loads, set the Dbow and ContextAwareness urls as well, so that they're ready for the FeaturePreloadManager to download the feature models

            if (ArdkGlobalConfig.SetDbowUrl(DbowUrl))
            {
                Debug.Log("Set the DBoW URL to: " + DbowUrl);
            }

            if (ArdkGlobalConfig.SetContextAwarenessUrl(ContextAwarenessUrl))
            {
                Debug.Log("Set the Context Awareness URL to: " + ContextAwarenessUrl);
            }
        }
    }
}
