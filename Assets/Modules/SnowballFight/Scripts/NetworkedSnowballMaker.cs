using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Extension of SnowballMaker with a customized implementation of InstantiatePrefab that
    /// performans a network spawn of its snowball so it'll be shared with all peers
    /// </summary>
    public class NetworkedSnowballMaker : SnowballMaker
    {
        private ARNetworkingHelper arNetworkingHelper;
        private PlayerManager playerManager;

        void Start()
        {
            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            playerManager = SceneLookup.Get<PlayerManager>();
        }

        public override GameObject InstantiatePrefab()
        {
            if (arNetworkingHelper.Networking == null)
            {
                Debug.Log("AR Networking session hasn't been inialized yet.");
                return null;
            }

            NetworkedUnityObject networkedSnowballPrefab = snowballPrefab.GetComponent<NetworkedUnityObject>();
            if (networkedSnowballPrefab == null)
            {
                Debug.LogError("The Snowball Prefab doesn't contain a NetworkedUnityObject component.");
                return base.InstantiatePrefab();
            }

            GameObject snowball = networkedSnowballPrefab.NetworkSpawn(arNetworkingHelper.Networking,
                                                                       Vector3.zero,
                                                                       Quaternion.identity,
                                                                       Role.Authority
            ).gameObject;

            return snowball;
        }
    }
}