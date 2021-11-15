using System;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Animate the SnowballTossButton
    /// Wrapper around SpriteSequencePlayer for the SnowballTossButton
    /// Ignore clicks while animating - which serves as a cooldown so players can't spam the button
    /// </summary>
    [RequireComponent(typeof(Button), typeof(SpriteSequencePlayer))]
    public class SnowballTossButton : MonoBehaviour
    {
        // Invoked when the button is clicked and its time to toss a snowball
        public AppEvent Clicked = new AppEvent();

        private Button button;
        private SpriteSequencePlayer spriteSequencePlayer;

        private Action onAnimationComplete;

        private void Awake()
        {
            button = GetComponent<Button>();
            spriteSequencePlayer = GetComponent<SpriteSequencePlayer>();

            // By default, reset when the animation completes
            SetResetOnAnimationComplete();
        }

        private void OnEnable()
        {
            // Listen for button clicks
            button.onClick.AddListener(OnClick);

            // Show the first frame of the sequence
            spriteSequencePlayer.SetFrame(0, visible: true);
        }

        private void OnDisable()
        {
            // Hide the player
            spriteSequencePlayer.SetVisible(false);

            // Unsubscribe
            button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            // Ignore click while playing
            if (spriteSequencePlayer.IsPlaying)
            {
                return;
            }

            // Invoke the event
            Clicked.Invoke();

            PlayAnimation();
        }

        /// <summary>
        /// Play the animation and run the current onAnimationComplete,
        /// </summary>
        public void PlayAnimation()
        {
            spriteSequencePlayer.Play(loop: false, onComplete: onAnimationComplete);
        }

        /// <summary>
        /// Reset the button. Will return to frame 0 of the animation and show the button.
        /// </summary>
        public void Reset()
        {
            // Show the first frame of the sequence
            spriteSequencePlayer.SetFrame(0, visible: true);
        }

        /// <summary>
        /// Set the button to reset when the animation completes. This is the default button behavior.
        /// </summary>
        public void SetResetOnAnimationComplete()
        {
            SetCustomOnAnimationComplete(Reset);
        }

        /// <summary>
        /// Set a custom action to run when the button animation completes.
        /// </summary>
        /// <param name="onAnimationComplete">The action to perform onAnimationComplete</param>
        public void SetCustomOnAnimationComplete(Action onAnimationComplete)
        {
            this.onAnimationComplete = onAnimationComplete;
        }
    }
}
