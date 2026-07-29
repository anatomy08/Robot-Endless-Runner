using UnityEngine;
using UnityEngine.UI;

namespace EndlessRunner
{
    public class RunnerHud : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text starText;
        [SerializeField] private Text messageText;

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.GameOver += ShowGameOver;
                gameManager.Restarted += HideGameOver;
                gameManager.StarsChanged += UpdateStarText;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.GameOver -= ShowGameOver;
                gameManager.Restarted -= HideGameOver;
                gameManager.StarsChanged -= UpdateStarText;
            }
        }

        private void Update()
        {
            if (gameManager == null || scoreText == null)
            {
                return;
            }

            scoreText.text = $"Score {Mathf.FloorToInt(gameManager.Score):00000}";
        }

        public void Configure(RunnerGameManager manager, Text score, Text stars, Text message)
        {
            if (gameManager != null)
            {
                gameManager.GameOver -= ShowGameOver;
                gameManager.Restarted -= HideGameOver;
                gameManager.StarsChanged -= UpdateStarText;
            }

            gameManager = manager;
            scoreText = score;
            starText = stars;
            messageText = message;

            if (gameManager != null && isActiveAndEnabled)
            {
                gameManager.GameOver += ShowGameOver;
                gameManager.Restarted += HideGameOver;
                gameManager.StarsChanged += UpdateStarText;
            }

            UpdateStarText(gameManager != null ? gameManager.Stars : 0);
            HideGameOver();
        }

        private void UpdateStarText(int stars)
        {
            if (starText != null)
            {
                starText.text = $"Stars {stars:000}";
            }
        }

        private void ShowGameOver()
        {
            if (messageText != null)
            {
                messageText.gameObject.SetActive(true);
                messageText.text = "Game Over\nPress R to Restart";
            }
        }

        private void HideGameOver()
        {
            if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
        }
    }
}
