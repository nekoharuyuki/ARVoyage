using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Add this component to an image to play a sprite sequence on it.
    /// Add the sprites to the component's sprites array; they must be set to Sprite or 2D.
    /// Includes:
    ///  Play the full sequence, forward or reverse.
    ///  Play the sequence between specified indices, inclusive.
    /// </summary>
    public class SpriteSequencePlayer : MonoBehaviour
    {
        public AppEvent<int> currentFrameChanged = new AppEvent<int>();

        [Tooltip("Sequence frames per second")]
        [SerializeField] public float fps = 30;

        [Tooltip("Start playing on enable?")]
        [SerializeField] public bool playOnEnable;

        [Tooltip("Should playback loop?")]
        public bool loop = true;

        [Tooltip("The sprite array")]
        [SerializeField] public Sprite[] sprites;

        // Is the player currently playing?
        public bool IsPlaying => playRoutine != null;

        // How many sprites in the sequence?
        public int NumSprites => sprites != null ? sprites.Length : 0;

        // Final frame in the sequence
        public int FinalFrame => sprites != null ? NumSprites - 1 : 0;

        // What is the current frame index?
        public int CurrentFrame { get; private set; } = 0;


        private Image sequenceImage;

        private Coroutine playRoutine;

        private bool visibleOnAwake;
        private int frameOnAwake;

        private void Awake()
        {
            sequenceImage = GetComponent<Image>();
            if (sequenceImage != null)
            {
                Debug.Log(this.name + " using image component");
            }
            else
            {
                Debug.LogError(typeof(SpriteSequencePlayer).Name + " didn't find image component");
            }

            // Initialize to the desired frame and visibility
            SetFrame(frameOnAwake, visibleOnAwake);
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play(loop);
            }
        }

        private void OnDisable()
        {
            Stop(isVisible: false);
        }

        /// <summary>
        /// Set the sequence to a specified frame index. Stops any running play.
        /// </summary>
        /// <param name="frameIndex">The frame index to set</param>
        /// <param name="visible">Shoudl the player be visible?</param>
        /// <returns>The playback coroutine</returns>
        public void SetFrame(int frameIndex, bool visible)
        {
            Stop(false);

            if (frameIndex < 0 || sprites.Length <= frameIndex)
            {
                Debug.LogError(name + " SetFrame got out of bounds index: " + frameIndex);
                frameIndex = Mathf.Clamp(frameIndex, 0, sprites.Length - 1);
            }

            SetCurrentFrame(frameIndex);
            SetVisible(visible);

        }

        /// <summary>
        /// Play the full sequence
        /// </summary>
        /// <param name="loop">Should playback loop?</param>
        /// <param name="onComplete">Optional action to perform at end if not looping. This is invoked after the sequence is stopped and hidden.It is safe to call SetFrame within.</param>
        /// <returns>The playback coroutine</returns>
        public Coroutine Play(bool loop, Action onComplete = null)
        {
            return Play(0, FinalFrame, loop, onComplete: onComplete);
        }

        /// <summary>
        /// Play the full sequence in reverse
        /// </summary>
        /// <param name="loop">Should playback loop?</param>
        /// <param name="onComplete">Optional action to perform at end if not looping. This is invoked after the sequence is stopped and hidden.It is safe to call SetFrame within.</param>
        /// <returns>The playback coroutine</returns>
        public Coroutine PlayReverse(bool loop, Action onComplete = null)
        {
            return Play(FinalFrame, 0, loop, onComplete: onComplete);
        }

        /// <summary>
        /// Play the sequence between specified indices, inclusive.
        /// </summary>
        /// <param name="firstFrame">First frame index</param>
        /// <param name="lastFrame">Last frame index</param>
        /// <param name="loop">Should playback loop? Defaults to false.</param>
        /// <param name="visibleAtEnd">Should the final frame remain visible fter playback? Defaults to false.</param>
        /// <param name="onComplete">Optional action to perform at end if not looping. This is invoked after the sequence is stopped and hidden.It is safe to call SetFrame within.</param>
        /// <returns>The playback coroutine</returns>
        public Coroutine Play(int firstFrame, int lastFrame, bool loop = false, bool visibleAtEnd = false, Action onComplete = null)
        {
            // Stop any running play
            if (IsPlaying)
            {
                StopCoroutine(playRoutine);
            }

            this.loop = loop;

            playRoutine = StartCoroutine(PlayRoutine(firstFrame, lastFrame, visibleAtEnd, onComplete));
            return playRoutine;
        }

        /// <summary>
        /// Stop playback. Safe to call if not currently playing.
        /// </summary>
        /// <param name="isVisible">Should the frame be visible?</param>
        public void Stop(bool isVisible)
        {
            SetVisible(isVisible);

            if (IsPlaying)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }
        }
        /// <summary>
        /// Set the player's visibility.
        /// </summary>
        /// <param name="visible">Should the player be visible?</param>
        public void SetVisible(bool visible)
        {
            if (sequenceImage != null)
            {
                sequenceImage.enabled = visible;
            }
            // If image is null because this component hasn't yet reached awake, queue the visibility set
            else
            {
                visibleOnAwake = visible;
            }
        }

        private void SetCurrentFrame(int currentFrame)
        {
            if (sequenceImage != null)
            {
                if (this.CurrentFrame != currentFrame)
                {
                    CurrentFrame = currentFrame;
                    sequenceImage.sprite = sprites[currentFrame];
                    currentFrameChanged.Invoke(CurrentFrame);
                }
            }
            // If image is null because this component hasn't yet reached awake, queue the frame set
            else
            {
                frameOnAwake = currentFrame;
            }
        }

        private int currentPlayFirstFrame;
        private int currentPlayNumFrames;
        private bool currentPlayForward;

        /// <summary>
        /// Update the last frame in the currently playing routine.
        /// This doesn't currently support modifying the play direction.
        /// This call will be ignored if not currently playing.
        /// </summary>
        /// <param name="lastFrame">The new last frame. If before CurrentFrame, CurrentFrame will be used.</param>
        /// <returns>The playback coroutine</returns>
        public Coroutine UpdateLastFrameInCurrentPlay(int lastFrame)
        {
            if (!IsPlaying)
            {
                Debug.LogWarning("Ignoring call to UpdateLastFrameInCurrentPlay while not playing");
                return null;
            }

            if ((currentPlayForward && lastFrame < CurrentFrame) ||
                (!currentPlayForward && lastFrame > CurrentFrame)
            )
            {
                Debug.LogWarningFormat("UpdateLastFrameInCurrentPlay does not currently support reversing play direction. [CurrentFirstFrame {0}] [CurrentForward? {1}] [CurrentFrame {2}] [New LastFrame {3}]",
                    currentPlayFirstFrame,
                    currentPlayForward,
                    CurrentFrame,
                    lastFrame
                );
                return null;
            }

            if (lastFrame < 0 || lastFrame > FinalFrame)
            {
                int invalidLastFrame = lastFrame;
                lastFrame = Mathf.Clamp(lastFrame, 0, FinalFrame);
                Debug.LogError(name + " got invalid last frame " + invalidLastFrame + ". Defaulting to " + lastFrame);
            }

            // If new last frame is before the current frame based on the direction, just use CurrentFrame
            if ((currentPlayForward && lastFrame < CurrentFrame) ||
                    (!currentPlayForward && (lastFrame > CurrentFrame)))
            {
                Debug.Log("UpdateLastFrameInCurrentPlay got lastFrame before CurrentFrame. Using CurrentFrame");
                lastFrame = CurrentFrame;
            }

            this.currentPlayNumFrames = Mathf.Abs(currentPlayFirstFrame - lastFrame) + 1;
            return playRoutine;
        }

        private IEnumerator PlayRoutine(int firstFrame, int lastFrame, bool visibleAtEnd, Action onComplete)
        {
            if (firstFrame < 0 || firstFrame > FinalFrame)
            {
                int invalidFirstFrame = firstFrame;
                firstFrame = Mathf.Clamp(firstFrame, 0, FinalFrame);
                Debug.LogError(name + " got invalid first frame " + invalidFirstFrame + ". Defaulting to " + firstFrame);
            }

            if (lastFrame < 0 || lastFrame > FinalFrame)
            {
                int invalidLastFrame = lastFrame;
                lastFrame = Mathf.Clamp(firstFrame, 0, FinalFrame);
                Debug.LogError(name + " got invalid last frame " + invalidLastFrame + ". Defaulting to " + lastFrame);
            }

            this.currentPlayFirstFrame = firstFrame;

            int frameCounter = firstFrame;
            float startPlayTime = Time.time;
            this.currentPlayNumFrames = Mathf.Abs(firstFrame - lastFrame) + 1;

            // If the request is for one or fewer frames, just set the last frame and bail
            if (currentPlayNumFrames <= 1)
            {
                SetCurrentFrame(lastFrame);
                Stop(isVisible: visibleAtEnd);
                onComplete?.Invoke();
                yield break;
            }

            // Determine play direction
            currentPlayForward = lastFrame > firstFrame;

            SetVisible(true);

            SetCurrentFrame(frameCounter);

            while (true)
            {
                float timePlayed = Time.time - startPlayTime;
                float playDuration = currentPlayNumFrames / fps;
                float percentPlayed = timePlayed / playDuration;

                if (percentPlayed >= 1.0f)
                {
                    // If looping, reset
                    if (loop)
                    {
                        startPlayTime = Time.time;
                        percentPlayed = 0f;
                    }
                    // Otherwise, break out
                    else
                    {
                        break;
                    }
                }

                int frameToPlay;

                if (currentPlayForward)
                {
                    // get the frame corresponding to the percent of duration played
                    float percentFrame = firstFrame + percentPlayed * currentPlayNumFrames;

                    // round down to the frame to play. This allows the final frame to play fully.
                    frameToPlay = Mathf.FloorToInt(percentFrame);
                }
                else
                {
                    // get the frame corresponding to the percent of duration played
                    float percentFrame = firstFrame - percentPlayed * currentPlayNumFrames;

                    // round up to the frame to play. This allows the final frame to play fully.
                    frameToPlay = Mathf.CeilToInt(percentFrame);
                }

                // if it's a new frame, then update the sprite
                if (frameToPlay != frameCounter)
                {
                    frameCounter = frameToPlay;
                    SetCurrentFrame(frameCounter);
                }

                yield return null;
            }

            Stop(isVisible: visibleAtEnd);
            onComplete?.Invoke();
        }
    }
}