using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Utilities;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Constants, GameObjects, game state, and helper methods used by 
    /// various State classes in the SnowballFight demo
    /// </summary>
    public class SnowballFightManager : MonoBehaviour, ISceneDependency
    {
        public const int gameDuration = 45;
        public const int nearGameEndDuration = 5;

        // Fixed physics timestep for this scene is only 30fps to conserve power as recommended by Niantic
        public const float SnowballFightFixedTimestep = 1f / 30f;

        // Game config
        [SerializeField] private List<string> playerNames = new List<string>();
        [SerializeField] public int ScoreIncrementPerEnemy = 100;

        public int MinVictoryPoints { get { return Players.Count / 2; } }

        public readonly Dictionary<Guid, PlayerData> Players = new Dictionary<Guid, PlayerData>();
        public const int MaxPlayers = 4;

        private readonly List<Guid> readyPlayers = new List<Guid>();

        // Game start Network Field to be broadcast by the host to all other players
        private INetworkedField<byte> gameStart;

        // Game control variables
        public bool IsGameOver
        {
            get
            {
                return gameStart == null ? false : !Convert.ToBoolean(gameStart.Value.GetOrDefault());
            }
        }

        private bool playerCollidersVisible = false;
        public bool PlayerCollidersVisible
        {
            get
            {
                return playerCollidersVisible;
            }
            set
            {
                ShowPlayerColliders(value);
                playerCollidersVisible = value;
            }
        }

        public bool AllPlayersReady
        {
            get
            {
                return readyPlayers.Count == Players.Count;
            }
        }


        private ARNetworkingHelper arNetworkingHelper;
        private PlayerManager playerManager;

        private List<string> shuffledPlayerNames;
        private int nextNameIndex;

        void Awake()
        {
            arNetworkingHelper = SceneLookup.Get<ARNetworkingHelper>();
            playerManager = SceneLookup.Get<PlayerManager>();

            // Shuffle the player names.
            shuffledPlayerNames = new List<string>(playerNames);
            ListShuffler.Shuffle(shuffledPlayerNames);

            // When loading into SnowballFight, set a custom physics timestep to decrease power
            // drain for the multiplayer experience
            Time.fixedDeltaTime = SnowballFightFixedTimestep;
        }

        public void InitializeNetworkFields(IAuthorityReplicator authorityReplicator, INetworkGroup networkGroup)
        {
            NetworkedDataDescriptor authorityToObserverDescriptor = authorityReplicator
                    .AuthorityToObserverDescriptor(TransportType.ReliableUnordered);

            gameStart = new NetworkedField<byte>("gameStart",
                                                 authorityToObserverDescriptor,
                                                 networkGroup);
            gameStart.ValueChanged += OnGameStartValueChanged;
        }

        private void OnGameStartValueChanged(NetworkedFieldValueChangedArgs<byte> args)
        {
            bool gameStarted = Convert.ToBoolean(gameStart.Value.GetOrDefault());
            Debug.LogFormat("Receiver: OnGameStartValueChanged: gameStarted={0}", gameStarted);

            if (gameStarted)
            {
                SnowballFightEvents.EventGameStart.Invoke();
            }
        }

        public void OnPlayerStateReceived(Guid playerId)
        {
            PlayerBehaviour behaviour = null;
            if (Players.TryGetValue(playerId, out PlayerData player))
            {
                behaviour = player.Behaviour;
            }
            UpdatePlayer(playerId, behaviour);

            if (arNetworkingHelper.IsSelf(playerId))
            {
                playerManager.SpawnPlayer();
            }
        }

        public void OnPlayerAvatarSpawned(Guid playerId, PlayerBehaviour behaviour)
        {
            // The host assigns the player's name when the player spawns
            if (arNetworkingHelper.IsHost && behaviour != null && behaviour.Name == null)
            {
                nextNameIndex = (nextNameIndex + 1) % shuffledPlayerNames.Count;
                string playerName = shuffledPlayerNames[nextNameIndex];
                behaviour.SetName(playerName);
            }

            UpdatePlayer(playerId, behaviour);
        }

        public void OnPlayerPoseReceived(Guid playerId, Matrix4x4 pose)
        {
            if (Players.TryGetValue(playerId, out PlayerData player))
            {
                if (player.IsReady)
                {
                    player.Behaviour.gameObject.transform.position = pose.ToPosition();
                    player.Behaviour.gameObject.transform.rotation = Quaternion.Euler(0f, pose.ToRotation().eulerAngles.y, 0f);
                }
            }
        }

        private void UpdatePlayer(Guid playerId, PlayerBehaviour behaviour)
        {
            bool isHost = arNetworkingHelper.Networking.Host.Identifier == playerId;
            PlayerData player = new PlayerData(arNetworkingHelper.IsSelf(playerId), isHost, behaviour);
            Players[playerId] = player;

            Debug.LogFormat("Player updated: player={0}", Players[playerId].ToString());
        }

        public PlayerData GetPlayerDataById(Guid playerId)
        {
            if (Players.TryGetValue(playerId, out PlayerData playerData))
            {
                return playerData;
            }
            else
            {
                return default(PlayerData);
            }
        }

        public void ShowPlayerColliders(bool show)
        {
            Debug.Log("PLAYERS: " + Players.Count);
            foreach (PlayerData playerData in Players.Values)
            {
                Debug.Log(playerData);
                if (!playerData.IsSelf)
                {
                    Debug.LogFormat("Player View:  Player: {0} Show: {1}", playerData.Name, show);
                    playerData.Behaviour.ShowDebugPlayer(show);
                }
            }
        }

        public void OnPlayerReadyValueChanged(Guid playerId, bool ready)
        {
            if (Players.TryGetValue(playerId, out PlayerData playerData))
            {
                if (ready)
                {
                    readyPlayers.Add(playerId);
                }
                else
                {
                    readyPlayers.Remove(playerId);
                }
            }
        }

        public void OnPlayerAvatarDestroyed(Guid playerId)
        {
            Debug.Log("SnowballFightManager.OnPlayerAvatarDestroyed: Removing destroyed player.");

            Players.Remove(playerId);
            readyPlayers.Remove(playerId);
        }

        public void StartGame()
        {
            gameStart.SetIfSender(Convert.ToByte(true));
        }

        public void EndGame()
        {
            gameStart.SetIfSender(Convert.ToByte(false));
        }

        public void ResetGame()
        {
            gameStart.SetIfSender(Convert.ToByte(false));
        }

        public void InitializeRemotePlayersHUD()
        {
            foreach (var player in Players)
            {
                if (player.Value.Behaviour != null)
                {
                    player.Value.Behaviour.InitHUD();
                }
            }
        }

        public void TallyFinalScores(out List<PlayerData> playersSortedByScore,
                                        out bool victory, out bool tied)
        {
            playersSortedByScore = new List<PlayerData>();

            int myScore = 0;
            int highScore = 0;
            int numHighScorePlayers = 0;

            foreach (PlayerData playerData in Players.Values)
            {
                int score = playerData.Behaviour.Score;

                if (score >= highScore)
                {
                    if (score > highScore)
                    {
                        numHighScorePlayers = 1;
                    }
                    else
                    {
                        ++numHighScorePlayers;
                    }

                    highScore = score;
                }

                if (playerData.IsSelf)
                {
                    myScore = score;
                }

                // add player to list, sorted by score
                bool added = false;
                for (int i = 0; i < playersSortedByScore.Count && !added; i++)
                {
                    if (score > playersSortedByScore[i].Behaviour.Score)
                    {
                        playersSortedByScore.Insert(i, playerData);
                        added = true;
                    }
                }
                if (!added)
                {
                    playersSortedByScore.Add(playerData);
                }
            }

            victory = myScore == highScore && numHighScorePlayers == 1;
            tied = myScore == highScore && numHighScorePlayers > 1;
        }
    }
}
