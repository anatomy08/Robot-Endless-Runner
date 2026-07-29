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

        public float CurrentSpeed { get; private set; }
        public float Score { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            CurrentSpeed = initialSpeed;
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

            CurrentSpeed = Mathf.Min(maxSpeed, CurrentSpeed + speedIncreasePerSecond * Time.deltaTime);
            Score += CurrentSpeed * scoreMultiplier * Time.deltaTime;
        }

        public void EndRun()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            CurrentSpeed = 0f;
            GameOver?.Invoke();
        }

        public void Restart()
        {
            Restarted?.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
