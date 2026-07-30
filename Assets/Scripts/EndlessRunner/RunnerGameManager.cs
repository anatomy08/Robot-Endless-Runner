using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EndlessRunner
{
    public class RunnerGameManager : MonoBehaviour
    {
        [SerializeField] private float initialSpeed = 8f;
        [SerializeField] private float speedIncreasePerSecond = 0.08f;
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float scoreMultiplier = 1f;

        public event Action GameOver;
        public event Action Restarted;
        public event Action<int> StarsChanged;
        public event Action RunStarted;
        public event Action<bool> PauseChanged;

        public float CurrentSpeed { get; private set; }
        public float Score { get; private set; }
        public int Stars { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsRunActive => HasStarted && !IsPaused && !IsGameOver;

        private void Awake()
        {
            CurrentSpeed = initialSpeed;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (IsGameOver)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    Restart();
                }

                return;
            }

            if (!IsRunActive)
            {
                return;
            }

            CurrentSpeed = Mathf.Min(maxSpeed, CurrentSpeed + speedIncreasePerSecond * Time.deltaTime);
            Score += CurrentSpeed * scoreMultiplier * Time.deltaTime;
        }

        public void StartRun()
        {
            if (HasStarted || IsGameOver)
            {
                return;
            }

            HasStarted = true;
            IsPaused = false;
            CurrentSpeed = initialSpeed;
            Time.timeScale = 1f;
            RunStarted?.Invoke();
        }

        public void TogglePause()
        {
            if (!HasStarted || IsGameOver)
            {
                return;
            }

            SetPaused(!IsPaused);
        }

        public void Resume()
        {
            SetPaused(false);
        }

        public void EndRun()
        {
            if (!HasStarted || IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            IsPaused = false;
            CurrentSpeed = 0f;
            Time.timeScale = 1f;
            PauseChanged?.Invoke(false);
            GameOver?.Invoke();
        }

        public void AddStar(int amount = 1)
        {
            if (!IsRunActive || amount <= 0)
            {
                return;
            }

            Stars += amount;
            StarsChanged?.Invoke(Stars);
        }

        public void Restart()
        {
            Restarted?.Invoke();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetPaused(bool paused)
        {
            if (!HasStarted || IsGameOver || IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            PauseChanged?.Invoke(IsPaused);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
