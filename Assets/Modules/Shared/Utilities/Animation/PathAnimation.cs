using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// Used for NPC path locomotion on a Gameboard.
    /// </summary>
    [System.Serializable]
    public class PathAnimation
    {
        [SerializeField] AnimationCurve xCurve;
        [SerializeField] AnimationCurve yCurve;
        [SerializeField] AnimationCurve zCurve;

        public float TotalDuration { get; private set; } = 0;

        public Vector3 Evaluate(float time)
        {
            float x = xCurve.Evaluate(time);
            float y = yCurve.Evaluate(time);
            float z = zCurve.Evaluate(time);
            return new Vector3(x, y, z);
        }

        public void SetPath(List<Vector3> path, float speed)
        {
            xCurve = new AnimationCurve();
            yCurve = new AnimationCurve();
            zCurve = new AnimationCurve();

            float time = 0;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 startPosition = path[i];

                xCurve.AddKey(time, startPosition.x);
                yCurve.AddKey(time, startPosition.y);
                zCurve.AddKey(time, startPosition.z);

                if (i < path.Count - 1)
                {
                    Vector3 nextPosition = path[i + 1];
                    float distance = Vector3.Distance(startPosition, nextPosition);
                    float duration = distance * (1 / speed);
                    time += duration;
                }
            }

            TotalDuration = time;
        }
    }
}