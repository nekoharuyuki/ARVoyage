using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Niantic.ARVoyage.SnowballToss
{
    /// <summary>
    /// The main game State in SnowballToss, where the player throws snowballs
    /// through snowrings for points.
    /// It begins by setting the SnowballMaker to active, which will display the 
    /// snowball toss UI button that holds/tosses 3D snowballs.
    /// The state waits until gameTimeAndScoreGUI's timer is done.
    /// Its next state (set via inspector) is StateGameOver.
    /// </summary>
    public class StateTossing : MonoBehaviour
    {
        private SnowballTossManager snowballTossManager;

        [Header("State machine")]
        [SerializeField] private GameObject nextState;
        private bool running;
        private float timeStartedState;
        private GameObject thisState;
        private GameObject exitState;
        protected float initialDelay = 0f;

        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private GameTimeAndScore gameTimeAndScoreGUI;
        private Fader fader;

        private AudioManager audioManager;

        void Awake()
        {
            // We're not the first state; start off disabled
            gameObject.SetActive(false);

            snowballTossManager = SceneLookup.Get<SnowballTossManager>();
            fader = SceneLookup.Get<Fader>();
            audioManager = SceneLookup.Get<AudioManager>();
        }

        void OnEnable()
        {
            thisState = this.gameObject;
            exitState = null;
            Debug.Log("Starting " + thisState);
            timeStartedState = Time.time;

            // initialize game state
            snowballTossManager.InitTossGame();

            // Fade in GUI
            StartCoroutine(DemoUtil.FadeInGUI(gui, fader, initialDelay: initialDelay));

            // create snowball, show snowball toss button
            snowballTossManager.snowballMaker.gameObject.SetActive(true);

            // start game time and score display
            gameTimeAndScoreGUI.Init(SnowballTossManager.gameDuration, SnowballTossManager.nearGameEndDuration);

            running = true;
        }


        void Update()
        {
            if (!running) return;

            // game over?
            if (gameTimeAndScoreGUI.done)
            {
                exitState = nextState;
            }

            if (exitState != null)
            {
                Exit(exitState);
            }

            // Update toss game
            snowballTossManager.UpdateTossGame();
        }


        private void Exit(GameObject nextState)
        {
            running = false;

            StartCoroutine(ExitRoutine(nextState));
        }

        private IEnumerator ExitRoutine(GameObject nextState)
        {
            Debug.Log("Snowball toss game over, score " + snowballTossManager.gameScore);

            // disable snowball and toss button
            snowballTossManager.snowballMaker.Expire();

            // disable snowballMaker
            snowballTossManager.snowballMaker.gameObject.SetActive(false);

            // expire all snowrings
            snowballTossManager.ExpireAllSnowrings();


            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(gui, fader));

            Debug.Log(thisState + " transitioning to " + nextState);

            nextState.SetActive(true);
            thisState.SetActive(false);
        }

    }
}
