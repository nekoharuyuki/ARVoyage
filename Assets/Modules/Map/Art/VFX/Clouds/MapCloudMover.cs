using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.Map
{
    public class MapCloudMover : MonoBehaviour
    {
        [SerializeField] Transform offsetTransform;
        [SerializeField] float duration = 10;

        private Vector3 startPosition;
        private Vector3 targetPosition;

        void Start()
        {
            startPosition = transform.localPosition;
            targetPosition = -offsetTransform.localPosition;
        }

        void Update()
        {
            float t = (Time.time % duration) / duration;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
        }

    }
}