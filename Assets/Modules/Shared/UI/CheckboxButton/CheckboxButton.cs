using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Checkbox used in debug menu GUIs.
    /// </summary>
    public class CheckboxButton : MonoBehaviour
    {
        [SerializeField] public Button checkboxButton;
        [SerializeField] private Image checkboxImage;
        [SerializeField] private Sprite uncheckedImage;
        [SerializeField] private Sprite checkedImage;

        public bool isChecked = false;

        private void OnEnable()
        {
            checkboxButton.onClick.AddListener(OnCheckboxButtonClick);
        }

        private void OnDisable()
        {
            checkboxButton.onClick.RemoveListener(OnCheckboxButtonClick);
        }

        private void OnCheckboxButtonClick()
        {
            // toggle the checkbox
            SetChecked(!isChecked);

            ButtonSFX();
        }

        public bool GetChecked()
        {
            return isChecked;
        }

        public void SetChecked(bool isChecked)
        {
            this.isChecked = isChecked;
            checkboxImage.sprite = isChecked ? checkedImage : uncheckedImage;
        }


        protected void ButtonSFX(string audioKey = AudioKeys.UI_Button_Press)
        {
            AudioManager audioManager = SceneLookup.Get<AudioManager>();
            audioManager?.PlayAudioNonSpatial(audioKey);
        }
    }
}
