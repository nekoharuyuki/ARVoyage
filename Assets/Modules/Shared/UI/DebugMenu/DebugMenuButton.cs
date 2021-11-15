using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Universal button in the upper right corner of all demos, 
    /// allowing the player to display/hide the current demo's debug menu GUI.
    /// </summary>
    public class DebugMenuButton : MonoBehaviour
    {
        [SerializeField] private Button debugMenuButton;
        [SerializeField] private GameObject debugMenuGUI;
        [SerializeField] private GameObject fullscreenScrim;

        private void OnEnable()
        {
            debugMenuButton.onClick.AddListener(OnDebugMenuButtonClick);
        }

        private void OnDisable()
        {
            debugMenuButton.onClick.RemoveListener(OnDebugMenuButtonClick);
        }

        private void OnDebugMenuButtonClick()
        {
            // toggle the debug menu on/off
            ShowDebugMenuGUI(!debugMenuGUI.activeSelf);
            ButtonSFX();
        }

        public void ShowDebugMenuGUI(bool show)
        {
            fullscreenScrim.SetActive(show);
            debugMenuGUI.SetActive(show);
        }

        protected void ButtonSFX(string audioKey = AudioKeys.UI_Button_Press)
        {
            AudioManager audioManager = SceneLookup.Get<AudioManager>();
            audioManager?.PlayAudioNonSpatial(audioKey);
        }
    }
}
