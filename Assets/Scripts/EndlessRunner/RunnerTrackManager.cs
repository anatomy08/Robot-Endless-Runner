using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class RunnerTrackManager : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Transform segmentParent;
        [SerializeField] private int segmentCount = 8;
        [SerializeField] private float segmentLength = 12f;
        [SerializeField] private float segmentWidth = 7f;
        [SerializeField] private Material trackMaterial;

        private readonly List<Transform> segments = new();
        private float recycleZ;

        private void Start()
        {
            BuildSegments();
        }

        private void Update()
        {
            if (gameManager == null || gameManager.IsGameOver)
            {
                return;
            }

            float speed = gameManager.CurrentSpeed * Time.deltaTime;
            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i];
                segment.position += Vector3.back * speed;

                if (segment.position.z < recycleZ)
                {
                    segment.position += Vector3.forward * segmentLength * segmentCount;
                }
            }
        }

        public void Configure(RunnerGameManager manager, Transform parent, Material material)
        {
            gameManager = manager;
            segmentParent = parent;
            trackMaterial = material;
        }

        private void BuildSegments()
        {
            if (segmentParent == null)
            {
                segmentParent = transform;
            }

            recycleZ = -segmentLength * 1.5f;

            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"Track Segment {i + 1:00}";
                segment.transform.SetParent(segmentParent);
                segment.transform.position = new Vector3(0f, -0.1f, i * segmentLength);
                segment.transform.localScale = new Vector3(segmentWidth, 0.2f, segmentLength);

                if (trackMaterial != null && segment.TryGetComponent(out Renderer renderer))
                {
                    renderer.sharedMaterial = trackMaterial;
                }

                segments.Add(segment.transform);
            }
        }
    }
}
