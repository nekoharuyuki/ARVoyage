

namespace Niantic.ARVoyage.Walkabout
{
    /// <summary>
    /// UI events for the Walkabout scene. Extends the general-purpose DemoEvents class.
    /// </summary>
    public class WalkaboutEvents : DemoEvents
    {
        public static AppEvent EventPlacementButton = new AppEvent();

        public void PlacementButtonPressed()
        {
            EventPlacementButton.Invoke();
            ButtonSFX();
        }
    }
}
