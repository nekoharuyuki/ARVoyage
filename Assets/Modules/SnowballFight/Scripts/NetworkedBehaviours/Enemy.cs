using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// A firefly enemy in the multiplayer SnowballFight demo.
    /// An enemy is spawned by the EnemyManager class, 
    /// and controlled by the networked EnemyBehaviour class.
    /// An enemy is killed when a player-thrown snowball collides with it.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public AppEvent<Collision> CollisionEnter = new AppEvent<Collision>();

        [SerializeField] Animator animator;
        [SerializeField] GameObject hitParticles;
        [SerializeField] GameObject deathParticles;

        private const float buzzSFXFadeDuration = 1f;

        private bool hit = false;

        private AudioManager audioManager;
        private AudioSource buzzAudioLoop;

        void OnEnable()
        {
            hit = false;

            transform.localScale = Vector3.zero;
            BubbleScaleUtil.ScaleUp(gameObject);

            // SFX
            audioManager = SceneLookup.Get<AudioManager>();
            audioManager.PlayAudioOnObject(AudioKeys.SFX_Bug_Splat_Gloop, this.gameObject);
            buzzAudioLoop = audioManager.PlayAudioOnObject(AudioKeys.SFX_Bug_Buzz_LP,
                                                            targetObject: this.gameObject,
                                                            loop: true,
                                                            volume: 0.5f,
                                                            fadeInDuration: buzzSFXFadeDuration);
        }

        public void OnCollisionEnter(Collision collision)
        {
            CollisionEnter.Invoke(collision);
        }

        public void Hit()
        {
            if (!hit)
            {
                hit = true;

                StartCoroutine(HitRoutine());
            }
        }

        private IEnumerator HitRoutine()
        {
            // Hit particles.
            {
                GameObject instance = Instantiate(hitParticles,
                    hitParticles.transform.position, Quaternion.identity);
                instance.SetActive(true);
            }

            // SFX
            audioManager.PlayAudioAtPosition(AudioKeys.VOX_Bug_Death, this.gameObject.transform.position);
            if (buzzAudioLoop != null)
            {
                buzzAudioLoop.Stop();
                buzzAudioLoop = null;
            }

            // Death animation and wait.
            animator.SetTrigger("Death");
            yield return new WaitForSeconds(.533f);

            // Death particles.
            {
                GameObject instance = Instantiate(deathParticles,
                    deathParticles.transform.position, Quaternion.identity);
                instance.SetActive(true);
            }

            // Disable art.
            gameObject.SetActive(false);
        }

        public void FadeOutBuzzLoopSFX(float fadeDuration)
        {
            if (buzzAudioLoop != null)
            {
                audioManager.FadeOutAudioSource(buzzAudioLoop, fadeDuration: fadeDuration);
            }
        }
    }
}