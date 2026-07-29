using UnityEngine;
using UnityEngine.UI;

namespace EndlessRunner
{
    public class RunnerHud : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text messageText;

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.GameOver += ShowGameOver;
                gameManager.Restarted += HideGameOver;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.GameOver -= ShowGameOver;
                gameManager.Restarted -= HideGameOver;
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

        public void Configure(RunnerGameManager manager, Text score, Text message)
        {
            if (gameManager != null)
            {
                gameManager.GameOver -= ShowGameOver;
                gameManager.Restarted -= HideGameOver;
            }

            gameManager = manager;
            scoreText = score;
            messageText = message;

            if (gameManager != null && isActiveAndEnabled)
            {
                gameManager.GameOver += ShowGameOver;
                gameManager.Restarted += HideGameOver;
            }

            HideGameOver();
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
