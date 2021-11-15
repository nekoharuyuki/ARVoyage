using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Decay (melt) persistent snowball splat over time.
    /// </summary>
    public class SnowballSplat : MonoBehaviour
    {
        [SerializeField] float meltDuration = 24;

        float startTime, endTime;
        Vector3 startScale;

        void Start()
        {
            startTime = Time.time;
            endTime = startTime + meltDuration;
            startScale = transform.localScale;
        }

        void Update()
        {
            float t = (Time.time - startTime) / (endTime - startTime);

            transform.localScale = startScale * (1.0f - t);

            if (t >= 1)
            {
                Destroy(gameObject);
            }
        }
    }
}