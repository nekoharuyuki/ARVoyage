using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Common button events used in various demos' GUIs.
    /// Each scene's events class inherits from this class.
    /// </summary>
    public class DemoEvents : MonoBehaviour
    {
        public static AppEvent EventStartButton = new AppEvent();
        public static AppEvent EventWarningOkButton = new AppEvent();
        public static AppEvent EventNextButton = new AppEvent();
        public static AppEvent EventRestartButton = new AppEvent();

        protected AudioManager audioManager;

        public void StartButtonPressed()
        {
            EventStartButton.Invoke();
            ButtonSFX();
        }

        public void WarningOkButtonPressed()
        {
            EventWarningOkButton.Invoke();
            ButtonSFX();
        }

        public void NextButtonPressed()
        {
            EventNextButton.Invoke();
            ButtonSFX();
        }

        public void RestartButtonPressed()
        {
            EventRestartButton.Invoke();
            ButtonSFX();
        }

        protected void ButtonSFX(string audioKey = AudioKeys.UI_Button_Press)
        {
            audioManager = SceneLookup.Get<AudioManager>();
            audioManager?.PlayAudioNonSpatial(audioKey);
        }
    }
}
