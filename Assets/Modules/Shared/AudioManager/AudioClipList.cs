using UnityEngine;
using System.Collections.Generic;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// A list of audio clips for a category
    /// </summary>
    [CreateAssetMenu(fileName = "AudioClipsDescription", menuName = "ScriptableObjects/AudioClipList")]
    public class AudioClipList : ScriptableObject
    {
        public string category;
        public List<AudioClip> clips;
    }
}
