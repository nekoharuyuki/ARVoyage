using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballToss
{
    /// <summary>
    /// A collider just behind a snowring's open center,
    /// triggered when a snowball is thrown through the snowring
    /// </summary>
    public class SnowringThruRing : MonoBehaviour
    {
        public Snowring snowring;

        private void OnTriggerEnter(Collider collider)
        {
            if (snowring != null)
            {
                snowring.Success();
            }
        }
    }
}

