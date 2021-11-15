using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Helper class for named interpolation operation keys, including:
    ///  Interpolate linearly over time.
    ///  Interpolate over time, sampling and applying an animation clip at each update.
    ///  Stop any running interpolation for this target and operationKey. 
    /// </summary>
    public class InterpolationOperationKey
    {
        public readonly string Name;

        public InterpolationOperationKey(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class InterpolationUtil
    {
        /// <summary>Interpolate linearly over time.</summary>
        /// <param name="target">The target for the interpolation (e.g. the gameObject).</param>
        /// <param name="operationKey">The key for the type of operation being performed. If an interpolation of the same operation is already running for this target, it will be stopped.</param>
        /// <param name="easingFunc">Easing Function that accepts a progress percentage and returns an eased percentage. If null, will be linear.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="preWait">Wait before starting.</param>
        /// <param name="postWait">Wait after interpolation has completed, before invoking onComplete.</param>
        /// <param name="onStart">Action called immediately after the delay.</param>
        /// <param name="onUpdate">Action called every frame with the current progress. Will always reach 1 if the routine completes.</param>
        /// <param name="onComplete">Action called immediately after progress completion.</param>
        public static Coroutine EasedInterpolation(
            object target,
            object operationKey,
            System.Func<float, float> easingFunc,
            float duration = 1,
            float preWait = 0,
            float postWait = 0,
            Action onStart = null,
            Action<float> onUpdate = null,
            Action onComplete = null)
        {
            // Call LinearInterpolation, applying the easing function to the interpolation
            return LinearInterpolation(target, operationKey, duration, preWait, postWait, onStart,
                onUpdate: (progressPercent) =>
                {
                    float easedProgressPercent = progressPercent;
                    if (easingFunc != null)
                    {
                        easedProgressPercent = Mathf.Clamp01(easingFunc(progressPercent));
                        // Debug.LogFormat("Progress [{0}] Eased [{1}]", progressPercent, easedProgressPercent);
                    }
                    onUpdate?.Invoke(easedProgressPercent);
                },
            onComplete);
        }

        /// <summary>Interpolate linearly over time.</summary>
        /// <param name="target">The target for the interpolation (e.g. the gameObject).</param>
        /// <param name="operationKey">The key for the type of operation being performed. If an interpolation of the same operation is already running for this target, it will be stopped.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="preWait">Wait before starting.</param>
        /// <param name="postWait">Wait after interpolation has finished, before invoking onComplete.</param>
        /// <param name="onStart">Action called immediately after the delay.</param>
        /// <param name="onUpdate">Action called every frame with the current progress. Will always reach 1 if the routine completes.</param>
        /// <param name="onComplete">Action called immediately after progress completion.</param>
        public static Coroutine LinearInterpolation(
            object target,
            object operationKey,
            float duration = 1,
            float preWait = 0,
            float postWait = 0,
            Action onStart = null,
            Action<float> onUpdate = null,
            Action onComplete = null)
        {
            // First stop any running interpolations on this target matching this operation
            // Call this before checking null or destroyed in case this is a non-null, destroyed Unity object
            // This call is null-safe
            StopRunningInterpolation(target, operationKey);

            if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
            {
                Debug.LogWarning("Ignoring call to LinearInterpolation with null or destroyed target for operation " + operationKey);
                return null;
            }

            // Start the routine and get the Coroutine variable
            Coroutine routine = Runner.StartCoroutine(LinearInterpolationRoutine(target, operationKey, duration, preWait, postWait, onStart, onUpdate, onComplete));

            if (routine != null)
            {
                AddRunningInterpolation(target, operationKey, routine);
            }

            return routine;
        }

        /// <summary>Interpolate over time, sampling and applying an animation clip at each update.</summary>
        /// <param name="target">The gameObject target to play the animation on during the interpolation.</param>
        /// <param name="operationKey">The key for the type of operation being performed. If an interpolation of the same operation is already running for this target, it will be stopped.</param>
        /// <param name="animationClip">The animation clip to sample. This will be applied directly to the target gameObject.</param>
        /// <param name="playForwards">Should the clip be played forwards? If false, will play in reverse.</param>
        /// <param name="duration">Duration in seconds. Defaults to -1, which will use the length of the clip.</param>
        /// <param name="preWait">Wait before starting.</param>
        /// <param name="postWait">Wait after interpolation has completed, before invoking onComplete.</param>
        /// <param name="onStart">Action called immediately after the delay.</param>
        /// <param name="onUpdate">Action called every frame with the current progress after the animation clip is sampled. Will always reach 1 if the routine completes.</param>
        /// <param name="onComplete">Action called immediately after progress completion.</param>
        public static Coroutine AnimationClipInterpolation(
            GameObject target,
            object operationKey,
            AnimationClip animationClip,
            bool playForwards = true,
            float duration = -1,
            float preWait = 0,
            float postWait = 0,
            Action onStart = null,
            Action<float> onUpdate = null,
            Action onComplete = null)
        {
            if (Mathf.Approximately(duration, -1f))
            {
                duration = animationClip.length;
            }

            return LinearInterpolation(
                target,
                operationKey,
                duration,
                preWait,
                postWait,
                onStart,
                onUpdate: (float progressPercent) =>
                {
                    float animationPercent = playForwards ? progressPercent : 1 - progressPercent;
                    animationClip.SampleAnimation(target, animationPercent * animationClip.length);
                    onUpdate?.Invoke(progressPercent);
                },
                onComplete
            );
        }

        /// <summary>Stop any running interpolation for this target and operationKey. Safe to call if no interpolation is running.</summary>
        /// <param name="target">The target for the interpolation (e.g. the gameObject).</param>
        /// <param name="operationKey">The key for the type of operation being performed.</param>
        public static void StopRunningInterpolation(object target, object operationKey)
        {
            Coroutine runningInterpolationRoutine = RemoveRunningInterpolation(target, operationKey);
            if (runningInterpolationRoutine != null)
            {
                Debug.LogFormat("StopRunningInterpolation [operation {0}] [target {1}]", target, operationKey);
                Runner.StopCoroutine(runningInterpolationRoutine);
            }
        }

        #region internalLogic

        public class InterpolationRoutineRunner : MonoBehaviour { }

        // This InterpolationRoutineRunner is used for all interpolation routines.
        // It will be added to the scene on demand if one doesn't exist
        // The InterpolationRoutineRunner gameObject will be destroyed when leaving the current scene, preventing routines from running across scenes
        private static InterpolationRoutineRunner runner;
        private static InterpolationRoutineRunner Runner
        {
            get
            {
                if (runner == null)
                {
                    runner = new GameObject(typeof(InterpolationRoutineRunner).Name).AddComponent<InterpolationRoutineRunner>();
                }
                return runner;
            }
        }

        // Mapping to track all running routines, so that routines that are targeting an object will be automatically stopped if 
        // a new interpolation using the same operationKey is started for the target.
        // The structure of the dictionary is target -> Dictionary<operationKey, Coroutine>
        // object is used for maximum flexibility
        private static Dictionary<object, Dictionary<object, Coroutine>> runningInterpolations = new Dictionary<object, Dictionary<object, Coroutine>>();

        static InterpolationUtil()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            // Clear the running routines on scene unloaded
            runningInterpolations.Clear();
        }

        // Add a running interpolation coroutine for this target and operationKey
        private static void AddRunningInterpolation(object target, object operationKey, Coroutine coroutine)
        {
            if (target == null)
            {
                Debug.LogWarning("Ignoring AddRunningInterpolation with null target");
                return;
            }

            // Get the running interpolations for this target
            runningInterpolations.TryGetValue(target, out Dictionary<object, Coroutine> runningInterpolationsForTarget);

            // If there is no dictionary for the target, add one
            if (runningInterpolationsForTarget == null)
            {
                runningInterpolationsForTarget = new Dictionary<object, Coroutine>();
                runningInterpolations.Add(target, runningInterpolationsForTarget);
            }

            // Add this operation and its routine
            runningInterpolationsForTarget.Add(operationKey, coroutine);
        }

        // Remove any running interpolation for this target and operationKey
        private static Coroutine RemoveRunningInterpolation(object target, object operationKey)
        {
            if (target == null)
            {
                return null;
            }

            // Get and remove the running interpolation routine for this target
            if (runningInterpolations.TryGetValue(target, out Dictionary<object, Coroutine> runningInterpolationsForTarget))
            {
                if (runningInterpolationsForTarget.TryGetValue(operationKey, out Coroutine operationRoutine))
                {
                    if (operationRoutine != null)
                    {
                        runningInterpolationsForTarget.Remove(operationKey);
                        return operationRoutine;
                    }
                }
            }

            return null;
        }

        private static IEnumerator LinearInterpolationRoutine(object target, object operationKey, float duration, float preWait, float postWait, Action onStart, Action<float> onUpdate, Action onComplete)
        {
            if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
            {
                Debug.LogWarning("Bailing on LinearInterpolationRoutine with null or destroyed target for operation " + operationKey);
                RemoveRunningInterpolation(target, operationKey);
                yield break;
            }

            if (preWait > 0)
            {
                float waitEndTime = Time.time + preWait;
                while (Time.time < waitEndTime)
                {
                    yield return null;
                }
            }

            onStart?.Invoke();

            // If this is a 0-duration interpolation, ensure onUpdate is invoked
            if (duration <= 0)
            {
                if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
                {
                    Debug.LogWarning("Bailing on LinearInterpolationRoutine with null or destroyed target for operation " + operationKey);
                    RemoveRunningInterpolation(target, operationKey);
                    yield break;
                }
                onUpdate?.Invoke(0);
            }
            // Otherwise run the interpolation over time
            else
            {
                float startTime = Time.time;
                float endTime = startTime + duration;

                while (Time.time < endTime)
                {
                    if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
                    {
                        Debug.LogWarning("Bailing on LinearInterpolationRoutine with null or destroyed target for operation " + operationKey);
                        RemoveRunningInterpolation(target, operationKey);
                        yield break;
                    }
                    float progressPercent = Mathf.Clamp01((Time.time - startTime) / duration);
                    onUpdate?.Invoke(progressPercent);
                    yield return null;
                }
            }

            if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
            {
                Debug.LogWarning("Bailing on LinearInterpolationRoutine with null or destroyed target for operation " + operationKey);
                RemoveRunningInterpolation(target, operationKey);
                yield break;
            }

            // Ensure the final onUpdate is invoked for full progress
            onUpdate?.Invoke(1);

            if (postWait > 0)
            {
                float waitEndTime = Time.time + postWait;
                while (Time.time < waitEndTime)
                {
                    yield return null;
                }
            }

            // Remove running operation before invoking OnComplete
            RemoveRunningInterpolation(target, operationKey);

            onComplete?.Invoke();
        }
        #endregion internalLogic

        #region Easing
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }

        public static float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }
        #endregion // Easing
    }
}