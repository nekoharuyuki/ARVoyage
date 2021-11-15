using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Screen-space overlay of frost, displayed when a player is hit by
    /// another player's snowball in the SnowballFight demo.
    /// </summary>
    public class FrostOverlayEffect : MonoBehaviour, ISceneDependency
    {
        [SerializeField] float showDuration = .2f;
        [SerializeField] float hideDuration = 2f;

        [SerializeField] float pulseDuration = .8f;
        [SerializeField] AnimationCurve pulseAnimationCurve;

        [SerializeField] RawImage image;

        public void Start()
        {
            image.gameObject.SetActive(false);
            image.material.SetFloat("_Reveal", 0);
        }

        public void OnDestroy()
        {
            image.material.SetFloat("_Reveal", 0);
        }

        public void PulseFrost()
        {
            PulseFrostRoutine();
        }

        public Coroutine PulseFrostRoutine()
        {

            return InterpolationUtil.LinearInterpolation(gameObject, gameObject,
                pulseDuration,
                onStart: () =>
                {
                    image.gameObject.SetActive(true);
                },
                onUpdate: (t) =>
                {
                    float value = pulseAnimationCurve.Evaluate(t);
                    image.material.SetFloat("_Reveal", value);
                },
                onComplete: () =>
                {
                    image.gameObject.SetActive(false);
                }
            );
        }

        public void ShowFrost(bool show)
        {
            ShowFrostRoutine(show);
        }

        public Coroutine ShowFrostRoutine(bool show)
        {
            float duration = (show) ? showDuration : hideDuration;
            float startReveal = image.material.GetFloat("_Reveal");
            float endReveal = (show) ? 1 : 0;

            return InterpolationUtil.EasedInterpolation(gameObject, gameObject,
                InterpolationUtil.EaseInOutCubic,
                duration,
                onStart: () =>
                {
                    image.gameObject.SetActive(true);
                },
                onUpdate: (t) =>
                 {
                     float reveal = Mathf.Lerp(startReveal, endReveal, t);
                     image.material.SetFloat("_Reveal", reveal);
                 },
                onComplete: () =>
                 {
                     image.gameObject.SetActive(show);
                 }
            );
        }

#if UNITY_EDITOR && FALSE
        void OnGUI()
        {
            if (GUILayout.Button("Freeze"))
            {
                ShowFrost(true);
            }

            if (GUILayout.Button("Unfreeze"))
            {
                ShowFrost(false);
            }

            if (GUILayout.Button("Pulse"))
            {
                PulseFrost();
            }
        }

#endif
    }

}
