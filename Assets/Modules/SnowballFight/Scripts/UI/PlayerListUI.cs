using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// GUI displaying a dynamic list of players in the multiplayer SnowballFight demo.
    /// </summary>
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class PlayerListUI : MonoBehaviour
    {
        [SerializeField] PlayerStatusUI[] playerStatusUIs;

        public void Refresh(IEnumerable<PlayerData> players, bool gameOver = false)
        {
            // First, reset the list
            foreach (PlayerStatusUI playerStatusUI in playerStatusUIs)
            {
                playerStatusUI.Reset();
            }

            int playerIndex = 0;
            int highScore = 0;

            foreach (var playerEntry in players)
            {
                if (playerEntry.Behaviour != null && playerEntry.Name != String.Empty)
                {
                    PlayerStatusUI playerStatusUI = playerStatusUIs[playerIndex];
                    PlayerData playerData = playerEntry;

                    // NOTE: At gameover, the player list is sorted.
                    if (playerIndex == 0 && gameOver) highScore = playerData.Behaviour.Score;

                    playerStatusUI.UpdateUI(playerData.IsSelf,
                                          playerData.IsHost,
                                          playerData.Name,
                                          playerData.IsReady ? PlayerStatusUI.Status.Ready : PlayerStatusUI.Status.Waiting,
                                          gameOver,
                                          winner: gameOver && playerData.Behaviour.Score == highScore,
                                          score: playerData.Behaviour.Score
                    );

                    playerIndex++;
                    if (playerIndex >= playerStatusUIs.Length) break;
                }
            }
        }
    }
}
