using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class RunnerObstaclePool : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Material obstacleMaterial;
        [SerializeField] private int poolSize = 12;
        [SerializeField] private float spawnZ = 42f;
        [SerializeField] private float despawnZ = -8f;
        [SerializeField] private float laneDistance = 2f;
        [SerializeField] private Vector2 spawnIntervalRange = new(0.85f, 1.35f);

        private readonly List<GameObject> obstacles = new();
        private float spawnTimer;

        private void Start()
        {
            BuildPool();
            ResetSpawnTimer();
        }

        private void Update()
        {
            if (gameManager == null || gameManager.IsGameOver)
            {
                return;
            }

            float speed = gameManager.CurrentSpeed * Time.deltaTime;
            for (int i = 0; i < obstacles.Count; i++)
            {
                GameObject obstacle = obstacles[i];
                if (!obstacle.activeSelf)
                {
                    continue;
                }

                obstacle.transform.position += Vector3.back * speed;
                if (obstacle.transform.position.z < despawnZ)
                {
                    obstacle.SetActive(false);
                }
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnObstacle();
                ResetSpawnTimer();
            }
        }

        public void Configure(RunnerGameManager manager, Material material)
        {
            gameManager = manager;
            obstacleMaterial = material;
        }

        private void BuildPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle {i + 1:00}";
                obstacle.transform.SetParent(transform);
                obstacle.transform.localScale = new Vector3(1.15f, 1.5f, 1.15f);
                obstacle.AddComponent<RunnerObstacle>();

                if (obstacle.TryGetComponent(out BoxCollider collider))
                {
                    collider.isTrigger = true;
                }

                if (obstacle.TryGetComponent(out Renderer renderer) && obstacleMaterial != null)
                {
                    renderer.sharedMaterial = obstacleMaterial;
                }

                obstacle.SetActive(false);
                obstacles.Add(obstacle);
            }
        }

        private void SpawnObstacle()
        {
            GameObject obstacle = GetInactiveObstacle();
            if (obstacle == null)
            {
                return;
            }

            int lane = Random.Range(-1, 2);
            obstacle.transform.position = new Vector3(lane * laneDistance, 0.25f, spawnZ);
            obstacle.SetActive(true);
        }

        private GameObject GetInactiveObstacle()
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (!obstacles[i].activeSelf)
                {
                    return obstacles[i];
                }
            }

            return null;
        }

        private void ResetSpawnTimer()
        {
            spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }
    }
}
