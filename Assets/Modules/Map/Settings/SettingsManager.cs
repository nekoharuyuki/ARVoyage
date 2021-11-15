using UnityEngine;
using Niantic.ARDK.Utilities.VersionUtilities;

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// Manages functionality for the map settings UI
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [SerializeField] private GameObject settingsGUI;
        [SerializeField] private GameObject settingsBackdrop;
        [SerializeField] private TMPro.TMP_Text ardkVersionText;
        [SerializeField] private GameObject resetProgressGUI;

        protected AudioManager audioManager;

        public void OnSettingsButton()
        {
            ardkVersionText.text = "ARDK Version: " + ARDKGlobalVersion.GetARDKVersion();
            settingsBackdrop.gameObject.SetActive(true);
            settingsGUI.gameObject.SetActive(true);
            ButtonSFX();
        }

        public void OnSettingsXCloseButton()
        {
            settingsBackdrop.gameObject.SetActive(false);
            settingsGUI.gameObject.SetActive(false);
            ButtonSFX(AudioKeys.UI_Close_Window);
        }

        public void OnSettingsResetProgressRequestButton()
        {
            resetProgressGUI.gameObject.SetActive(true);
            ButtonSFX();
        }

        public void OnSettingsResetProgressConfirmButton()
        {
            resetProgressGUI.gameObject.SetActive(false);
            ButtonSFX();

            // reset progress
            SaveUtil.Clear();
            SceneLookup.Get<LevelSwitcher>().LoadLevel(Level.Map, fadeOutBeforeLoad: true);
        }

        public void OnSettingsResetProgressCancelButton()
        {
            resetProgressGUI.gameObject.SetActive(false);
            ButtonSFX();
        }

        protected void ButtonSFX(string audioKey = AudioKeys.UI_Button_Press)
        {
            audioManager = SceneLookup.Get<AudioManager>();
            audioManager?.PlayAudioNonSpatial(audioKey);
        }
    }
}
