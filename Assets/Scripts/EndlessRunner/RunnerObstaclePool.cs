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
        [SerializeField] private Vector2 rockScaleRange = new(0.9f, 1.35f);
        [SerializeField] private float collectibleLaneTolerance = 0.95f;
        [SerializeField] private float collectibleClearanceZ = 10f;

        private readonly List<GameObject> obstacles = new();
        private Mesh rockMesh;
        private float spawnTimer;

        private void Start()
        {
            rockMesh = CreateRockMesh();
            BuildPool();
            ResetSpawnTimer();
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsRunActive)
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
                GameObject obstacle = new($"Rock Obstacle {i + 1:00}");
                obstacle.name = $"Obstacle {i + 1:00}";
                obstacle.transform.SetParent(transform);

                MeshFilter meshFilter = obstacle.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = rockMesh;

                MeshRenderer renderer = obstacle.AddComponent<MeshRenderer>();
                if (obstacleMaterial != null)
                {
                    renderer.sharedMaterial = obstacleMaterial;
                }

                obstacle.AddComponent<RunnerObstacle>();

                BoxCollider collider = obstacle.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = new Vector3(0f, 0.7f, 0f);
                collider.size = new Vector3(1.25f, 1.4f, 1.25f);

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

            if (!TryGetOpenLane(out int lane))
            {
                return;
            }

            float scale = Random.Range(rockScaleRange.x, rockScaleRange.y);
            obstacle.transform.localScale = new Vector3(scale, scale, scale);
            obstacle.transform.position = new Vector3(lane * laneDistance, 0f, spawnZ);
            obstacle.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            obstacle.SetActive(true);
        }

        private bool TryGetOpenLane(out int lane)
        {
            int startLane = Random.Range(-1, 2);
            for (int i = 0; i < 3; i++)
            {
                int candidateLane = WrapLane(startLane + i);
                if (IsLaneClear(candidateLane))
                {
                    lane = candidateLane;
                    return true;
                }
            }

            lane = 0;
            return false;
        }

        private bool IsLaneClear(int lane)
        {
            float laneX = lane * laneDistance;
            RunnerStarCollectible[] collectibles = FindObjectsByType<RunnerStarCollectible>(FindObjectsInactive.Exclude);

            for (int i = 0; i < collectibles.Length; i++)
            {
                Transform collectibleTransform = collectibles[i].transform;
                if (Mathf.Abs(collectibleTransform.position.x - laneX) > collectibleLaneTolerance)
                {
                    continue;
                }

                if (Mathf.Abs(collectibleTransform.position.z - spawnZ) < collectibleClearanceZ)
                {
                    return false;
                }
            }

            return true;
        }

        private static int WrapLane(int lane)
        {
            if (lane > 1)
            {
                return lane - 3;
            }

            if (lane < -1)
            {
                return lane + 3;
            }

            return lane;
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

        private static Mesh CreateRockMesh()
        {
            Vector3[] vertices =
            {
                new(-0.55f, 0f, -0.45f),
                new(0.5f, 0f, -0.5f),
                new(0.65f, 0f, 0.35f),
                new(-0.45f, 0f, 0.55f),
                new(-0.75f, 0.55f, -0.25f),
                new(0.45f, 0.7f, -0.65f),
                new(0.78f, 0.62f, 0.28f),
                new(-0.38f, 0.78f, 0.7f),
                new(-0.32f, 1.45f, -0.18f),
                new(0.36f, 1.3f, -0.28f),
                new(0.48f, 1.2f, 0.34f),
                new(-0.28f, 1.35f, 0.42f)
            };

            int[] triangles =
            {
                0, 1, 2, 0, 2, 3,
                0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3,
                3, 7, 4, 3, 4, 0,
                4, 8, 9, 4, 9, 5,
                5, 9, 10, 5, 10, 6,
                6, 10, 11, 6, 11, 7,
                7, 11, 8, 7, 8, 4,
                8, 11, 10, 8, 10, 9
            };

            Mesh mesh = new()
            {
                name = "Runner Rock Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
