using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.BuildAShip
{
    /// <summary>
    /// Sprite for displaying a BuildAShip resource aligned with the semantic segmentation channel texture
    /// Managed by the BuildAShipResourceRenderer  
    /// </summary>
    public class BuildAShipResourceTile : MonoBehaviour
    {
        public int samples;
        private SpriteRenderer spriteRenderer;

        public float startTime;
        public float endTime;

        public AnimationCurve positionCurveX = new AnimationCurve();
        public AnimationCurve positionCurveY = new AnimationCurve();
        public AnimationCurve positionCurveZ = new AnimationCurve();
        public AnimationCurve rotationCurve = new AnimationCurve();
        public AnimationCurve scaleCurve = new AnimationCurve();

        public void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        }
    }
}
