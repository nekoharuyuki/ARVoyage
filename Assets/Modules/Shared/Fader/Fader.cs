using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Utility class that controls full-screen fading as well as providing functionality for fading specific objects
    /// </summary>
    public class Fader : MonoBehaviour, ISceneDependency
    {
        public const float SceneFadeDuration = 1f;

        // Events invoked when the scene is fading in/out. Includes the fade duration.
        public static AppEvent<float> FadingSceneIn = new AppEvent<float>();
        public static AppEvent<float> FadingSceneOut = new AppEvent<float>();

        // Is the scene faded in?
        public bool IsSceneFadedIn => !sceneFadeCanvasGroup.gameObject.activeInHierarchy;

        // Canvas group used for fullscreen fades
        private CanvasGroup sceneFadeCanvasGroup;

        [Tooltip("Should the scene fade in on start?")]
        [SerializeField] private bool fadeInOnStart = true;

        private Dictionary<CanvasGroup, Coroutine> runningFades = new Dictionary<CanvasGroup, Coroutine>();

        private void Awake()
        {
            // Get the scene fade canvas group and set its initial state
            sceneFadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
            if (sceneFadeCanvasGroup == null)
            {
                Debug.LogError(name + " didnt't find CanvasGroup in children.");
                return;
            }

            // initialize faded out
            if (fadeInOnStart)
            {
                FadeSceneOutImmediate();
            }
        }

        private void Start()
        {
            if (fadeInOnStart)
            {
                FadeSceneIn();
            }
        }

        /// <summary>
        /// Fade the canvas group to the desired alpha.
        /// Will first stop any running fades for the CanvasGroup.
        /// Will activate the canvas group's gameObject at the start of the fade.
        /// </summary>
        /// <param name="canvasGroup">The canvasGroup to fade</param>
        /// <param name="alpha">The target alpha</param>
        /// <param name="duration">The fade duration</param>
        /// <param name="deactivateIfFadedOut">Should the canvasGroup's gameObject be deactivated when complete if this is a fade out to 0 alpha? Defaults to true.</param>
        /// <param name="onComplete">An action to invoke when the fade is completed. Defaults to null.</param>
        /// <param name="initialDelay">Time to wait before starting the fade</param>
        /// <returns>The fade coroutine. Can be yielded on from a Coroutine.</returns>
        public Coroutine Fade(CanvasGroup canvasGroup, float alpha, float duration, bool deactivateIfFadedOut = true, Action onComplete = null, float initialDelay = 0f)
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning("Ignoring fade for null CanvasGroup.");
                return null;
            }

            canvasGroup.gameObject.SetActive(true);
            float startAlpha = canvasGroup.alpha;

            Debug.Log("Start fading  " + canvasGroup.name + " to " + alpha + " at " + Time.time);

            // Use the InterpolationUtil to run the fade
            return InterpolationUtil.LinearInterpolation(
                target: canvasGroup,
                operationKey: this,
                duration: duration,
                preWait: initialDelay,
                onUpdate: (float percent) =>
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, alpha, percent);
                },
                onComplete: () =>
                {
                    // If specified and this is a fade out, disable the canvas group's gameobject
                    if (deactivateIfFadedOut && Mathf.Approximately(alpha, 0))
                    {
                        canvasGroup.gameObject.SetActive(false);
                    }

                    Debug.Log("Done fading  " + canvasGroup.name + " to " + alpha + " at " + Time.time);
                    onComplete?.Invoke();
                }
            );
        }

        /// <summary>
        /// Fade the scene in
        /// While the scene is fading, the scene fade canvas will block all input
        /// </summary>
        public Coroutine FadeSceneIn(float duration = SceneFadeDuration, Action onComplete = null)
        {
            FadingSceneIn.Invoke(duration);
            return Fade(sceneFadeCanvasGroup, 0, duration, onComplete: onComplete);
        }

        /// <summary>
        /// Fade the scene out
        /// While the scene is fading, the scene fade canvas will block all input
        /// </summary>
        public Coroutine FadeSceneOut(float duration = SceneFadeDuration, Action onComplete = null)
        {
            FadingSceneOut.Invoke(duration);
            return Fade(sceneFadeCanvasGroup, 1, duration, onComplete: onComplete);
        }

        /// <summary>
        /// Fade the scene in immediately
        /// </summary>
        public void FadeSceneInImmediate()
        {
            FadingSceneIn.Invoke(0);
            // First stop any running fade interpolation for the scene fade canvas group
            InterpolationUtil.StopRunningInterpolation(sceneFadeCanvasGroup, this);
            sceneFadeCanvasGroup.alpha = 0;
            sceneFadeCanvasGroup.gameObject.SetActive(false);
        }

        /// <summary>
        /// Fade the scene out immediately
        /// </summary>
        public void FadeSceneOutImmediate()
        {
            FadingSceneOut.Invoke(0);
            // First stop any running fade interpolation for the scene fade canvas group
            InterpolationUtil.StopRunningInterpolation(sceneFadeCanvasGroup, this);
            sceneFadeCanvasGroup.gameObject.SetActive(true);
            sceneFadeCanvasGroup.alpha = 1;
        }
    }
}
