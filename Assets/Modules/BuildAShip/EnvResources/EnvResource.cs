using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.BuildAShip
{
    // Accurate as of 2021-06-30
    public enum Channel
    {
        sky,
        ground,
        natural_ground,
        artificial_ground,
        water,
        people,
        building,
        flowers,
        foliage,
        tree_trunk,
        pet,
        sand,
        grass,
        tv,
        dirt,
    }

    [CreateAssetMenu(fileName = "EnvResource", menuName = "ScriptableObjects/EnvResource")]
    public class EnvResource : ScriptableObject
    {
        public Channel Channel;
        public string ChannelName;
        public string ResourceName;
        public Sprite ResourceIcon;
        public Sprite ResourceSprite;
    }
}