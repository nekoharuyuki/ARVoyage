using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Manages the local player, including network spawning the player's PlayerBehaviour and updating its position
    /// </summary>
    public class PlayerManager : MonoBehaviour, ISceneDependency
    {
        [SerializeField] private NetworkedUnityObject playerPrefab;

        public PlayerBehaviour player;

        private ARNetworkingHelper arNetworkingHelper;
        private IARSession arSession;

        public bool IsPlayerNamed
        {
            get
            {
                if (player == null)
                {
                    return false;
                }
                return player.Name != null;
            }
        }

        public bool IsPlayerReady
        {
            get
            {
                if (player == null)
                {
                    return false;
                }
                return player.IsReady;
            }
        }


        void Start()
        {
            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            ARSessionFactory.SessionInitialized += OnARSessionInitialized;
        }

        private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
        {
            arSession = args.Session;
            arSession.FrameUpdated += OnFrameUpdated;
        }

        private void OnFrameUpdated(FrameUpdatedArgs args)
        {
            Vector3 cameraPosition = args.Frame.Camera.Transform.ToPosition();
            Quaternion cameraRotation = args.Frame.Camera.Transform.ToRotation();

            if (player != null)
            {
                player.transform.position = cameraPosition;
                player.transform.rotation = Quaternion.Euler(0f, cameraRotation.eulerAngles.y, 0f);
            }
        }

        // Each player will spawn an avatar that they're the authority of.
        public void SpawnPlayer()
        {
            if (arNetworkingHelper.Networking == null)
            {
                return;
            }

            // Sanity-check: don't instantiate the player avatar more than once.
            if (player != null)
            {
                return;
            }

            player = playerPrefab.NetworkSpawn(arNetworkingHelper.Networking,
                                               Vector3.zero,
                                               Quaternion.identity,
                                               Role.Authority
            ).DefaultBehaviour as PlayerBehaviour;
        }

        public void SetPlayerReady(bool isReady)
        {
            if (player != null)
            {
                player.SetReady(isReady);
            }
        }
    }
}
