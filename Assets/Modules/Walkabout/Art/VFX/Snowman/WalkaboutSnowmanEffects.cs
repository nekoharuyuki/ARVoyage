using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// VFX for snowman
    /// </summary>
    public class WalkaboutSnowmanEffects : MonoBehaviour
    {
        const float AlbedoBoostBase = 1;
        const float ShaftAlphaBase = .5f;

        const float AlbedoBoostHover = 1.35f;
        const float ShaftAlphaHover = 1f;

        [SerializeField] MeshRenderer bodyRenderer = null;
        [SerializeField] MeshRenderer shaftRenderer = null;

        void Start()
        {
            Reset();
        }

        public void Reset()
        {
            SetAlbedoBoost(AlbedoBoostBase);
            SetShaftAlpha(ShaftAlphaBase);
        }

        public void SetHover(bool value)
        {
            float duration = .5f;
            float targetAlbedoBoost = value ? AlbedoBoostHover : AlbedoBoostBase;
            float targetShaftAlpha = value ? ShaftAlphaHover : ShaftAlphaBase;

            Animate(targetAlbedoBoost, targetShaftAlpha, duration);
        }

        private void Animate(float targetAlbedoBoost, float targetShaftAlpha, float duration)
        {
            float startAlbedoBoost = 0, startShaftAlpha = 0;

            InterpolationUtil.EasedInterpolation(gameObject, gameObject,
                InterpolationUtil.EaseInOutCubic, duration,
                onStart: () =>
                {
                    startAlbedoBoost = GetAlbedoBoost();
                    startShaftAlpha = GetShaftAlpha();
                },
                onUpdate: (t) =>
                 {
                     float boost = Mathf.Lerp(startAlbedoBoost, targetAlbedoBoost, t);
                     SetAlbedoBoost(boost);

                     float shaftAlpha = Mathf.Lerp(startShaftAlpha, targetShaftAlpha, t);
                     SetShaftAlpha(shaftAlpha);
                 }
            );
        }

        private float GetAlbedoBoost()
        {
            return bodyRenderer.material.GetFloat("_AlbedoBoost");
        }

        private void SetAlbedoBoost(float boost)
        {
            bodyRenderer.material.SetFloat("_AlbedoBoost", boost);
        }

        private float GetShaftAlpha()
        {
            return shaftRenderer.material.GetFloat("_MasterAlpha");
        }

        private void SetShaftAlpha(float alpha)
        {
            shaftRenderer.material.SetFloat("_MasterAlpha", alpha);
        }

#if UNITY_EDITOR && FALSE
        private void OnGUI()
        {
            if (GUILayout.Button("Hover")) SetHover(true);
            if (GUILayout.Button("No Hover")) SetHover(false);
        }
#endif
    }
}