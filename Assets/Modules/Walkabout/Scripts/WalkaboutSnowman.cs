using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// Snowman used in Walkabout demo.
    /// At first only the bottom half (base) of the snowman appears.
    /// The player can cause a hover VFX to appear on the snowman.
    /// Eventually the top half (head) appears.
    /// </summary>
    public class WalkaboutSnowman : MonoBehaviour
    {
        [SerializeField] GameObject snowmanBase;
        [SerializeField] GameObject snowmanBody;
        [SerializeField] GameObject baseParticles;
        [SerializeField] GameObject bodyParticles;

        private WalkaboutSnowmanEffects effects;

        public void Awake()
        {
            effects = GetComponent<WalkaboutSnowmanEffects>();
        }

        // Auto reset on each enable. Reveal animations
        // must be triggered explicitly.
        public void OnEnable()
        {
            Reset();
        }

        // Hide all assets.
        public void Reset()
        {
            baseParticles.SetActive(false);
            bodyParticles.SetActive(false);

            snowmanBase.SetActive(false);
            snowmanBody.SetActive(false);

            SetHover(false);
        }

        // Reveal the base, which triggers its animation.
        public void RevealBase()
        {
            snowmanBase.SetActive(true);
            snowmanBody.SetActive(false);

            baseParticles.SetActive(true);
            bodyParticles.SetActive(false);
        }

        // Reveal the body/head (total snowman), 
        // which triggers its animation and reveal the particles.
        public void RevealBody()
        {
            snowmanBase.SetActive(false);
            snowmanBody.SetActive(true);

            baseParticles.SetActive(false);
            bodyParticles.SetActive(true);
        }

        public void SetHover(bool hover)
        {
            effects.SetHover(hover);
        }

#if UNITY_EDITOR
        // Debug UI
        private void OnGUI()
        {
            if (GUILayout.Button("Reset")) Reset();
            if (GUILayout.Button("RevealBase")) RevealBase();
            if (GUILayout.Button("RevealBody")) RevealBody();

            if (GUILayout.Button("Hover")) SetHover(true);
            if (GUILayout.Button("No Hover")) SetHover(false);
        }
#endif

    }
}
