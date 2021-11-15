using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    [Serializable]
    public class Badge
    {
        public Level level;

        // Notification GUI icon and text
        public GameObject badgeIcon;
        public string subtitleText;

        // Badge row button
        public GameObject badgeRowButton;
    }

    /// <summary>
    /// Manages state and display of the player's earned badges in the map scene
    /// </summary>
    public class BadgeManager : MonoBehaviour, ISceneDependency
    {
        [SerializeField] private List<Badge> badges;

        [SerializeField] private GameObject badgeNotifyGUI;
        [SerializeField] private TMPro.TMP_Text subtitleText;
        [SerializeField] private GameObject badgeRowGUI;

        private AudioManager audioManager;
        private Fader fader;

        void Awake()
        {
            audioManager = SceneLookup.Get<AudioManager>();
            fader = SceneLookup.Get<Fader>();
        }

        public void SetBadgeButtonsClickable(bool clickable)
        {
            if (clickable)
            {
                // Subscribe to badge button events
                MapEvents.EventBadgeOkButton.AddListener(OnEventOkButton);
                MapEvents.EventBadge1Button.AddListener(OnEventBadge1Button);
                MapEvents.EventBadge2Button.AddListener(OnEventBadge2Button);
                MapEvents.EventBadge3Button.AddListener(OnEventBadge3Button);
                MapEvents.EventBadge4Button.AddListener(OnEventBadge4Button);
                MapEvents.EventBadge5Button.AddListener(OnEventBadge5Button);
            }
            else
            {
                // Unsubscribe from badge button events
                MapEvents.EventBadgeOkButton.RemoveListener(OnEventOkButton);
                MapEvents.EventBadge1Button.RemoveListener(OnEventBadge1Button);
                MapEvents.EventBadge2Button.RemoveListener(OnEventBadge2Button);
                MapEvents.EventBadge3Button.RemoveListener(OnEventBadge3Button);
                MapEvents.EventBadge4Button.RemoveListener(OnEventBadge4Button);
                MapEvents.EventBadge5Button.RemoveListener(OnEventBadge5Button);
            }
        }

        void OnDisable()
        {
            SetBadgeButtonsClickable(false);
        }

        private void OnEventBadge1Button()
        {
            DisplayBadgeAchievedGUI(Level.Walkabout);
        }

        private void OnEventBadge2Button()
        {
            DisplayBadgeAchievedGUI(Level.SnowballToss);
        }

        private void OnEventBadge3Button()
        {
            DisplayBadgeAchievedGUI(Level.SnowballFight);
        }

        private void OnEventBadge4Button()
        {
            DisplayBadgeAchievedGUI(Level.BuildAShip);
        }

        private void OnEventBadge5Button()
        {
            DisplayBadgeAchievedGUI(Level.Map);
        }

        private void OnEventOkButton()
        {
            // Don't fade out the GUI if there are more badges to present
            if (GetNextBadgeToPresent() != Level.None) return;

            StartCoroutine(DemoUtil.FadeOutGUI(badgeNotifyGUI, fader));
        }


        public Level GetNextBadgeToPresent()
        {
            foreach (Badge badge in badges)
            {
                if (SaveUtil.IsBadgeUnlocked(badge.level) &&
                    !SaveUtil.WasBadgeNotificationPresented(badge.level))
                {
                    return badge.level;
                }
            }

            return Level.None;
        }


        public bool DisplayBadgeAchievedGUI(Level badgeLevel, bool playSFX = false)
        {
            // If a badge is being displayed
            if (badgeNotifyGUI.activeSelf)
            {
                // if it's already displaying this badge, fade out (it's being toggled off)
                if (IsDisplayingBadgeLevel(badgeLevel))
                {
                    StartCoroutine(DemoUtil.FadeOutGUI(badgeNotifyGUI, fader));
                    return true;
                }

                // if it's a new badge, fade this one out first, then call this method again
                else
                {
                    StartCoroutine(DisplayNextBadgeCoroutine(badgeLevel, playSFX));
                    return true;
                }
            }

            // If no badge is being displayed, fade in
            else
            {
                StartCoroutine(DemoUtil.FadeInGUI(badgeNotifyGUI, fader));
            }

            // SFX
            if (playSFX)
            {
                audioManager.PlayAudioNonSpatial(AudioKeys.SFX_Victory_Quick, volume: 0.7f);
            }

            // set badge icon and subtitle text in GUI
            bool found = false;
            foreach (Badge badge in badges)
            {
                if (badge.level == badgeLevel)
                {
                    found = true;
                    badge.badgeIcon.gameObject.SetActive(true);
                    subtitleText.text = badge.subtitleText;
                }
                else
                {
                    badge.badgeIcon.gameObject.SetActive(false);
                }
            }

            return found;
        }

        private IEnumerator DisplayNextBadgeCoroutine(Level badgeLevel, bool playSFX = true)
        {
            // Fade out GUI
            yield return StartCoroutine(DemoUtil.FadeOutGUI(badgeNotifyGUI, fader));

            DisplayBadgeAchievedGUI(badgeLevel, playSFX: playSFX);
        }


        private bool IsDisplayingBadgeLevel(Level badgeLevel)
        {
            foreach (Badge badge in badges)
            {
                if (badge.level == badgeLevel)
                {
                    return badge.badgeIcon.gameObject.activeSelf;
                }
            }

            return false;
        }

        public void DisplayBadgeRowButtons(bool showBadgeRow, Level animateBadgeLevel = Level.None)
        {
            badgeRowGUI.gameObject.SetActive(showBadgeRow);

            if (showBadgeRow)
            {
                foreach (Badge badge in badges)
                {
                    bool showBadgeButton = SaveUtil.WasBadgeNotificationPresented(badge.level);

                    // Bubble-scale animate up the badge button if requested
                    if (badge.level == animateBadgeLevel)
                    {
                        DemoUtil.DisplayWithBubbleScale(badge.badgeRowButton.gameObject, show: true);
                    }

                    // otherwise just display it
                    else
                    {
                        badge.badgeRowButton.gameObject.SetActive(showBadgeButton);
                    }
                }
            }
        }


        private int GetNumBadgesPresented()
        {
            int numBadgesPresented = 0;
            foreach (Badge badge in badges)
            {
                if (SaveUtil.WasBadgeNotificationPresented(badge.level))
                {
                    ++numBadgesPresented;
                }
            }

            return numBadgesPresented;
        }


        public bool AreAllBadgesPresented()
        {
            return GetNumBadgesPresented() == badges.Count;
        }


        public void CheckForAirshipBadgeUnlock()
        {
            // if only 1 more badge to go, and it's the Airship (Map)
            // then unlock the airship (map) badge!
            if (GetNumBadgesPresented() == badges.Count - 1 &&
                !SaveUtil.IsBadgeUnlocked(Level.Map))
            {
                SaveUtil.SaveBadgeUnlocked(Level.Map);
            }
        }

    }
}
