using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Helper for various rendering functionality, including setting render scale
    /// </summary>
    public class RenderPipelineHelper : MonoBehaviour, ISceneDependency
    {
        [SerializeField] float shadowDistance = 4;
        private float originalShadowDistance;

        [SerializeField] bool scaleRender = false;
        [SerializeField] int referenceResolution = 1920;
        private float originalRenderScale;

        private UniversalRenderPipelineAsset pipelineAsset;

        public void SetScaleRender(bool scaleRender)
        {
            this.scaleRender = scaleRender;
            UpdateScaling();
        }

        public void SetReferenceResolution(int referenceResolution)
        {
            this.referenceResolution = referenceResolution;
            UpdateScaling();
        }

        void Awake()
        {
            pipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;

            if (pipelineAsset)
            {
                // Render scale.
                originalRenderScale = pipelineAsset.renderScale;
                UpdateScaling();

                // Shadow settings.
                originalShadowDistance = pipelineAsset.shadowDistance;
                pipelineAsset.shadowDistance = shadowDistance;
            }
        }

        void Update()
        {
#if UNITY_EDITOR
            UpdateScaling();
#endif
        }

        void UpdateScaling()
        {
            if (scaleRender)
            {
                float scale = Mathf.Clamp01((float)referenceResolution / (float)Camera.main.pixelHeight);
                pipelineAsset.renderScale = scale;
            }
            else
            {
                pipelineAsset.renderScale = originalRenderScale;
            }
        }

        void OnDestroy()
        {
            if (pipelineAsset)
            {
                pipelineAsset.renderScale = originalRenderScale;
                pipelineAsset.shadowDistance = originalShadowDistance;
            }
        }
    }
}