using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Universal underlying template for all demos' debug menus.
    /// </summary>
    public class DebugMenuGUI : MonoBehaviour
    {
        // TODO : Replace these with two dynamic arrays for checkboxes and buttons.
        // This existing system is pretty crude, and painful to update.
        public static AppEvent EventDebugOption1Checkbox = new AppEvent();
        public static AppEvent EventDebugOption2Checkbox = new AppEvent();
        public static AppEvent EventDebugOption3Checkbox = new AppEvent();
        public static AppEvent EventDebugOption4Checkbox = new AppEvent();
        public static AppEvent EventDebugOption5Checkbox = new AppEvent();
        public static AppEvent EventDebugOption5Button = new AppEvent();
        public static AppEvent EventDebugOption6Button = new AppEvent();

        [SerializeField] private DebugMenuButton debugMenuButton;
        [SerializeField] private Button xCloseButton;

        [SerializeField] public CheckboxButton option1Checkbox;
        [SerializeField] public CheckboxButton option2Checkbox;
        [SerializeField] public CheckboxButton option3Checkbox;
        [SerializeField] public CheckboxButton option4Checkbox;
        [SerializeField] public CheckboxButton option5Checkbox;
        [SerializeField] public Button option5Button;
        [SerializeField] public Button option6Button;


        private void OnEnable()
        {
            if (option1Checkbox != null) option1Checkbox.checkboxButton.onClick.AddListener(OnOption1CheckboxButtonClick);
            if (option2Checkbox != null) option2Checkbox.checkboxButton.onClick.AddListener(OnOption2CheckboxButtonClick);
            if (option3Checkbox != null) option3Checkbox.checkboxButton.onClick.AddListener(OnOption3CheckboxButtonClick);
            if (option4Checkbox != null) option4Checkbox.checkboxButton.onClick.AddListener(OnOption4CheckboxButtonClick);
            if (option5Checkbox != null) option5Checkbox.checkboxButton.onClick.AddListener(OnOption5CheckboxButtonClick);
            if (option5Button != null) option5Button.onClick.AddListener(OnOption5ButtonClick);
            if (option6Button != null) option6Button.onClick.AddListener(OnOption6ButtonClick);
            xCloseButton.onClick.AddListener(OnXCloseButtonClick);
        }

        private void OnDisable()
        {
            if (option1Checkbox != null) option1Checkbox.checkboxButton.onClick.RemoveListener(OnOption1CheckboxButtonClick);
            if (option2Checkbox != null) option2Checkbox.checkboxButton.onClick.RemoveListener(OnOption2CheckboxButtonClick);
            if (option3Checkbox != null) option3Checkbox.checkboxButton.onClick.RemoveListener(OnOption3CheckboxButtonClick);
            if (option4Checkbox != null) option4Checkbox.checkboxButton.onClick.RemoveListener(OnOption4CheckboxButtonClick);
            if (option5Checkbox != null) option5Checkbox.checkboxButton.onClick.RemoveListener(OnOption5CheckboxButtonClick);
            if (option5Button != null) option5Button.onClick.RemoveListener(OnOption5ButtonClick);
            if (option6Button != null) option6Button.onClick.RemoveListener(OnOption6ButtonClick);
            xCloseButton.onClick.RemoveListener(OnXCloseButtonClick);
        }

        public bool GetChecked(int index)
        {
            switch (index)
            {
                case 0: return option1Checkbox.GetChecked();
                case 1: return option2Checkbox.GetChecked();
                case 2: return option3Checkbox.GetChecked();
                case 3: return option4Checkbox.GetChecked();
                case 4: return option5Checkbox.GetChecked();
            }
            return false;
        }

        public void SetChecked(int index, bool isChecked)
        {
            switch (index)
            {
                case 0: option1Checkbox.SetChecked(isChecked); break;
                case 1: option2Checkbox.SetChecked(isChecked); break;
                case 2: option3Checkbox.SetChecked(isChecked); break;
                case 3: option4Checkbox.SetChecked(isChecked); break;
                case 4: option5Checkbox.SetChecked(isChecked); break;
            }
        }

        private void OnOption1CheckboxButtonClick()
        {
            EventDebugOption1Checkbox.Invoke();
        }

        private void OnOption2CheckboxButtonClick()
        {
            EventDebugOption2Checkbox.Invoke();
        }

        private void OnOption3CheckboxButtonClick()
        {
            EventDebugOption3Checkbox.Invoke();
        }

        private void OnOption4CheckboxButtonClick()
        {
            EventDebugOption4Checkbox.Invoke();
        }

        private void OnOption5CheckboxButtonClick()
        {
            EventDebugOption5Checkbox.Invoke();
        }

        private void OnOption5ButtonClick()
        {
            EventDebugOption5Button.Invoke();
            ButtonSFX();
        }

        private void OnOption6ButtonClick()
        {
            EventDebugOption6Button.Invoke();
            ButtonSFX();
        }

        private void OnXCloseButtonClick()
        {
            HideGUI();
            ButtonSFX();
        }

        public void HideGUI()
        {
            debugMenuButton.ShowDebugMenuGUI(false);
        }

        protected void ButtonSFX(string audioKey = AudioKeys.UI_Button_Press)
        {
            AudioManager audioManager = SceneLookup.Get<AudioManager>();
            audioManager?.PlayAudioNonSpatial(audioKey);
        }
    }
}
