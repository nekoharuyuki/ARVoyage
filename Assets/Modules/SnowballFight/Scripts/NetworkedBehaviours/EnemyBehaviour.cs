
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// A network-spawned EnemyBehaviour
    /// This class tracks the networked states for each enemy spawned in the session
    /// as well as controlling enemy functionality
    /// This component manages its corresponding Enemy component.
    /// </summary>
    [RequireComponent(typeof(AuthBehaviour))]
    public class EnemyBehaviour : NetworkedBehaviour
    {
        [SerializeField] Enemy firefly;
        [SerializeField] int destroyTimeout = 5;

        public INetworkedField<bool> isAlive;
        public INetworkedField<bool> isExpiring;

        private bool IsInitialized = false;
        private bool isExpired = false;

        private Vector3 originalPosition;

        private Transform targetPositionTransform;
        private float targetPositionUpdateTimeout = 2.5f;
        private float lastTargetPositionUpdateTime;
        private float targetPositionLerpSpeed = 2.5f;
        private float targetPositionRandomRadius = .5f;

        private Transform targetLookTransform;
        private float targetLookUpdateTimeout = 4;
        private float targetLookLerpSpeed = .5f;
        private float lastTargetLookUpdateTime;

        private float expireTimeout = 10;
        private float expireScaleDuration = 1;

        public bool WasSpawnedByMe
        {
            get { return this.Owner.WasSpawnedByMe; }
        }

        public string OwnerId
        {
            get { return this.Owner.SpawningPeer.Identifier.ToString(); }
        }

        private void Start()
        {
            firefly.CollisionEnter.AddListener(OnFireflyCollisionEnter);
            Invoke("Expire", expireTimeout);
        }

        private void OnDestroy()
        {
            firefly.CollisionEnter.RemoveListener(OnFireflyCollisionEnter);

            if (targetPositionTransform != null)
            {
                Destroy(targetPositionTransform.gameObject);
                targetPositionTransform = null;
            }
        }

        private void Update()
        {
            // The spawner randomizes position.
            if (IsInitialized && WasSpawnedByMe && targetPositionTransform != null)
            {
                if (Time.time - lastTargetPositionUpdateTime > targetPositionUpdateTimeout)
                {
                    targetPositionTransform.transform.position = originalPosition +
                        (UnityEngine.Random.onUnitSphere * UnityEngine.Random.value * targetPositionRandomRadius);

                    lastTargetPositionUpdateTime = Time.time;
                }
            }

            // Everybody randomizes rotation.
            if (IsInitialized)
            {
                if (Time.time - lastTargetLookUpdateTime > targetLookUpdateTimeout)
                {
                    targetLookTransform = GetBestLookTransform();
                    lastTargetLookUpdateTime = Time.time;
                }
            }

            // Everyone lerps to the current target position.
            if (targetPositionTransform != null)
            {
                transform.position = Vector3.Lerp(transform.position,
                    targetPositionTransform.position,
                    Time.deltaTime / targetPositionLerpSpeed);
            }

            // Everyone lerps to the current look target.
            if (targetLookTransform != null)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(targetLookTransform.position -
                                            transform.position, Vector3.up),
                    Time.deltaTime / targetLookLerpSpeed);
            }

        }

        protected override void SetupSession(out Action initializer, out int order)
        {
            initializer = () =>
            {
                // Position/rotation handling.
                {
                    originalPosition = transform.position;

                    // Create target null for lerping.
                    targetPositionTransform = (new GameObject()).transform;
                    targetPositionTransform.name = gameObject.name + "_target";
                    targetPositionTransform.transform.position = transform.position;

                    // Default look transform to camera.
                    targetLookTransform = GetBestLookTransform();
                }

                NetworkedDataDescriptor authorityToObserver =
                    Owner.Auth.AuthorityToObserverDescriptor(TransportType.ReliableUnordered);

                NetworkedDataDescriptor anyToAny =
                    Owner.Auth.AnyToAnyDescriptor(TransportType.ReliableUnordered);

                new UnreliableBroadcastTransformPacker("fireflyTransform",
                                                       targetPositionTransform,
                                                       authorityToObserver,
                                                       TransformPiece.Position,
                                                       Owner.Group);

                isAlive = new NetworkedField<bool>("fireflyIsAlive",
                                                   anyToAny,
                                                   Owner.Group, true);
                isAlive.ValueChanged += OnIsAliveValueChanged;

                isExpiring = new NetworkedField<bool>("fireflyIsExpiring",
                                                   authorityToObserver,
                                                   Owner.Group, false);
                isExpiring.ValueChanged += OnIsExpiringValueChanged;


                IsInitialized = true;
            };

            order = 0;
        }

        private Transform GetBestLookTransform()
        {
            SnowballFightManager snowballFightManager = SceneLookup.Get<SnowballFightManager>();

            // Default look transform to camera.
            Transform bestTransform = Camera.main.transform;

            // Try to find a closer player.
            if (snowballFightManager.Players.Count > 1)
            {
                float bestDistance = Mathf.Infinity;

                foreach (PlayerData playerData in snowballFightManager.Players.Values)
                {
                    float currentDistance = Vector3.Distance(transform.position,
                                                             playerData.Behaviour.transform.position);

                    if (currentDistance < bestDistance)
                    {
                        bestDistance = currentDistance;
                        bestTransform = playerData.Behaviour.transform;
                    }
                }
            }

            // Return new best transform.
            return bestTransform;
        }

        public void Expire()
        {
            isExpiring.SetIfSender(true);
        }

        private void OnIsExpiringValueChanged(NetworkedFieldValueChangedArgs<bool> args)
        {
            if (args.Value == true)
            {
                Debug.Log("EnemyBehaviour.Expire");
                System.Action onComplete = () =>
                {
                    isExpired = true;
                    isAlive.SetIfSender(false);
                };

                BubbleScaleUtil.ScaleDown(gameObject, 0, expireScaleDuration, onComplete: onComplete);

                if (firefly != null)
                {
                    firefly.FadeOutBuzzLoopSFX(expireScaleDuration);
                }
            }
        }

        private void OnIsAliveValueChanged(NetworkedFieldValueChangedArgs<bool> args)
        {
            if (args.Value == false)
            {
                // Stop scaling if we were.
                BubbleScaleUtil.StopRunningScale(gameObject);

                // Animate death unless we expired.
                if (!isExpired) firefly?.Hit();

                // Only the spawner can destroy.
                if (WasSpawnedByMe) Destroy(gameObject, destroyTimeout);
            }
        }

        private void OnFireflyCollisionEnter(Collision collision)
        {
            isAlive.SetIfSender(false);
        }
    }
}