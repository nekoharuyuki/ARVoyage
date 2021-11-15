

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Manage spawning of enemy fireflies in multiplayer SnowballFight demo.
    /// N.B.: non-host players send requests to the host of good locations to spawn new enemies;
    /// only the host does the actual spawning, broadcasted out to all players.
    /// </summary>
    public class EnemyManager : MonoBehaviour, ISceneDependency
    {
        private const float minEnemyYOffset = -0.2f;
        private const float maxEnemyYOffset = 0.7f;

        [SerializeField] public int EnemiesPerPlayer = 2;
        private int MaxEnemies { get { return Mathf.Min(4, snowballFightManager.Players.Count * EnemiesPerPlayer); } }

        [SerializeField] private float planeGridElementSize = 1f;
        private float MinEnemySpacing { get { return planeGridElementSize / 2f; } }

        [SerializeField] public float nearPlayerThresholdDist = 2.5f;

        //[SerializeField] private float respawnEnemyCooldown = 2f;

        [SerializeField] private NetworkedUnityObject enemyPrefab;

        public HashSet<EnemyBehaviour> enemies = new HashSet<EnemyBehaviour>();

        private List<Vector3> recentlyOccupiedVisibleGridElements = new List<Vector3>();

        private ARNetworkingHelper arNetworkingHelper;
        private ARPlaneHelper arPlaneHelper;
        private SnowballFightManager snowballFightManager;

        private bool spawning = false;

        void Awake()
        {
            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            arPlaneHelper = SceneLookup.Get<ARPlaneHelper>();
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();

            arNetworkingHelper.Connected.AddListener(OnConnected);
        }

        void OnDestroy()
        {
            arNetworkingHelper.Connected.RemoveListener(OnConnected);
        }

        private void OnConnected(ConnectedArgs args)
        {
            // ONLY THE HOST does the spawning.
            if (args.IsHost)
            {
                StartCoroutine(PersistentSpawnEnemies());
            }
        }


        private IEnumerator PersistentSpawnEnemies()
        {
            // Persistently spawn, always maintaining a MaxEnemies
            while (true)
            {
                // If we need more enemies
                if (spawning && enemies.Count < MaxEnemies)
                {
                    // Spawn an enemy per player
                    foreach (PlayerData playerData in snowballFightManager.Players.Values)
                    {
                        // confirm we still need more
                        if (enemies.Count < MaxEnemies && playerData.Behaviour != null)
                        {
                            // Get this player's preferred enemy spawn position
                            Vector3 spawnPoint = playerData.Behaviour.EnemySpawnPoint;
                            if (spawnPoint != default)
                            {
                                if (!IsPositionOccupied(spawnPoint))
                                {
                                    Debug.Log("Spawn enemy for " + playerData.Name);
                                    SpawnEnemy(spawnPoint);
                                }
                            }
                        }
                    }
                }

                // be sure this routine doesn't slam the CPU
                yield return null;
            }
        }


        private bool IsPositionOccupied(Vector3? pos)
        {
            if (pos == null) return false;
            Vector3 position = pos ?? Vector3.zero;

            foreach (EnemyBehaviour enemy in enemies)
            {
                if (DemoUtil.GetXZDistance(enemy.gameObject.transform.position, position) < MinEnemySpacing)
                {
                    return true;
                }
            }

            foreach (PlayerData playerData in snowballFightManager.Players.Values)
            {
                if (playerData.Behaviour != null &&
                    DemoUtil.GetXZDistance(playerData.Behaviour.gameObject.transform.position, position) < MinEnemySpacing)
                {
                    return true;
                }
            }

            return false;
        }


        private bool WasPositionRecentlyOccupied(Vector3? pos)
        {
            if (pos == null) return false;
            Vector3 position = pos ?? Vector3.zero;

            foreach (Vector3 recentPos in recentlyOccupiedVisibleGridElements)
            {
                if (DemoUtil.GetXZDistance(recentPos, position) < MinEnemySpacing)
                {
                    return true;
                }
            }

            return false;
        }


        private void SpawnEnemy(Vector3? position)
        {
            if (position == null) return;

            Vector3 enemyPos = (Vector3)position;

            // Vertically offset enemy from player's (camera's) y position,
            // tending to be above the camera
            float yOffset;
            if (Random.Range(0, 100) < 75)
            {
                yOffset = Random.Range(maxEnemyYOffset / 2f, maxEnemyYOffset);
            }
            else
            {
                yOffset = Random.Range(minEnemyYOffset, maxEnemyYOffset);
            }
            enemyPos.y = Camera.main.transform.position.y + yOffset;

            // Instantiate
            EnemyBehaviour enemy = InstantiateEnemy(enemyPos);

            string id = enemy.Owner.Id.ToString();

            Debug.Log("Adding enemy: " + id);
            enemies.Add(enemy);

            enemy.isAlive.ValueChanged += (args) =>
            {
                if (!args.Value.GetOrDefault())
                {
                    Debug.LogFormat("Removing Enemy: ID: {0} Alive: {1} Expiring: {2}",
                        id, enemy.isAlive.Value.GetOrDefault(),
                        enemy.isExpiring.Value.GetOrDefault());
                    enemies.Remove(enemy);
                }
            };
        }


        private EnemyBehaviour InstantiateEnemy(Vector3 position)
        {
            if (arNetworkingHelper.Networking == null) return null;

            return enemyPrefab.NetworkSpawn(arNetworkingHelper.Networking,
                                                    position,
                                                    Quaternion.identity,
                                                    Role.Authority
            ).DefaultBehaviour as EnemyBehaviour;
        }

        public void StartSpawning()
        {
            spawning = true;
        }

        public void StopAndExpireAll()
        {
            if (arNetworkingHelper.IsHost)
            {
                // Stop spawning.
                spawning = false;

                // Expire all remaining enemies. Use a copy of the set since expiring enemies can cause them to be removed from the enemies set.
                List<EnemyBehaviour> enemiesCopy = new List<EnemyBehaviour>(enemies);
                foreach (EnemyBehaviour enemy in enemiesCopy)
                {
                    enemy.Expire();
                }

                // Clear the enemy collection for the next session.
                enemies.Clear();
            }
        }


        public Vector3? FindEnemySpawnPosition(Transform playerTransform)
        {
            // Get latest list of AR planes
            List<ARPlane> arPlanes = arPlaneHelper.GetPlanes();

            // Get list of visible enemy placement positions on those planes, sampled using planeGridElementSize
            List<List<Vector3>> arPlanesGridElements = DemoUtil.CalculateARPlanesGridElements(arPlanes, planeGridElementSize);
            List<Vector3> visibleGridElements =
                //DemoUtil.FindCameraVisibleGridElements(arPlanesGridElements, playerTransform);
                DemoUtil.FindInFrontGridElements(arPlanesGridElements, playerTransform);

            // Filter out elements already occupied by an enemy or player
            List<Vector3> visibleUnoccupiedGridElements = new List<Vector3>();
            List<Vector3> visibleOccupiedGridElements = new List<Vector3>();
            foreach (Vector3 visibleGridElement in visibleGridElements)
            {
                if (!IsPositionOccupied(visibleGridElement))
                {
                    // avoid respawning enemies where one recently was
                    if (!WasPositionRecentlyOccupied(visibleGridElement))
                    {
                        visibleUnoccupiedGridElements.Add(visibleGridElement);
                    }
                }
                else
                {
                    visibleOccupiedGridElements.Add(visibleGridElement);
                }
            }

            // update recentlyOccupiedVisibleGridElements for next time
            recentlyOccupiedVisibleGridElements = visibleOccupiedGridElements;

            // If there are visible currently+previously unoccupied grid elements
            float dist;
            if (visibleUnoccupiedGridElements.Count > 0)
            {
                // Get sublist of nearby grid elements
                List<Vector3> nearbyGridElements = new List<Vector3>();
                foreach (Vector3 gridElement in visibleUnoccupiedGridElements)
                {
                    dist = DemoUtil.GetXZDistance(gridElement, Camera.main.transform.position);
                    if (dist < nearPlayerThresholdDist)
                    {
                        nearbyGridElements.Add(gridElement);
                    }
                }

                // 50% chance of choosing a nearby grid element
                Vector3 enemyPlacementPoint;
                if (nearbyGridElements.Count > 0 && UnityEngine.Random.Range(0, 100) < 50)
                {
                    enemyPlacementPoint = nearbyGridElements[UnityEngine.Random.Range(0, nearbyGridElements.Count - 1)];
                }

                // else choose any visible unoccupied grid element
                else
                {
                    enemyPlacementPoint = visibleUnoccupiedGridElements[UnityEngine.Random.Range(0, visibleUnoccupiedGridElements.Count - 1)];
                }

                // Add some xz variation within this grid element, 
                // in case we're forced to respawn in the same grid element as a previous enemy

                //  Only do this if the enemy spawn position is near the player, 
                //  to try to avoid placing enemies toward back edge of an AR plane, 
                //  to reduce accidental occlusion by room walls
                dist = DemoUtil.GetXZDistance(enemyPlacementPoint, Camera.main.transform.position);
                if (dist < 3f)
                {
                    float variation = planeGridElementSize / 2f;
                    enemyPlacementPoint.x += UnityEngine.Random.Range(-variation, variation);
                    enemyPlacementPoint.z += UnityEngine.Random.Range(-variation, variation);
                }

                return enemyPlacementPoint;
            }

            // no placement points are visible
            return null;
        }

    }
}
