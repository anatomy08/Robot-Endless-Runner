using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class RunnerCollectiblePool : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Material starMaterial;
        [SerializeField] private AudioClip collectAudioClip;
        [SerializeField, Range(0f, 1f)] private float collectAudioVolume = 0.8f;
        [SerializeField] private int poolSize = 18;
        [SerializeField] private float spawnZ = 48f;
        [SerializeField] private float despawnZ = -8f;
        [SerializeField] private float laneDistance = 2f;
        [SerializeField] private float starHeight = 1.65f;
        [SerializeField] private float obstacleLaneTolerance = 0.95f;
        [SerializeField] private float obstacleClearanceZ = 10f;
        [SerializeField] private float obstacleBoundsPadding = 0.85f;
        [SerializeField] private Vector2 spawnIntervalRange = new(0.65f, 1.1f);
        [SerializeField] private float rotationSpeed = 180f;

        private readonly List<GameObject> collectibles = new();
        private Mesh starMesh;
        private AudioSource collectAudioSource;
        private float spawnTimer;

        private void Awake()
        {
            EnsureCollectAudioSource();
        }

        private void Start()
        {
            starMesh = CreateStarMesh();
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
            for (int i = 0; i < collectibles.Count; i++)
            {
                GameObject collectible = collectibles[i];
                if (!collectible.activeSelf)
                {
                    continue;
                }

                collectible.transform.position += Vector3.back * speed;
                collectible.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

                if (collectible.transform.position.z < despawnZ)
                {
                    collectible.SetActive(false);
                }
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnCollectible();
                ResetSpawnTimer();
            }
        }

        public void Configure(
            RunnerGameManager manager,
            Material material,
            AudioClip audioClip = null,
            float audioVolume = 0.8f,
            float spawnHeight = 1.65f,
            float obstacleZClearance = 10f,
            float obstacleLaneClearance = 0.95f)
        {
            gameManager = manager;
            starMaterial = material;
            collectAudioClip = audioClip;
            collectAudioVolume = Mathf.Clamp01(audioVolume);
            starHeight = spawnHeight;
            obstacleClearanceZ = Mathf.Max(0f, obstacleZClearance);
            obstacleLaneTolerance = Mathf.Max(0f, obstacleLaneClearance);

            EnsureCollectAudioSource();
            UpdateCollectibleAudio();
        }

        private void BuildPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject collectible = new($"Star Collectible {i + 1:00}");
                collectible.transform.SetParent(transform);
                collectible.transform.localScale = Vector3.one * 0.75f;

                MeshFilter meshFilter = collectible.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = starMesh;

                MeshRenderer meshRenderer = collectible.AddComponent<MeshRenderer>();
                if (starMaterial != null)
                {
                    meshRenderer.sharedMaterial = starMaterial;
                }

                SphereCollider collider = collectible.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.55f;

                RunnerStarCollectible star = collectible.AddComponent<RunnerStarCollectible>();
                star.Configure(collectAudioClip, collectAudioVolume, collectAudioSource);
                collectible.SetActive(false);
                collectibles.Add(collectible);
            }
        }

        private void EnsureCollectAudioSource()
        {
            if (collectAudioSource == null && !TryGetComponent(out collectAudioSource))
            {
                collectAudioSource = gameObject.AddComponent<AudioSource>();
            }

            collectAudioSource.playOnAwake = false;
            collectAudioSource.loop = false;
            collectAudioSource.spatialBlend = 0f;
        }

        private void UpdateCollectibleAudio()
        {
            for (int i = 0; i < collectibles.Count; i++)
            {
                if (collectibles[i].TryGetComponent(out RunnerStarCollectible star))
                {
                    star.Configure(collectAudioClip, collectAudioVolume, collectAudioSource);
                }
            }
        }

        private void SpawnCollectible()
        {
            GameObject collectible = GetInactiveCollectible();
            if (collectible == null)
            {
                return;
            }

            if (!TryGetOpenLane(out int lane))
            {
                return;
            }

            collectible.transform.position = new Vector3(lane * laneDistance, starHeight, spawnZ);
            collectible.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            collectible.SetActive(true);
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
            Vector3 candidatePosition = new(laneX, starHeight, spawnZ);
            RunnerObstacle[] obstacles = FindObjectsByType<RunnerObstacle>(FindObjectsInactive.Exclude);

            for (int i = 0; i < obstacles.Length; i++)
            {
                Transform obstacleTransform = obstacles[i].transform;
                if (Mathf.Abs(obstacleTransform.position.x - laneX) > obstacleLaneTolerance)
                {
                    continue;
                }

                if (Mathf.Abs(obstacleTransform.position.z - spawnZ) < obstacleClearanceZ)
                {
                    return false;
                }

                if (obstacles[i].TryGetComponent(out Collider collider))
                {
                    Bounds paddedBounds = collider.bounds;
                    paddedBounds.Expand(obstacleBoundsPadding);

                    if (paddedBounds.Contains(candidatePosition))
                    {
                        return false;
                    }
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

        private GameObject GetInactiveCollectible()
        {
            for (int i = 0; i < collectibles.Count; i++)
            {
                if (!collectibles[i].activeSelf)
                {
                    return collectibles[i];
                }
            }

            return null;
        }

        private void ResetSpawnTimer()
        {
            spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }

        private static Mesh CreateStarMesh()
        {
            const int pointCount = 5;
            Vector3[] vertices = new Vector3[pointCount * 2 + 1];
            int[] triangles = new int[pointCount * 12];

            vertices[0] = Vector3.zero;
            for (int i = 0; i < pointCount * 2; i++)
            {
                float radius = i % 2 == 0 ? 0.55f : 0.24f;
                float angle = Mathf.Deg2Rad * (90f + i * 36f);
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            for (int i = 0; i < pointCount * 2; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i == pointCount * 2 - 1 ? 1 : i + 2;

                int reverseTriangleIndex = triangles.Length / 2 + triangleIndex;
                triangles[reverseTriangleIndex] = 0;
                triangles[reverseTriangleIndex + 1] = i == pointCount * 2 - 1 ? 1 : i + 2;
                triangles[reverseTriangleIndex + 2] = i + 1;
            }

            Mesh mesh = new()
            {
                name = "Runner Star Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
