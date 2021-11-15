

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;

using System;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// A network-spawned SnowballBehaviour
    /// This class tracks the networked states for each snowball in the session.
    /// When a player tosses a snowball, it creates TossData which is shared across the network
    /// so that ecah peer's device can run snowball phyiscs locally
    /// </summary>
    [RequireComponent(typeof(AuthBehaviour), typeof(Snowball))]
    public class SnowballBehaviour : NetworkedBehaviour
    {
        public static AppEvent<SnowballBehaviour> HeldSnowballCollided = new AppEvent<SnowballBehaviour>();

        private const int AR_ENEMY_LAYER = 12;

        private INetworkedField<string> tossData;

        private Snowball snowball;
        private SnowballFightManager snowballFightManager;
        private PlayerManager playerManager;
        private ARNetworkingHelper arNetworkingHelper;

        private TossData tossDataIn = new TossData();
        private TossData tossDataOut = new TossData();

        protected override void SetupSession(out Action initializer, out int order)
        {
            initializer = () =>
            {
                snowball = GetComponent<Snowball>();

                // Subscribe to this snowball's events
                snowball.EventLocallySpawnedSnowballTossed.AddListener(OnSnowballTossed);
                snowball.EventSnowballCollided.AddListener(OnSnowballCollisionEntered);

                snowballFightManager = SceneLookup.Get<SnowballFightManager>();
                playerManager = SceneLookup.Get<PlayerManager>();
                arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();

                PlayerData spawningPlayerData = snowballFightManager.GetPlayerDataById(Owner.SpawningPeer.Identifier);
                PlayerBehaviour spawningPlayerBehaviour = spawningPlayerData.Behaviour;

                // Ignore collisions between players and their spawned snowballs.
                Physics.IgnoreCollision(spawningPlayerBehaviour.GetComponentInChildren<Collider>(),
                                        snowball.GetComponentInChildren<Collider>());

                // If this snowball wasn't spawned by the local peer, then attach it to its spwaner's PlayerBehaviour
                if (!arNetworkingHelper.IsSelf(Owner.SpawningPeer.Identifier))
                {
                    snowball.InitSnowball("Remote player " + spawningPlayerData.Name, spawningPlayerBehaviour.transform);
                }

                tossData = new NetworkedField<string>("snowballTossData",
                                                      Owner.Auth.AuthorityToObserverDescriptor(TransportType.ReliableOrdered),
                                                      Owner.Group);
                tossData.ValueChangedIfReceiver += OnTossDataValueChanged;
            };
            order = 0;
        }

        private void OnDestroy()
        {
            if (snowball != null)
            {
                snowball.EventLocallySpawnedSnowballTossed.RemoveListener(OnSnowballTossed);
                snowball.EventSnowballCollided.RemoveListener(OnSnowballCollisionEntered);
            }
        }

        private void OnSnowballTossed(Snowball snowball, float angle, Vector3 force, Vector3 torque)
        {
            tossDataOut.angle = angle;
            tossDataOut.force = force;
            tossDataOut.torque = torque;

            Debug.LogFormat("Sender: OnToss: angle={0}, force={1}, torque={2}",
                tossDataOut.angle, tossDataOut.force, tossDataOut.torque);

            tossData.SetIfSender(JsonUtility.ToJson(tossDataOut));

            tossDataOut.Reset();
        }

        private void OnTossDataValueChanged(NetworkedFieldValueChangedArgs<string> args)
        {
            string tossData = args.Value.GetOrDefault();
            JsonUtility.FromJsonOverwrite(tossData, tossDataIn);

            Debug.LogFormat("Receiver: OnTossDataValueChanged: angle={0}, force={1}, torque={2}",
                tossDataIn.angle, tossDataIn.force, tossDataIn.torque);

            snowball.TossNetworkSpawnedSnowball(tossDataIn.angle, tossDataIn.force, tossDataIn.torque);
        }

        private void OnSnowballCollisionEntered(Snowball snowball, Collision collision)
        {
            Debug.Log("OnSnowballCollisionEntered " + snowball.SpawnerDescription);

            // If snowball is held, just trigger an event so the player can handle it
            if (snowball.IsHeld)
            {
                HeldSnowballCollided.Invoke(this);
            }
            // Otherwise handle the snowball collision
            else
            {
                // If the collision hit an enemy, invoke the event
                if (collision.gameObject && collision.gameObject.layer == AR_ENEMY_LAYER)
                {
                    SnowballFightEvents.EventSnowballHitEnemy.Invoke(this);
                }

                // Have snowball handle its collision, but only destroy it if spawned by me
                snowball.HandleCollision(collision, destroy: Owner.WasSpawnedByMe);
            }
        }
    }
}
