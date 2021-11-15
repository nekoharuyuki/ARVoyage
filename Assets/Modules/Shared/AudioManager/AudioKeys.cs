using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Names of all audio keys. These names can map to multiple audio variations per key.
    /// Generate the contents of this class by loading the AudioManager with all AudioClipLists 
    /// in the project and setting buildKeyConstsToClipboardInEditor to true.
    /// </summary>
    public static class AudioKeys
    {
        // BuildAShip
        public const string SFX_Resource_Vacuum = "SFX_Resource_Vacuum";
        public const string SFX_Vacuum_End = "SFX_Vacuum_End";
        public const string SFX_Vacuum_LP = "SFX_Vacuum_LP";
        public const string SFX_Vacuum_Start = "SFX_Vacuum_Start";
        public const string SFX_Yeti_Binocular = "SFX_Yeti_Binocular";

        // Map
        public const string MX_Background = "MX_Background";
        public const string SFX_Airship_FlyAway = "SFX_Airship_FlyAway";
        public const string SFX_Airship_LP = "SFX_Airship_LP";
        public const string SFX_AirShipBuild = "SFX_AirShipBuild";
        public const string SFX_Door_Open = "SFX_Door_Open";
        public const string SFX_Door_Shut = "SFX_Door_Shut";
        public const string SFX_MountainWind_LP = "SFX_MountainWind_LP";
        public const string SFX_Victory_Quick = "SFX_Victory_Quick";
        public const string SFX_WaterGentle_LP = "SFX_WaterGentle_LP";

        // Shared
        public const string SFX_Countdown_Timer = "SFX_Countdown_Timer";
        public const string SFX_Countdown_Timer_End = "SFX_Countdown_Timer_End";
        public const string SFX_Snowball_Bump = "SFX_Snowball_Bump";
        public const string SFX_SnowballThrow = "SFX_SnowballThrow";
        public const string SFX_SnowmanBuild = "SFX_SnowmanBuild";
        public const string SFX_Success_Magic = "SFX_Success_Magic";
        public const string SFX_Timer_Alarm = "SFX_Timer_Alarm";
        public const string SFX_Winner_Fanfare = "SFX_Winner_Fanfare";
        public const string SFX_Yeti_Footstep_Snow = "SFX_Yeti_Footstep_Snow";
        public const string UI_Button_Press = "UI_Button_Press";
        public const string UI_Close_Window = "UI_Close_Window";
        public const string UI_Slide_Flip = "UI_Slide_Flip";

        // SnowballFight
        public const string SFX_Bug_Buzz_LP = "SFX_Bug_Buzz_LP";
        public const string SFX_Bug_Splat_Gloop = "SFX_Bug_Splat_Gloop";
        public const string SFX_Loser_Fanfare = "SFX_Loser_Fanfare";
        public const string VOX_Bug_Death = "VOX_Bug_Death";

        // SnowballToss
        public const string SFX_IceRing_Spawn = "SFX_IceRing_Spawn";
        public const string SFX_RingScore_Indicator = "SFX_RingScore_Indicator";

        // Walkabout
        public const string SFX_Snowball_Roll_LP = "SFX_Snowball_Roll_LP";
        public const string SFX_Snowball_SizeAchieved = "SFX_Snowball_SizeAchieved";
    }
}
