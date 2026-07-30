using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRunner
{
    public class RunnerMenuController : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private GameObject startMenu;
        [SerializeField] private Button startButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private Button resumeButton;

        private bool isBound;

        private void OnEnable()
        {
            Bind();
            RefreshUi();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (gameManager == null || keyboard == null || gameManager.IsGameOver)
            {
                return;
            }

            bool enterPressed = keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame;
            bool pausePressed = keyboard.pKey.wasPressedThisFrame
                || keyboard.escapeKey.wasPressedThisFrame;

            if (!gameManager.HasStarted && enterPressed)
            {
                StartGame();
            }
            else if (gameManager.IsPaused && (enterPressed || pausePressed))
            {
                ResumeGame();
            }
            else if (gameManager.IsRunActive && pausePressed)
            {
                TogglePause();
            }
        }

        public void Configure(
            RunnerGameManager manager,
            GameObject startPanel,
            Button start,
            Button pause,
            GameObject pausePanel,
            Button resume)
        {
            Unbind();
            gameManager = manager;
            startMenu = startPanel;
            startButton = start;
            pauseButton = pause;
            pauseMenu = pausePanel;
            resumeButton = resume;
            Bind();
            RefreshUi();
        }

        private void Bind()
        {
            if (isBound || gameManager == null)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
            }

            gameManager.RunStarted += RefreshUi;
            gameManager.PauseChanged += HandlePauseChanged;
            gameManager.GameOver += RefreshUi;
            isBound = true;
        }

        private void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(TogglePause);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeGame);
            }

            if (gameManager != null)
            {
                gameManager.RunStarted -= RefreshUi;
                gameManager.PauseChanged -= HandlePauseChanged;
                gameManager.GameOver -= RefreshUi;
            }

            isBound = false;
        }

        private void StartGame()
        {
            gameManager?.StartRun();
        }

        private void TogglePause()
        {
            gameManager?.TogglePause();
        }

        private void ResumeGame()
        {
            gameManager?.Resume();
        }

        private void HandlePauseChanged(bool _)
        {
            RefreshUi();
        }

        private void RefreshUi()
        {
            bool hasManager = gameManager != null;
            bool showStartMenu = hasManager && !gameManager.HasStarted && !gameManager.IsGameOver;
            bool showPauseMenu = hasManager && gameManager.IsPaused && !gameManager.IsGameOver;
            bool showPauseButton = hasManager && gameManager.HasStarted && !gameManager.IsPaused && !gameManager.IsGameOver;

            if (startMenu != null)
            {
                startMenu.SetActive(showStartMenu);
            }

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(showPauseMenu);
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(showPauseButton);
            }
        }
    }
}
