

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// UI events used in the BuildAShip scene
    /// </summary>
    public class BuildAShipEvents : DemoEvents
    {
        public static AppEvent EventLocationPlacementButton = new AppEvent();
        public static AppEvent EventCollectButtonHeld = new AppEvent();
        public static AppEvent EventCollectButtonReleased = new AppEvent();

        public void LocationPlacementButtonPressed()
        {
            EventLocationPlacementButton.Invoke();
            ButtonSFX();
        }

        public void CollectButtonHeld()
        {
            EventCollectButtonHeld.Invoke();
            ButtonSFX();
        }

        public void CollectButtonReleased()
        {
            EventCollectButtonReleased.Invoke();
        }
    }
}
