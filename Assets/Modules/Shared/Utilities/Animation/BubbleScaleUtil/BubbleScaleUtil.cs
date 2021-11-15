using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Niantic.ARVoyage
{
    public enum BubbleScaleAnimationType
    {
        Default = 0,
        Quick
    }

    /// <summary>
    /// "Bubble"-style scaling up and down of game objects. Used for both 3D world-space objects and 2D screen-space UIs.
    /// </summary>
    public static class BubbleScaleUtil
    {
        private static readonly InterpolationOperationKey BubbleScaleOperationKey = new InterpolationOperationKey(typeof(BubbleScaleUtil).Name + "Key");

        /// <summary>Bubble-scale up over time, starting from current scale. If the target's current scale is already greater than or equal to the targetScale, this will complete immediately without processing waits.</summary>
        /// <param name="target">The gameObject to scale up.</param>
        /// <param name="targetScale">The target uniform scale. This call will be ignored if target is lower than current scale. Any running BubbleScale will be stopped.</param>
        /// <param name="duration">Duration in seconds. Defaults to -1, which will use the length of the clip.</param>
        /// <param name="animationType">Which animation type to use</param>        
        /// <param name="preWait">Wait before starting, if this is a valid scaling.</param>
        /// <param name="postWait">Wait after scale has completed, before invoking onComplete, if this is a valid scaling.</param>
        /// <param name="onStart">Action called immediately after the delay.</param>
        /// <param name="onUpdate">Action called every frame with the current progress after the animation clip is sampled. Will always reach 1 if the routine completes.</param>
        /// <param name="onComplete">Action called immediately after progress completion.</param>
        /// <param name="activateTargetOnStart">Should the target gameObject be activated before scaling up?</param>
        public static Coroutine ScaleUp(
            GameObject target,
            float targetScale = 1f,
            float duration = -1f,
            BubbleScaleAnimationType animationType = BubbleScaleAnimationType.Default,
            float preWait = 0,
            float postWait = 0,
            Action onStart = null,
            Action<float> onUpdate = null,
            Action onComplete = null,
            bool activateTargetOnStart = true)
        {
            if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
            {
                Debug.LogWarning("Ignoring call to ScaleUp with null or destroyed target");
                return null;
            }

            float startingScale = target.transform.localScale.x;

            // If target scale is less than or equal to the starting scale, can't scale up to target, so complete immediately
            if (targetScale <= startingScale)
            {
                Debug.LogFormat("Immediately completing call to ScaleUp with targetScale <= startingScale [startingScale {0}] [targetScale {1}] [target {2}]",
                    startingScale,
                    targetScale,
                    target.name
                );

                // Invoke events immediately, and return
                if (activateTargetOnStart)
                {
                    target.SetActive(true);
                }

                onStart?.Invoke();
                onUpdate?.Invoke(1);
                onComplete?.Invoke();

                // Stop any running interpolation if there is one
                InterpolationUtil.StopRunningInterpolation(target, operationKey: BubbleScaleOperationKey);

                return null;
            }

            // When scaling up, use target-start as the diff
            float scaleDiff = targetScale - startingScale;

            return InterpolationUtil.AnimationClipInterpolation(
                target,
                operationKey: BubbleScaleOperationKey,
                GetAnimationClipForType(animationType),
                playForwards: true, duration, preWait, postWait,
                onStart: () =>
                {
                    if (activateTargetOnStart)
                    {
                        target.SetActive(true);
                    }
                    onStart?.Invoke();
                },
                onUpdate: (float durationProgress) =>
                {
                    // The scale of the clip is relative to a 0-1 scale, that will already have been applied to the GameObject.
                    float clipScale = target.transform.localScale.x;
                    float scaleToApply = Mathf.Max(0f, startingScale + clipScale * scaleDiff);
                    target.transform.localScale = new Vector3(scaleToApply, scaleToApply, scaleToApply);

                    onUpdate?.Invoke(durationProgress);
                },
                onComplete);
        }

        /// <summary>Bubble-scale up over time, starting from current scale. If the target's current scale is already less than or equal to the targetScale, this will complete immediately without processing waits.</summary>
        /// <param name="target">The gameObject to scale up.</param>
        /// <param name="targetScale">The target uniform scale. This call will be ignored if target is higher than current scale. Any running BubbleScale will be stopped.</param>
        /// <param name="duration">Duration in seconds. Defaults to -1, which will use the length of the clip.</param>
        /// <param name="animationType">Which animation to use</param>        
        /// <param name="preWait">Wait before starting, if this is a valid scaling.</param>
        /// <param name="postWait">Wait after scale has completed, before invoking onComplete, if this is a valid scaling.</param>
        /// <param name="onStart">Action called immediately after the delay.</param>
        /// <param name="onUpdate">Action called every frame with the current progress after the animation clip is sampled. Will always reach 1 if the routine completes.</param>
        /// <param name="onComplete">Action called immediately after progress completion.</param>
        /// <param name="deactivateTargetOnComplete">Should the target gameObject be deactivated after scaling down?</param>
        public static Coroutine ScaleDown(
            GameObject target,
            float targetScale = 0f,
            float duration = -1f,
            BubbleScaleAnimationType animationType = BubbleScaleAnimationType.Default,
            float preWait = 0,
            float postWait = 0,
            Action onStart = null,
            Action<float> onUpdate = null,
            Action onComplete = null,
            bool deactivateTargetOnComplete = false)
        {
            if (DemoUtil.IsNullOrIsDestroyedUnityObject(target))
            {
                Debug.LogWarning("Ignoring call to ScaleDown with null or destroyed target");
                return null;
            }

            float startingScale = target.transform.localScale.x;

            // If target scale is greater than or equal to the starting scale, can't scale down to target, so complete immediately
            if (targetScale >= startingScale)
            {
                Debug.LogFormat("Immediately completing call to ScaleDown with targetScale >= startingScale [startingScale {0}] [targetScale {1}] [target {2}]",
                    startingScale,
                    targetScale,
                    target.name
                );

                // Invoke completion events immediately, and return
                onStart?.Invoke();
                onUpdate?.Invoke(1);

                if (deactivateTargetOnComplete)
                {
                    target.SetActive(false);
                }

                onComplete?.Invoke();

                // Stop any running interpolation if there is one
                InterpolationUtil.StopRunningInterpolation(target, operationKey: BubbleScaleOperationKey);

                return null;
            }

            // When scaling down, use start - target as the diff
            float scaleDiff = startingScale - targetScale;

            return InterpolationUtil.AnimationClipInterpolation(
                target,
                operationKey: BubbleScaleOperationKey,
                animationClip: GetAnimationClipForType(animationType),
                playForwards: false, duration, preWait, postWait, onStart,
                onUpdate: (float durationProgress) =>
                {
                    // The scale of the clip is relative to a 0-1 scale, that will already have been applied to the GameObject.
                    float clipScale = target.transform.localScale.x;
                    float scaleToApply = Mathf.Max(0f, targetScale + clipScale * scaleDiff);
                    target.transform.localScale = new Vector3(scaleToApply, scaleToApply, scaleToApply);

                    onUpdate?.Invoke(durationProgress);
                },
                onComplete: () =>
                {
                    if (deactivateTargetOnComplete)
                    {
                        target.SetActive(false);
                    }
                    onComplete?.Invoke();
                });
        }

        /// <summary>Stop any running bubble-scale on this object. Safe to call if there is no running BubbleScale.</summary>
        /// <param name="target">The gameObject to scale up.</param>
        public static void StopRunningScale(GameObject target)
        {
            InterpolationUtil.StopRunningInterpolation(target, operationKey: BubbleScaleOperationKey);
        }

        #region internalLogic

        // This clip is used both for scaling up and down.
        private const string BubbleScaleDefaultClipName = "BubbleScaleUp";

        private static AnimationClip GetAnimationClipForType(BubbleScaleAnimationType animation)
        {
            switch (animation)
            {
                case BubbleScaleAnimationType.Quick:
                    return BubbleScaleQuickClip;
                default:
                    return BubbleScaleDefaultClip;
            }
        }

        private static AnimationClip bubbleScaleDefaultClip;
        private static AnimationClip BubbleScaleDefaultClip
        {
            get
            {
                if (bubbleScaleDefaultClip == null)
                {
                    bubbleScaleDefaultClip = Resources.Load<AnimationClip>(BubbleScaleDefaultClipName);
                }
                return bubbleScaleDefaultClip;
            }
        }

        private const string BubbleScaleQuickClipName = "BubbleScaleAlt";
        private static AnimationClip bubbleScaleQuickClip;
        private static AnimationClip BubbleScaleQuickClip
        {
            get
            {
                if (bubbleScaleQuickClip == null)
                {
                    bubbleScaleQuickClip = Resources.Load<AnimationClip>(BubbleScaleQuickClipName);
                }
                return bubbleScaleQuickClip;
            }
        }
        #endregion internalLogic

    }
}
