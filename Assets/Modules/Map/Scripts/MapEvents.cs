

namespace Niantic.ARVoyage.Map
{
    /// <summary>
    /// Manages UI events in the map scene
    /// </summary>
    public class MapEvents : DemoEvents
    {
        // Level Choice GUI
        public static AppEvent EventLevelGoButton = new AppEvent();
        public static AppEvent EventLevelXCloseButton = new AppEvent();

        // BadgeNotify GUI
        public static AppEvent EventBadgeOkButton = new AppEvent();

        // BadgeRowButtons
        public static AppEvent EventBadge1Button = new AppEvent();
        public static AppEvent EventBadge2Button = new AppEvent();
        public static AppEvent EventBadge3Button = new AppEvent();
        public static AppEvent EventBadge4Button = new AppEvent();
        public static AppEvent EventBadge5Button = new AppEvent();

        // StateAirship ThankYou / ResetProgress GUIs
        public static AppEvent EventResetProgressRequestButton = new AppEvent();
        public static AppEvent EventResetProgressConfirmButton = new AppEvent();
        public static AppEvent EventResetProgressCancelButton = new AppEvent();
        public static AppEvent EventBackToMapButton = new AppEvent();

        // Level Choice GUI

        public void LevelGoButtonPressed()
        {
            EventLevelGoButton.Invoke();
            ButtonSFX();
        }

        public void LevelXCloseButtonPressed()
        {
            EventLevelXCloseButton.Invoke();
            ButtonSFX(AudioKeys.UI_Close_Window);
        }


        // BadgeNotify GUI

        public void BadgeOkButtonPressed()
        {
            EventBadgeOkButton.Invoke();
            ButtonSFX();
        }


        // BadgeRowButtons

        public void Badge1ButtonPressed()
        {
            EventBadge1Button.Invoke();
            ButtonSFX();
        }

        public void Badge2ButtonPressed()
        {
            EventBadge2Button.Invoke();
            ButtonSFX();
        }

        public void Badge3ButtonPressed()
        {
            EventBadge3Button.Invoke();
            ButtonSFX();
        }

        public void Badge4ButtonPressed()
        {
            EventBadge4Button.Invoke();
            ButtonSFX();
        }

        public void Badge5ButtonPressed()
        {
            EventBadge5Button.Invoke();
            ButtonSFX();
        }


        // StateAirship ThankYou / ResetProgress GUIs

        public void ResetProgressRequestButton()
        {
            EventResetProgressRequestButton.Invoke();
            ButtonSFX();
        }

        public void ResetProgressConfirmButton()
        {
            EventResetProgressConfirmButton.Invoke();
            ButtonSFX();
        }

        public void ResetProgressCancelButton()
        {
            EventResetProgressCancelButton.Invoke();
            ButtonSFX();
        }

        public void BackToMapButton()
        {
            EventBackToMapButton.Invoke();
            ButtonSFX(AudioKeys.UI_Close_Window);
        }

    }
}
