using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// Yeti NPC, used in StatePlacement and StateYetiRequest in the BuildAShip demo.
    /// Includes the yeti's speech bubble billboard.
    /// </summary>
    public class BuildAShipActor : MonoBehaviour
    {
        [SerializeField] GameObject speechBubble;
        [SerializeField] TransparentDepthHelper transparentDepthHelper;

        public void Awake()
        {
            SetSpeechBubbleVisible(false);
        }

        public void SetSpeechBubbleVisible(bool visible)
        {
            speechBubble.SetActive(visible);
        }

        public void SetTransparent(bool transparent)
        {
            transparentDepthHelper.SetTransparent(transparent);
        }

#if UNITY_EDITOR

        private void OnGUI()
        {
            if (GUILayout.Button("Transparent"))
            {
                SetTransparent(true);
            }
            if (GUILayout.Button("Opaque"))
            {
                SetTransparent(false);
            }
        }
#endif

        /*
        void OnGUI()
        {
            if (GUILayout.Button("Bubble On"))
            {
                SetSpeechBubbleVisible(true);
            }

            if (GUILayout.Button("Bubble Off"))
            {
                SetSpeechBubbleVisible(false);
            }
        }
        */
    }
}