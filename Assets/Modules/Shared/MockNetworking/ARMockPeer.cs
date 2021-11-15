using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.Networking.HLAPI.Authority;
using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// A mock peer used for running SnowballFight in Unity Editor
    /// Provides keyboard shortcuts for having mock peers perform basic gameplay actions for testing
    /// </summary>
    public class ARMockPeer : MonoBehaviour
    {
        [SerializeField] NetworkedUnityObject playerPrefab;
        [SerializeField] NetworkedUnityObject snowballPrefab;

        PlayerBehaviour playerBehaviour;

        private IARNetworking arNetworking;

        private SnowballBehaviour heldSnowballBehaviour;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) LeaveSession();
            if (Input.GetKeyDown(KeyCode.J)) AttackRandomEnemy();
            if (Input.GetKeyDown(KeyCode.K)) AttackRandomPlayer();
            if (Input.GetKeyDown(KeyCode.L)) AttackHost();
            if (Input.GetKeyDown(KeyCode.Semicolon)) SpawnHeldSnowball();
        }

        public void Initialize(IARNetworking arNetworking)
        {
            this.arNetworking = arNetworking;
            arNetworking.Networking.PeerDataReceived += OnPeerDataReceived;

            playerBehaviour = playerPrefab.NetworkSpawn(
                                              arNetworking.Networking,
                                              Vector3.zero,
                                              Quaternion.identity,
                                              Role.Authority
           ).DefaultBehaviour as PlayerBehaviour;

            playerBehaviour.ShowDebugPlayer(true);

            StartCoroutine(SetReadyRoutine(playerBehaviour));
            StartCoroutine(SnowballTossRoutine());

        }

        private void OnPeerDataReceived(PeerDataReceivedArgs args)
        {
            if (args.Tag == ARNetworkingHelper.AcknowledgeMessageTag)
            {
                ARNetworkingHelper.AcknowledgeMessageState state =
                    (ARNetworkingHelper.AcknowledgeMessageState)args.CopyData()[0];

                Debug.Log("Peer Acknowledged: " + state);

                if (state == ARNetworkingHelper.AcknowledgeMessageState.Rejected)
                {
                    Debug.Log("Peer Rejected: Leaving Session");
                    arNetworking.Dispose();
                    Destroy(gameObject);
                }
            }
        }

        private IEnumerator SetReadyRoutine(PlayerBehaviour playerBehaviour)
        {
            if (DevSettings.SkipToSnowballFightMainInEditor)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(1);
            }
            playerBehaviour.SetReady(true);
        }

        private IEnumerator SnowballTossRoutine()
        {
            if (arNetworking.Networking == null || arNetworking.Networking.IsConnected == false) yield return null;

            while (true)
            {
                //Uncomment for random enemy attacking.
                //AttackRandomEnemy();

                // Wait for next attack.
                yield return new WaitForSeconds(5);
            }

        }

        public void LeaveSession()
        {
            arNetworking.Networking.Leave();
        }

        public void AttackRandomEnemy()
        {
            EnemyManager enemyManager = SceneLookup.Get<EnemyManager>();
            if (enemyManager.enemies.Count > 0)
            {
                // Pick a target.
                EnemyBehaviour[] enemyArray = enemyManager.enemies.ToArray();
                EnemyBehaviour randomEnemy = enemyArray[Random.Range(0, enemyArray.Length)];

                AttackTarget(transform, randomEnemy.transform);
            }
        }

        public void AttackRandomPlayer()
        {
            if (arNetworking.Networking == null) return;

            SnowballFightManager snowballFightManager = SceneLookup.Get<SnowballFightManager>();

            List<PlayerData> otherPlayers = snowballFightManager.Players.Values.Where(
                playerData =>
                playerData.Behaviour != null && arNetworking != null && arNetworking.Networking != null
                    && playerData.Behaviour.Owner.SpawningPeer != arNetworking.Networking.Self).ToList();

            if (otherPlayers.Count > 0)
            {
                PlayerData randomPlayerData = otherPlayers.ElementAt(Random.Range(0, otherPlayers.Count));
                AttackTarget(transform, randomPlayerData.Behaviour.PlayerBodyTransform);
            }
        }

        public void AttackHost()
        {
            AttackTarget(transform, Camera.main.transform);
        }

        public void AttackTarget(Transform source, Transform target)
        {
            if (heldSnowballBehaviour == null)
            {
                SpawnHeldSnowball();
            }

            // Look at target.
            Vector3 lookTarget = target.position;
            lookTarget.y = transform.position.y;
            this.transform.LookAt(lookTarget);

            // Toss networked snowball with zero velocity.
            Snowball snowball = heldSnowballBehaviour.GetComponent<Snowball>();
            snowball.TossNetworkSpawnedSnowball(0, Vector3.zero, Vector3.zero);

            // Lerp it instead.
            float duration = Random.Range(.5f, 1f);
            InterpolationUtil.LinearInterpolation(snowball.gameObject, snowball.gameObject,
                duration,
                onUpdate: (t) =>
                {
                    if (snowball != null)
                    {
                        snowball.transform.position = Vector3.Lerp(source.position, target.position, t);
                    }
                });

            heldSnowballBehaviour = null;
        }

        public void SpawnHeldSnowball()
        {
            if (heldSnowballBehaviour == null)
            {
                // Create a snowball.
                heldSnowballBehaviour = snowballPrefab.NetworkSpawn(
                                                   arNetworking.Networking,
                                                   transform.position,
                                                   Quaternion.identity,
                                                   Role.Authority
                ).DefaultBehaviour as SnowballBehaviour;
            }
        }
    }
}