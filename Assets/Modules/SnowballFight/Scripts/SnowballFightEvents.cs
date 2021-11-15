using Niantic.ARVoyage.SnowballToss;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// UI and other shared events for the SnowballFight level
    /// </summary>
    public class SnowballFightEvents : DemoEvents
    {
        public static AppEvent EventHostButton = new AppEvent();
        public static AppEvent EventJoinButton = new AppEvent();
        public static AppEvent EventConfirmButton = new AppEvent();
        public static AppEvent EventBackButton = new AppEvent();
        public static AppEvent<uint> EventConnectionFailed = new AppEvent<uint>();
        public static AppEvent EventSnowballTossButton = new AppEvent();

        // Stores the typed-in join code input field value
        public string SessionJoinCodeInput { get; set; }
        public static AppEvent<string> EventSessionJoinCodeInputChanged = new AppEvent<string>();

        public static AppEvent EventGameStart = new AppEvent();
        public static AppEvent<int> EventLocalPlayerScoreChanged = new AppEvent<int>();
        public static AppEvent EventLocalPlayerHit = new AppEvent();
        public static AppEvent<SnowballBehaviour> EventSnowballHitEnemy = new AppEvent<SnowballBehaviour>();

        public void HostButtonPressed()
        {
            EventHostButton.Invoke();
            ButtonSFX();
        }

        public void JoinButtonPressed()
        {
            EventJoinButton.Invoke();
            ButtonSFX();
        }

        public void ConfirmButtonPressed()
        {
            EventConfirmButton.Invoke();
            ButtonSFX();
        }

        public void BackButtonPressed()
        {
            EventBackButton.Invoke();
            ButtonSFX(AudioKeys.UI_Close_Window);
        }

        public void SessionJoinCodeInputChanged()
        {
            EventSessionJoinCodeInputChanged.Invoke(SessionJoinCodeInput.ToUpper());
        }

        public void SnowballTossButtonPressed()
        {
            EventSnowballTossButton.Invoke();
            ButtonSFX();
        }
    }
}
