using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Component used for testing the BubbleScaleUtil
    /// </summary>
    public class BubbleScaleTester : MonoBehaviour
    {
        [Tooltip("Should the tester automatically run a scale test loop? Otherwise, use the spacebar to trigger a scale up/down.")]
        public bool autoRunScaleLoop;

        [Tooltip("The Gameobject to scale.")]
        public GameObject target;

        public BubbleScaleAnimationType animationType;

        public float scaleUpTarget = 1f;
        public float scaleDownTarget = 0f;

        private bool scaleUp;

        private void OnEnable()
        {
            if (autoRunScaleLoop)
            {
                StartCoroutine(AutoScaleLoopRoutine());
            }
        }

        private IEnumerator AutoScaleLoopRoutine()
        {
            while (true)
            {
                yield return BubbleScaleUtil.ScaleUp(target);
                yield return new WaitForSeconds(.5f);
                yield return BubbleScaleUtil.ScaleDown(target);
                yield return new WaitForSeconds(.5f);
            }
        }

        void Update()
        {
            if (!autoRunScaleLoop)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    scaleUp = !scaleUp;

                    if (scaleUp)
                    {
                        BubbleScaleUtil.ScaleUp(target, animationType: animationType, targetScale: scaleUpTarget);
                    }
                    else
                    {
                        BubbleScaleUtil.ScaleDown(target, animationType: animationType, targetScale: scaleDownTarget);
                    }
                }
            }
        }

    }
}
