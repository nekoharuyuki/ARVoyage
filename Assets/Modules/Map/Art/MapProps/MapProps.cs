using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    public class MapProps : MonoBehaviour
    {
        [SerializeField] GameObject propWalkaboutIncomplete;
        [SerializeField] GameObject propWalkaboutComplete;

        [SerializeField] GameObject propSnowballTossIncomplete;
        [SerializeField] GameObject propSnowballTossComplete;

        [SerializeField] GameObject propSnowballFightIncomplete;
        [SerializeField] GameObject propSnowballFightComplete;

        private void OnEnable()
        {
            bool walkaboutCompleted = SaveUtil.IsBadgeUnlocked(Level.Walkabout);
            propWalkaboutIncomplete.SetActive(!walkaboutCompleted);
            propWalkaboutComplete.SetActive(walkaboutCompleted);

            bool snowballTossCompleted = SaveUtil.IsBadgeUnlocked(Level.SnowballToss);
            propSnowballTossIncomplete.SetActive(!snowballTossCompleted);
            propSnowballTossComplete.SetActive(snowballTossCompleted);

            bool snowballFightCompleted = SaveUtil.IsBadgeUnlocked(Level.SnowballFight);
            propSnowballFightIncomplete.SetActive(!snowballFightCompleted);
            propSnowballFightComplete.SetActive(snowballFightCompleted);
        }
    }
}
