//#define TEST_ANIMATIONS

using System.Collections;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// Manages animation of the map airship
    /// </summary>
    public class Airship : MonoBehaviour, ISceneDependency
    {
        private const string AnimatorDepartBool = "FlyAway";

        [SerializeField] Animator animator;
        [SerializeField] AnimationClip airshipDepartClip;
        [SerializeField] GameObject airshipPoof;

        private AudioManager audioManager;
        private AudioSource airshipLoopSource;

        private void Awake()
        {
            // If the airship isn't yet built, or if it has departed, set it inactive
            if (!SaveUtil.IsAirshipBuilt() || SaveUtil.IsAirshipDeparted())
            {
                animator.gameObject.SetActive(false);
            }
            audioManager = SceneLookup.Get<AudioManager>();
        }

        private void Start()
        {
            // If the airship is active on start, start the loop
            if (animator.gameObject.activeInHierarchy)
            {
                airshipLoopSource = audioManager.PlayAudioOnObject(
                    AudioKeys.SFX_Airship_LP,
                    animator.gameObject,
                    spatialBlend: .5f,
                    loop: true,
                    fadeInDuration: .5f);
            }
        }

        public Coroutine Build()
        {
            return StartCoroutine(BuildRoutine());
        }

        private IEnumerator BuildRoutine()
        {
            airshipPoof.SetActive(true);
            animator.gameObject.SetActive(true);

            // Play the build audio
            audioManager.PlayAudioOnObject(
                AudioKeys.SFX_AirShipBuild,
                animator.gameObject,
                spatialBlend: .5f);

            yield return new WaitForSeconds(1);

            // Start the loop audio
            airshipLoopSource = audioManager.PlayAudioOnObject(
                AudioKeys.SFX_Airship_LP,
                animator.gameObject,
                spatialBlend: .5f,
                loop: true,
                fadeInDuration: .5f);

            SaveUtil.SaveAirshipBuilt();
        }

        public Coroutine Depart()
        {
            return StartCoroutine(DepartRoutine());
        }

        private IEnumerator DepartRoutine()
        {
            animator.SetBool(AnimatorDepartBool, true);
            SaveUtil.SaveAirshipDeparted();

            // Fade out the loop if it's playing
            if (airshipLoopSource != null)
            {
                audioManager.FadeOutAudioSource(airshipLoopSource, fadeDuration: .5f);
                airshipLoopSource = null;
            }

            // Play the fly-away audio
            audioManager.PlayAudioOnObject(
                AudioKeys.SFX_Airship_FlyAway,
                animator.gameObject,
                spatialBlend: .5f);

            // Wait for the animation to complete
            yield return new WaitForSeconds(airshipDepartClip.length);
        }

#if UNITY_EDITOR && TEST_ANIMATIONS
        void OnGUI()
        {
            GUIStyle customButton = new GUIStyle("button");
            customButton.fontSize = 60;

            if (GUILayout.Button("Build Airship", customButton, GUILayout.Width(400), GUILayout.Height(100)))
            {
                Build();
            }

            if (GUILayout.Button("Depart Airship", customButton, GUILayout.Width(400), GUILayout.Height(100)))
            {
                Depart();
            }
        }
#endif
    }
}
