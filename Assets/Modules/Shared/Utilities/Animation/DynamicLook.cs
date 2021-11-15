using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage
{
    /// <summary>
    /// For NPCs dynamically looking at a target
    /// </summary>
    public class DynamicLook : MonoBehaviour
    {
        [SerializeField] Transform lookTransform;
        [SerializeField] Transform targetTransform;
        [SerializeField] float lookSpeed = 5f;

        private Quaternion lastRotation = Quaternion.identity;

        void LateUpdate()
        {
            if (lookTransform == null) return;

            Quaternion targetRotation = lookTransform.rotation;

            if (targetTransform != null)
            {
                Vector3 lookDirection = (targetTransform.position - lookTransform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

                float angle = Vector3.Dot(lookTransform.forward, lookDirection);
                angle = Mathf.Clamp01(angle);

                targetRotation = Quaternion.Lerp(lookTransform.rotation, lookRotation, angle);
            }

            lookTransform.rotation = Quaternion.Lerp(lastRotation, targetRotation, Time.deltaTime * lookSpeed);
            lastRotation = lookTransform.rotation;
        }
    }
}
