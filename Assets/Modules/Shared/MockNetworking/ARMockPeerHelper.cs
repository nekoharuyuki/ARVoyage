using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.VirtualStudio;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    public class MockMessageHandler : MessageHandlerBase
    {
        public override void HandleMessage(PeerDataReceivedArgs args)
        {
            /*
            Debug.LogFormat
            (
              "[Message Received] Tag: {0}, Sender: {1}, Data Length: {2}",
              args.Tag,
              args.Peer,
              args.DataLength
            );
            */
        }
    }

    /// <summary>
    /// Helper for creating and managing mock peers when testing SnowballFight in Unity editor.
    /// Automatically creates mock peers as specified in the ARVoyageMockPlayConfiguration.
    /// </summary>
    public class ARMockPeerHelper : MonoBehaviour
    {
        [SerializeField] NetworkedUnityObject playerPrefab;
        [SerializeField] MockPlayConfiguration mockPlayConfiguration;

        private IARNetworking arNetworking;
        private ARNetworkingHelper arNetworkingHelper;
        private int peerCount = 0;

        private void Awake()
        {
            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            arNetworkingHelper.Connected.AddListener(OnConnected);

            ARNetworkingFactory.ARNetworkingInitialized += OnAnyInitialized;
        }

        private void OnDestroy()
        {
            arNetworkingHelper.Connected.RemoveListener(OnConnected);

            ARNetworkingFactory.ARNetworkingInitialized -= OnAnyInitialized;
        }

        private void OnConnected(ConnectedArgs args)
        {
            StartCoroutine(OnConnectedRoutine(args));
        }

        private IEnumerator OnConnectedRoutine(ConnectedArgs args)
        {
            yield return null;
            CreateMockPeers();
        }

        private void OnAnyInitialized(AnyARNetworkingInitializedArgs args)
        {
            if (arNetworking != null) return;

            arNetworking = args.ARNetworking;
            arNetworking.Deinitialized += OnDeinitialized;
            arNetworking.Networking.PeerAdded += OnPeerAdded;
        }

        private void OnDeinitialized(ARNetworkingDeinitializedArgs args)
        {
            if (arNetworking == null) return;

            arNetworking.Deinitialized -= OnDeinitialized;
            arNetworking.Networking.PeerAdded -= OnPeerAdded;

            arNetworking = null;
        }

        private void OnPeerAdded(PeerAddedArgs args)
        {
            Debug.LogFormat("PeerAdded: Id: {0}, Self: {1}, Host: {2}",
                args.Peer.Identifier,
                args.Peer == arNetworking.Networking.Self,
                args.Peer == arNetworking.Networking.Host);

            if (args.Peer != arNetworking.Networking.Host)
            {
                Debug.Log("Creating mock peer");
                StartCoroutine(OnPeerAddedRoutine(args));
            }
        }


        private IEnumerator OnPeerAddedRoutine(PeerAddedArgs args)
        {
            MockPlayer mockPlayer = mockPlayConfiguration.GetPlayerWithPeer(args.Peer);

            // Wait until we have a gameobject.
            yield return new WaitUntil(() => { return mockPlayer.GameObject != null; });

            Vector3 randomPostion = Random.onUnitSphere;
            randomPostion.Scale(new Vector3(2, 0, 2));
            mockPlayer.GameObject.transform.position = randomPostion;
            mockPlayer.SetMessageHandler(new MockMessageHandler());

            ARMockPeer arMockPeer = mockPlayer.GameObject.GetComponent<ARMockPeer>();
            arMockPeer?.Initialize(mockPlayer.ARNetworking);


            peerCount++;
        }

        public void CreateMockPeers()
        {
            var sessionID = arNetworkingHelper.SessionId;
            mockPlayConfiguration.ConnectAllPlayersNetworkings(
                System.Text.Encoding.ASCII.GetBytes(sessionID)
            );

            var arConfiguration = ARWorldTrackingConfigurationFactory.Create();
            arConfiguration.PlaneDetection = PlaneDetection.Horizontal | PlaneDetection.Vertical;
            arConfiguration.IsSharedExperienceEnabled = true;
            mockPlayConfiguration.RunAllPlayersARSessions(arConfiguration);
        }

#if UNITY_EDITOR && FALSE
        void OnGUI()
        {
            if (GUILayout.Button("Create Mock Peers"))
            {
                CreateMockPeers();
            }
        }
#endif
    }

}
