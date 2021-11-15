using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Display a 3-second animated countdown.
    /// Used by multiple demos including SnowballToss and SnowballFight.
    /// Its next state is set via inspector, custom to that demo.
    /// </summary>
    public class StateCountdown : MonoBehaviour
    {
        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0f;


        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private SpriteSequencePlayer countdownPlayer;
        private Fader fader;

        private AudioManager audioManager;


        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            fader = SceneLookup.Get<Fader>();
            audioManager = SceneLookup.Get<AudioManager>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // Start the countdown
            countdownPlayer.currentFrameChanged.AddListener(OnCountdownFrameChanged);
            countdownPlayer.Play(loop: false, onComplete: OnCountdownComplete);

            running = true;
        }

        void OnDisable()
        {
            countdownPlayer.currentFrameChanged.RemoveListener(OnCountdownFrameChanged);
        }

        void Update()
        {
            if (!running) return;

            if (exitState != null)
            {
                Exit(exitState);
                return;
            }
        }

        private void OnCountdownFrameChanged(int frame)
        {
            // Play countdown sfx on frames where the countdown number appears
            if (frame == 1 ||
                frame == 6 ||
                frame == 11
            )
            {
                audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Countdown_Timer);
            }
        }

        private void OnCountdownComplete()
        {
            audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Countdown_Timer_End);
            exitState = nextState;
        }

        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
