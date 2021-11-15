using System;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Data structure created when a player tosses a locally spawned snowball in SnowballFight
    /// This structure is serialized and transmitted to all peers in the session so that they
    /// can locally apply the physics in their instance
    /// </summary>
    [Serializable]
    class TossData
    {
        public float angle;
        public Quaternion rotation;
        public Vector3 force;
        public Vector3 torque;

        public void Reset()
        {
            angle = 0f;
            rotation = Quaternion.identity;
            force = Vector3.zero;
            torque = Vector3.zero;
        }
    }
}
