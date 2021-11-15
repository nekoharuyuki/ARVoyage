using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Universal button in the upper left corner of all demos, 
    /// allowing the player to return to the Map (main menu) at all times.
    /// Includes a retractable confirmation button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ReturnToMapButton : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject confirmationButtonPanel;
        [SerializeField] private Button confirmationButton;

        private float timeToggled = 0f;
        private const float toggleTimeout = 5f;

        LevelSwitcher levelSwitcher;
        AudioManager audioManager;

        void Awake()
        {
            levelSwitcher = SceneLookup.Get<LevelSwitcher>();
        }

        private void OnEnable()
        {
            toggleButton.onClick.AddListener(OnToggleButtonClick);
            confirmationButton.onClick.AddListener(OnConfirmationButtonClick);
        }

        private void OnDisable()
        {
            toggleButton.onClick.RemoveListener(OnToggleButtonClick);
            confirmationButton.onClick.RemoveListener(OnConfirmationButtonClick);
        }

        void Update()
        {
            // untoggle after a timeout
            if (confirmationButtonPanel.activeInHierarchy &&
                Time.time > timeToggled + toggleTimeout)
            {
                OnToggleButtonClick();
            }
        }

        private void OnToggleButtonClick()
        {
            confirmationButtonPanel.gameObject.SetActive(!confirmationButtonPanel.activeInHierarchy);

            if (confirmationButtonPanel.activeInHierarchy)
            {
                timeToggled = Time.time;
            }

            ButtonSFX(AudioKeys.UI_Slide_Flip);
        }

        private void OnConfirmationButtonClick()
        {
            ButtonSFX(AudioKeys.UI_Close_Window);

            if (levelSwitcher != null)
            {
                Debug.Log("Return to map");
                levelSwitcher.ReturnToMap();
            }
            else
            {
                Debug.LogError(name + " couldn't find scene switcher");
            }
        }

        protected void ButtonSFX(string audioKey = AudioKeys.UI_Button_Press)
        {
            audioManager = SceneLookup.Get<AudioManager>();
            audioManager?.PlayAudioNonSpatial(audioKey);
        }
    }
}
