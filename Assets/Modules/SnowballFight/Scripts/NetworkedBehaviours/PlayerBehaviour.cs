using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;

using System;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// A network-spawned PlayerBehaviour
    /// This class tracks the networked states for each player peer in the session.
    /// This includes selecting positions to spawn enemies for this player, when spawning new enemies.
    /// This component manages its corresponding Player component.
    /// </summary>
    [RequireComponent(typeof(AuthBehaviour))]
    public class PlayerBehaviour : NetworkedBehaviour
    {
        private const int ARSnowballLayer = 13;

        [SerializeField] private Player player;
        [SerializeField] private GameObject playerNameLabel;
        [SerializeField] private TMPro.TMP_Text playerNameText;

        private INetworkedField<string> playerName;
        private INetworkedField<byte> ready;
        private INetworkedField<int> score;

        // Current preferred position to spawn an enemy
        // e.g. visible to player, possibly near player, unoccupied by existing enemy
        private INetworkedField<Vector3> enemySpawnPoint;

        // How often to re-choose a suggested enemy spawn point
        // Important to re-choose this frequently enough to allow new enemies to cluster around a player
        [SerializeField] private float enemySpawnPointRefreshSecs = 2f;
        private float lastEnemySpawnPointUpdateTime;

        private ARNetworkingHelper arNetworkingHelper;
        private EnemyManager enemyManager;
        private SnowballFightManager snowballFightManager;

        [SerializeField] private MeshRenderer playerColliderRenderer;

        public string Name
        {
            get
            {
                if (playerName == null)
                {
                    return null;
                }
                return playerName.Value.GetOrDefault();
            }
        }

        public bool IsReady
        {
            get
            {
                if (ready == null)
                {
                    return false;
                }
                return Convert.ToBoolean(ready.Value.GetOrDefault());
            }
        }

        public int Score
        {
            get
            {
                if (score == null)
                {
                    return 0;
                }
                return score.Value.GetOrDefault();
            }
        }

        public Vector3 EnemySpawnPoint
        {
            get { return enemySpawnPoint.Value.GetOrDefault(); }
        }

        public Transform PlayerBodyTransform
        {
            get { return player.transform; }
        }

        void Start()
        {
            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();
            enemyManager = SceneLookup.Get<EnemyManager>();

            SnowballFightEvents.EventSnowballHitEnemy.AddListener(OnSnowballHitEnemy);
        }

        void Update()
        {
            // Find and set a new enemy spawn position.
            // N.B.: Only the host's EnemyManager will spawn enemies, 
            // using this value and values from other players
            if (Time.time - enemySpawnPointRefreshSecs > lastEnemySpawnPointUpdateTime)
            {
                lastEnemySpawnPointUpdateTime = Time.time;

                Vector3? position = enemyManager.FindEnemySpawnPosition(transform);
                if (position != null && enemySpawnPoint != null)
                {
                    enemySpawnPoint.SetIfSender((Vector3)position);
                }
            }
        }

        void OnDrawGizmos()
        {
            if (enemySpawnPoint != null)
            {
                // Draw a yellow sphere at the desired spawn point.
                Vector3 spawnPoint = enemySpawnPoint.Value.GetOrDefault();
                if (spawnPoint != default)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(spawnPoint, .1f);
                }
            }
        }

        public void ShowDebugPlayer(bool show)
        {
            playerColliderRenderer.enabled = show;
        }

        private void OnSnowballHitEnemy(SnowballBehaviour snowballBehaviour)
        {
            // If this player spawned this snowball, handle score increment
            if (arNetworkingHelper.WasSpawnedByPeer(Owner.SpawningPeer, snowballBehaviour))
            {
                // Only set score if I'm the sender. This allows mock players in editor to increment their scores.
                score.SetIfSender(score.Value.GetOrDefault() + snowballFightManager.ScoreIncrementPerEnemy);

                // If I'm the local player, send the event that I've incremented my score
                if (arNetworkingHelper.IsSelf(Owner.SpawningPeer.Identifier))
                {
                    SnowballFightEvents.EventLocalPlayerScoreChanged.Invoke(score.Value.Value);
                }

                Debug.Log(Owner.SpawningPeer.Identifier + " changed score to " + score.Value.Value);
            }
        }

        private void OnPlayerCollisionEnter(Collision collision)
        {
            // If it was a snowball, trigger the appropriate effects
            if (collision.gameObject.layer == ARSnowballLayer)
            {
                OnHitBySnowball();
            }
        }

        private void OnHeldSnowballCollided(SnowballBehaviour snowballBehaviour)
        {
            // If this player spawned this held snowball, handle it as though this player was hit
            // since the held snowball won't burst
            if (arNetworkingHelper.WasSpawnedByPeer(Owner.SpawningPeer, snowballBehaviour))
            {
                OnHitBySnowball();
            }
        }

        private void OnHitBySnowball()
        {
            // If I'm the local peer
            if (arNetworkingHelper.IsSelf(Owner.SpawningPeer.Identifier))
            {
                // Trigger Screen Space fx
                SnowballFightEvents.EventLocalPlayerHit.Invoke();
            }
            else
            {
                // Trigger World Space fx
                player.TriggerHitEffects();
            }
        }

        private void OnDestroy()
        {
            player.CollisionEnter.RemoveListener(OnPlayerCollisionEnter);
            SnowballFightEvents.EventSnowballHitEnemy.RemoveListener(OnSnowballHitEnemy);
            SnowballBehaviour.HeldSnowballCollided.RemoveListener(OnHeldSnowballCollided);
        }

        protected override void SetupSession(out Action initializer, out int order)
        {
            initializer = () =>
            {
                player.CollisionEnter.AddListener(OnPlayerCollisionEnter);

                playerName = new NetworkedField<string>("playerName",
                                                        Owner.Auth.AnyToAnyDescriptor(TransportType.ReliableUnordered),
                                                        Owner.Group);

                ready = new NetworkedField<byte>("playerReady",
                                                 Owner.Auth.AuthorityToObserverDescriptor(TransportType.ReliableUnordered),
                                                 Owner.Group);
                ready.ValueChanged += OnReadyValueChanged;

                score = new NetworkedField<int>("playerScore",
                                                 Owner.Auth.AuthorityToObserverDescriptor(TransportType.ReliableUnordered),
                                                 Owner.Group);

                enemySpawnPoint = new NetworkedField<Vector3>(
                    "playerEnemySpawnPoint",
                    Owner.Auth.AuthorityToObserverDescriptor(TransportType.ReliableUnordered),
                    Owner.Group);

                SnowballBehaviour.HeldSnowballCollided.AddListener(OnHeldSnowballCollided);
            };
            order = 0;
        }

        public void InitHUD()
        {
            // Hide display of my own player name label
            if (arNetworkingHelper.IsSelf(Owner.SpawningPeer.Identifier))
            {
                playerNameLabel.gameObject.SetActive(false);
            }

            // Fill in on-screen name labels of the other players
            else
            {
                foreach (PlayerData playerData in snowballFightManager.Players.Values)
                {
                    playerData.Behaviour.playerNameLabel.SetActive(true);
                    playerData.Behaviour.playerNameText.text = playerData.Name;
                }
            }
        }

        // Sender
        public void SetName(string name)
        {
            playerName.Value = name;
        }

        public void SetReady(bool isReady)
        {
            ready.SetIfSender(Convert.ToByte(isReady));

            // reset score to 0 when ready to start
            if (isReady)
            {
                score.SetIfSender(0);
            }
        }

        // Receiver
        private void OnReadyValueChanged(NetworkedFieldValueChangedArgs<byte> args)
        {
            bool ready = Convert.ToBoolean(args.Value.GetOrDefault());
            Debug.LogFormat("Receiver: OnReadyValueChanged: ready={0}", ready);

            snowballFightManager.OnPlayerReadyValueChanged(Owner.SpawningPeer.Identifier, ready);
        }
    }
}
