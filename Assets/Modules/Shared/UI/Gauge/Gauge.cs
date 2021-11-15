using UnityEngine;
using UnityEngine.UI;
using System;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Animating progress gauge, used in various demos.
    /// </summary>
    [RequireComponent(typeof(SpriteSequencePlayer))]
    public class Gauge : MonoBehaviour, ISceneDependency
    {
        [SerializeField] private int fillStartFrameIndex;
        [SerializeField] private int fillEndFrameIndex;
        [SerializeField] private int completedStartFrameIndex;
        [SerializeField] private int completedEndFrameIndex;
        [SerializeField] private int resetStartFrameIndex;
        [SerializeField] private int resetEndFrameIndex;

        [SerializeField] private bool showFillStartFrameOnEnable = true;

        [SerializeField] private Image iconImage;

        [Space]
        [SerializeField] private float testPercent;

        private int FillFrameRange => fillEndFrameIndex - fillStartFrameIndex;

        private SpriteSequencePlayer spriteSequencePlayer;

        private void Awake()
        {
            spriteSequencePlayer = GetComponent<SpriteSequencePlayer>();
            spriteSequencePlayer.loop = false;
            spriteSequencePlayer.playOnEnable = false;
        }

        private void OnEnable()
        {
            if (showFillStartFrameOnEnable)
            {
                spriteSequencePlayer.SetFrame(fillStartFrameIndex, visible: true);
            }
        }

        public Coroutine FillToPercent(float percent)
        {
            gameObject.SetActive(true);
            int frameForPercent = fillStartFrameIndex + Mathf.RoundToInt(Mathf.Clamp01(percent) * FillFrameRange);
            // Debug.LogFormat("FillToPercent [percent {0}] [frame {1}]", percent, frameForPercent);

            if (spriteSequencePlayer.IsPlaying)
            {
                return spriteSequencePlayer.UpdateLastFrameInCurrentPlay(frameForPercent);
            }
            else
            {
                // If the player is within the fill frame range, start on the current frame, otherwise start from the first fill frame
                int startFrame = spriteSequencePlayer.CurrentFrame;
                if (startFrame < fillStartFrameIndex || fillEndFrameIndex < startFrame)
                {
                    startFrame = fillStartFrameIndex;
                }

                return spriteSequencePlayer.Play(startFrame, frameForPercent, loop: false, visibleAtEnd: true);
            }
        }

        public Coroutine PlayCompletedSequence(bool visibleAtEnd = true, Action onComplete = null)
        {
            gameObject.SetActive(true);

            return spriteSequencePlayer.Play(completedStartFrameIndex, completedEndFrameIndex,
                visibleAtEnd: visibleAtEnd,
                onComplete: onComplete);
        }

        public Coroutine PlayResetSequence(bool visibleAtEnd = true, bool returnToFillStartFrame = true, Action onComplete = null)
        {
            gameObject.SetActive(true);

            return spriteSequencePlayer.Play(resetStartFrameIndex, resetEndFrameIndex,
                visibleAtEnd: visibleAtEnd,
                onComplete: () =>
                {
                    if (returnToFillStartFrame)
                    {
                        // If specified, return to the fill start on complete
                        spriteSequencePlayer.SetFrame(fillStartFrameIndex, visible: visibleAtEnd);
                    }
                    onComplete?.Invoke();
                });
        }

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }

        // Comment in to test
        // void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.F))
        //     {
        //         FillToPercent(testPercent);
        //     }

        //     if (Input.GetKeyDown(KeyCode.C))
        //     {
        //         PlayCompletedSequence();
        //     }

        //     if (Input.GetKeyDown(KeyCode.R))
        //     {
        //         PlayResetSequence();
        //     }
        // }
    }
}
