using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Handles the collision detection and VFX for a non-local player. 
    /// This component is managed by its corresponding PlayerBehaviour.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public AppEvent<Collision> CollisionEnter = new AppEvent<Collision>();

        [SerializeField] ParticleSystem frostParticleSystem;
        [SerializeField] ParticleSystem sparkleParticleSystem;
        [SerializeField] int frostParticleCount = 32;
        [SerializeField] int sparkleParticleCount = 64;

        public void OnCollisionEnter(Collision collision)
        {
            CollisionEnter.Invoke(collision);
        }

        public void TriggerHitEffects()
        {
            frostParticleSystem.Emit(frostParticleCount);
            sparkleParticleSystem.Emit(sparkleParticleCount);
        }

#if UNITY_EDITOR && FALSE
        void OnGUI()
        {
            if (GUILayout.Button("Player Trigger"))
            {
                Trigger();
            }
        }
#endif
    }
}
