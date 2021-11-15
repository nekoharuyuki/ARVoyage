using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.Networking.HLAPI.Routing;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Extensions;

using System;
using System.Collections;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Helper convenience functions for managing the ARDK network session.
    /// Also wraps and relays ARDK network events.
    /// </summary>
    public class ARNetworkingHelper : MonoBehaviour, ISceneDependency
    {
        // A message with this tag and state data is sent by the host to each peer that joins
        // A peer must be acknowledged in order to enter the session
        public const uint AcknowledgeMessageTag = 404;
        public enum AcknowledgeMessageState : byte
        {
            Waiting,
            Acknowledged,
            Rejected
        }

        // ARDK's NetworkSessionManager can be found in the scene as a component of ARNetworkingSceneManager
        // (ARDK/Helpers/Prefabs/ARNetworkingSceneManager.prefab)
        //
        // Since there is more than one entry point to input the Session ID in this demo
        // we need to bypass the NetworkSessionManager's Session ID grab directly from the InputField
        // and set it explicitly.
        [SerializeField] private NetworkSessionManager networkSessionManager;

        // ARDK's ARNetworkingManager can be found in the scene as a component of ARNetworkingSceneManager
        // (ARDK/Helpers/Prefabs/ARNetworkingSceneManager.prefab)
        //
        // ARNetworkingManager 'Manage Using Unity Lifecycle' should be set to true in the inspector
        // so that, once enabled, it will trigger the networking initialization process.
        [SerializeField] private ARNetworkingManager networkingManager;

        [SerializeField] public int sessionIdLength = 4;

        public string SessionId { get; private set; } = "";
        public AcknowledgeMessageState HostAcknowledgeState { get; private set; } =
               AcknowledgeMessageState.Waiting;

        // Reference to the networking session
        private IARNetworking arNetworking;

        // Networking structures used to allow the host to communicate
        // with observers without spawning NetworkedUnityObjects (e.g. start game, winner ID)
        private IHlapiSession hlapiSession;
        private IAuthorityReplicator authorityReplicator;

        // Did the local user select the option to join an existing session?
        public bool Joined { get; private set; }

        private uint lastNetworkError = 0;

        // The player's networking info
        private IPeer self;
        public bool IsHost { get; private set; }
        public bool IsConnected
        {
            get
            {
                return arNetworking != null ? arNetworking.Networking.IsConnected : false;
            }
        }
        public bool IsLocalized
        {
            get
            {
                return arNetworking != null ? arNetworking.LocalPeerState == PeerState.Stable : false;
            }
        }
        public IMultipeerNetworking Networking
        {
            get
            {
                return arNetworking != null ? arNetworking.Networking : null;
            }
        }

        public AppEvent<ConnectedArgs> Connected = new AppEvent<ConnectedArgs>();

        // Other managers
        private SnowballFightManager snowballFightManager;

        private void Start()
        {
            snowballFightManager = SceneLookup.Get<SnowballFightManager>();

            ARNetworkingFactory.ARNetworkingInitialized += OnARNetworkingInitialized;

            NetworkSpawner.NetworkObjectSpawned += OnNetworkObjectSpawned;
            NetworkSpawner.NetworkObjectDestroyed += OnNetworkObjectDestroyed;
        }

        private void Update()
        {
            // Send updates from networking channels
            if (hlapiSession != null)
            {
                hlapiSession.SendQueuedData();
            }
        }

        private void OnDestroy()
        {
            ARNetworkingFactory.ARNetworkingInitialized -= OnARNetworkingInitialized;

            NetworkSpawner.NetworkObjectSpawned -= OnNetworkObjectSpawned;
            NetworkSpawner.NetworkObjectDestroyed -= OnNetworkObjectDestroyed;

            if (arNetworking != null)
            {
                arNetworking.Networking.Connected -= OnConnected;
                arNetworking.Networking.PeerAdded -= OnPeerAdded;
                arNetworking.Networking.PeerDataReceived -= OnPeerDataReceived;
                arNetworking.PeerStateReceived -= OnPeerStateReceived;
            }
        }

        // Called by the players that choose the 'Join' option.
        public void InitAndJoin(string sessionId, bool joined = true)
        {
            this.SessionId = sessionId;

            // First set the session ID
            networkSessionManager.SetSessionIdentifier(sessionId);

            // Then enable the networkingMananger. It is set to manage using Unity lifecycle and will properly
            // manage both the ARSessionManager and the NetworkSessionManager
            networkingManager.enabled = true;

            Joined = joined;

            Debug.Log("InitAndJoin sessionId " + sessionId);
        }

        // Called by the players that choose the 'Host' option.
        public void InitAndHost()
        {
            // generate a sessionId string
            string sessionId = GenerateSessionID(sessionIdLength);

            InitAndJoin(sessionId, joined: false);
        }

        // Generate a random session ID when hosting
        private string GenerateSessionID(int sessionIdLength)
        {
            return RandomString.Generate(sessionIdLength);
        }

        public bool IsSelf(string playerId)
        {
            return playerId == self.Identifier.ToString();
        }

        public bool IsSelf(Guid playerId)
        {
            return playerId == self.Identifier;
        }

        // Was this NetworkedBehaviour spawned by this peer? 
        public bool WasSpawnedByPeer(IPeer peer, NetworkedBehaviour networkedBehaviour)
        {
            return networkedBehaviour != null && networkedBehaviour.Owner != null && networkedBehaviour.Owner.SpawningPeer == peer;
        }

        #region ARDK Events

        private void OnARNetworkingInitialized(AnyARNetworkingInitializedArgs args)
        {
            if (arNetworking != null) return;

            arNetworking = args.ARNetworking;
            arNetworking.Networking.PeerAdded += OnPeerAdded;
            arNetworking.Networking.PeerDataReceived += OnPeerDataReceived;
            arNetworking.Networking.Connected += OnConnected;
            arNetworking.PeerStateReceived += OnPeerStateReceived;
            arNetworking.PeerPoseReceived += OnPeerPoseReceived;

            // Error handling
            arNetworking.Networking.ConnectionFailed += OnConnectionFailed;
        }

        private void OnConnected(ConnectedArgs args)
        {
            lastNetworkError = 0;

            self = args.Self;
            IsHost = args.IsHost;

            hlapiSession = new HlapiSession(23445);

            INetworkGroup networkGroup = hlapiSession.CreateAndRegisterGroup(new NetworkId(53422));

            authorityReplicator = new GreedyAuthorityReplicator("SnowballFightHLAPIAuth", networkGroup);
            authorityReplicator.TryClaimRole(IsHost ? Role.Authority : Role.Observer, () => { }, () => { });

            // Host always self acknowledges.
            if (args.IsHost) HostAcknowledgeState = AcknowledgeMessageState.Acknowledged;

            snowballFightManager.InitializeNetworkFields(authorityReplicator, networkGroup);
            Connected?.Invoke(args);
        }

        private void OnPeerAdded(PeerAddedArgs args)
        {
            if (IsHost)
            {
                int totalPeers = arNetworking.Networking.OtherPeers.Count + 1;
                Debug.Log("OnPeerAdded: Count: " + totalPeers);

                AcknowledgeMessageState state = (totalPeers <= SnowballFightManager.MaxPlayers) ?
                    AcknowledgeMessageState.Acknowledged :
                    AcknowledgeMessageState.Rejected;
                StartCoroutine(PeerAcknowledgeRoutine(args, state));
            }
        }

        private IEnumerator PeerAcknowledgeRoutine(PeerAddedArgs args,
            AcknowledgeMessageState state)
        {
            yield return new WaitForSeconds(1);

            // Acknowledge the peer and tell them if there is room or not.
            uint tag = AcknowledgeMessageTag;
            byte[] data = new byte[] { (byte)state };

            arNetworking.Networking.SendDataToPeer(tag, data,
                args.Peer, TransportType.ReliableUnordered, false);
        }

        private void OnPeerDataReceived(PeerDataReceivedArgs args)
        {
            if (args.Tag == AcknowledgeMessageTag)
            {
                HostAcknowledgeState = (AcknowledgeMessageState)args.CopyData()[0];
                Debug.Log("Peer Acknowledged: " + HostAcknowledgeState);
            }
        }

        private void OnPeerStateReceived(PeerStateReceivedArgs args)
        {
            Debug.LogFormat("OnPeerStateReceived: peerId={0}, state={1}", args.Peer.Identifier, args.State);
            snowballFightManager.OnPlayerStateReceived(args.Peer.Identifier);
        }

        private void OnPeerPoseReceived(PeerPoseReceivedArgs args)
        {
            snowballFightManager.OnPlayerPoseReceived(args.Peer.Identifier, args.Pose);
        }

        private void OnNetworkObjectSpawned(NetworkObjectLifecycleArgs args)
        {
            Debug.LogFormat("OnNetworkObjectSpawned: peerId={0}", args.Peer.Identifier);
            if (args.Object.DefaultBehaviour is PlayerBehaviour)
            {
                snowballFightManager.OnPlayerAvatarSpawned(args.Peer.Identifier, args.Object.DefaultBehaviour as PlayerBehaviour);
            }
        }

        private void OnConnectionFailed(ConnectionFailedArgs args)
        {
            var errorCode = args.ErrorCode;
            if (lastNetworkError == errorCode)
            {
                return;
            }

            lastNetworkError = errorCode;
            SnowballFightEvents.EventConnectionFailed.Invoke(errorCode);
        }

        private void OnNetworkObjectDestroyed(NetworkObjectLifecycleArgs args)
        {
            Debug.LogFormat("OnNetworkObjectDestroyed: peerId={0}", args.Peer.Identifier);
            if (args.Object.DefaultBehaviour is PlayerBehaviour)
            {
                snowballFightManager.OnPlayerAvatarDestroyed(args.Peer.Identifier);
            }
        }

        #endregion // ARDK Events
    }
}
