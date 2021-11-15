using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Manage GUI with game timer countdown and score.
    /// </summary>
    public class GameTimeAndScore : MonoBehaviour
    {
        [Header("GUI")]
        [SerializeField] private GameObject gui;
        [SerializeField] private TMPro.TMP_Text scoreText;
        [SerializeField] private TMPro.TMP_Text timerText;
        [SerializeField] private Color timerTextColor;
        [SerializeField] private Color timerTextColorRed;

        [SerializeField] private GameObject scoreIncrement;
        [SerializeField] private TMPro.TMP_Text scoreIncrementText;
        private CanvasGroup scoreIncrementCanvasGroup;
        private float origScoreIncrementYPos;
        protected const float scoreIncrementFadeDuration = 1f;
        protected const float scoreIncrementYRise = 25f;

        public int gameScore { get; set; } = 0;
        public int gameDuration { get; set; } = 0;
        public int nearGameEndDuration { get; set; } = 0;

        public bool done { get; private set; } = false;
        private float timeStarted = 0f;
        private float curCountdown = 0f;

        private AudioManager audioManager;
        private Fader fader;

        void Awake()
        {
            audioManager = SceneLookup.Get<AudioManager>();
            fader = SceneLookup.Get<Fader>();
        }

        void OnEnable()
        {
            timeStarted = Time.time;

            gui.gameObject.SetActive(false);
            scoreIncrement.gameObject.SetActive(false);
        }

        public void Init(int gameDuration, int nearGameEndDuration)
        {
            this.gameDuration = gameDuration;
            this.nearGameEndDuration = nearGameEndDuration;
            gameScore = 0;
            done = false;
            IncrementScore();
            UpdateGameTimeDisplay();

            origScoreIncrementYPos = scoreIncrement.gameObject.transform.localPosition.y;

            gui.gameObject.SetActive(true);
        }

        void Update()
        {
            if (!done)
            {
                UpdateGameTimeDisplay();
            }
        }

        private void UpdateGameTimeDisplay()
        {
            int numSecsIntoState = (int)(Time.time - timeStarted);
            int latestCountdown = gameDuration - numSecsIntoState;
            if (curCountdown != latestCountdown)
            {
                curCountdown = latestCountdown;

                // SFX
                if (curCountdown > 0f && curCountdown <= 5f)
                {
                    audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Countdown_Timer, volume: 0.5f);
                }
                else if (curCountdown == 0f)
                {
                    audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Timer_Alarm, volume: 0.5f);
                }

                // DONE when countdown has reached and displayed 0
                if (curCountdown < 0)
                {
                    done = true;
                    return;
                }

                string zeroDigitStr = latestCountdown < 10 ? "0" : "";
                timerText.text = ":" + zeroDigitStr + latestCountdown;

                if (curCountdown <= nearGameEndDuration)
                {
                    timerText.color = timerTextColorRed;
                }
                else
                {
                    timerText.color = timerTextColor;
                }
            }
        }

        public void IncrementScore(int scoreIncr = 0)
        {
            gameScore += scoreIncr;

            scoreText.text = gameScore.ToString();

            if (scoreIncr > 0)
            {
                StartCoroutine(ScoreIncrementRoutine(scoreIncr));
            }
        }

        public void SetScore(int score)
        {
            int previousScore = gameScore;
            gameScore = score;
            int scoreChange = gameScore - previousScore;

            scoreText.text = gameScore.ToString();

            if (scoreChange > 0)
            {
                StartCoroutine(ScoreIncrementRoutine(scoreChange));
            }
        }

        private IEnumerator ScoreIncrementRoutine(int scoreIncr)
        {
            scoreIncrementText.text = "+" + scoreIncr;
            Vector3 startPos = scoreIncrement.gameObject.transform.localPosition;
            startPos.y = origScoreIncrementYPos;
            scoreIncrement.gameObject.transform.localPosition = startPos;
            scoreIncrement.gameObject.SetActive(true);

            scoreIncrementCanvasGroup = scoreIncrement.GetComponent<CanvasGroup>();
            scoreIncrementCanvasGroup.alpha = 1f;
            fader.Fade(scoreIncrementCanvasGroup, alpha: 0f, duration: scoreIncrementFadeDuration);

            // animate the text upwards
            float startTime = Time.time;
            Vector3 endPos = new Vector3(startPos.x, startPos.y + scoreIncrementYRise, startPos.z);
            while (Time.time < startTime + scoreIncrementFadeDuration)
            {
                scoreIncrement.gameObject.transform.localPosition = Vector3.Lerp(startPos, endPos,
                    (Time.time - startTime) / scoreIncrementFadeDuration);
                yield return null;
            }

            // put it back in original position when done
            scoreIncrement.gameObject.transform.localPosition = startPos;
        }

    }

}

