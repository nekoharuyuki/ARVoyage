using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Information row for a player for the PlayerListUI class.
    /// Used before a game begins, to show which players are active/connected,
    /// as well as after a game is over, to show final scores and the winner.
    /// </summary>
    public class PlayerStatusUI : MonoBehaviour
    {
        [SerializeField] private Image playerIcon;
        [SerializeField] private TMPro.TMP_Text playerName;
        [SerializeField] private Image highlightBackground;

        [SerializeField] private Image statusIcon;
        [SerializeField] private Sprite statusIconWaiting;
        [SerializeField] private Sprite statusIconReady;
        [SerializeField] private Sprite statusIconError;

        [SerializeField] private Image winnerIcon;
        [SerializeField] private TMPro.TMP_Text scoreText;

        public void UpdateUI(bool isSelf, bool isHost, string name, Status status,
                                bool gameOver, bool winner = false, int score = -1)
        {
            highlightBackground.enabled = isSelf;
            playerIcon.GetComponent<CanvasGroup>().alpha = 1f;

            string suffix = string.Empty;
            if (isHost && !gameOver) suffix += " (Host)";
            if (isSelf) suffix += " (You!)";
            playerName.text = name + suffix;

            statusIcon.enabled = !gameOver;
            switch (status)
            {
                case Status.Waiting:
                    statusIcon.sprite = statusIconWaiting;
                    break;
                case Status.Ready:
                    statusIcon.sprite = statusIconReady;
                    break;
                case Status.Error:
                    statusIcon.sprite = statusIconError;
                    break;
            }

            // if game over, show score, and possibly show winner icon
            scoreText.enabled = gameOver;
            if (gameOver) scoreText.text = score.ToString();
            winnerIcon.enabled = gameOver && winner;
        }

        public void Reset()
        {
            highlightBackground.enabled = false;
            playerIcon.GetComponent<CanvasGroup>().alpha = 0.7f;
            playerName.text = string.Empty;
            statusIcon.enabled = false;
            statusIcon.sprite = statusIconWaiting;
        }

        public enum Status
        {
            Waiting,
            Ready,
            Error
        }
    }
}
