using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EndlessRunner
{
    public class RunnerTrackManager : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform segmentParent;
        [SerializeField] private GameObject[] environmentSegmentPrefabs;
        [FormerlySerializedAs("segmentCount")]
        [SerializeField, Min(2)] private int initialSegmentCount = 16;
        [SerializeField, Min(1f)] private float spawnAheadDistance = 160f;
        [SerializeField, Min(0f)] private float recycleBehindDistance = 24f;
        [SerializeField, Min(0.1f)] private float segmentLength = 12f;
        [SerializeField, Min(0.1f)] private float segmentWidth = 7f;
        [SerializeField, Min(0.2f)] private float platformWidth = 72f;
        [SerializeField] private Material trackMaterial;
        [SerializeField] private Material platformMaterial;

        private readonly Queue<EnvironmentSegment> activeSegments = new();
        private int nextPrefabIndex;

        private void Start()
        {
            BuildInitialSegments();
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsRunActive)
            {
                return;
            }

            float movement = gameManager.CurrentSpeed * Time.deltaTime;
            foreach (EnvironmentSegment segment in activeSegments)
            {
                segment.Root.position += Vector3.back * movement;
            }

            RecyclePassedSegments();
        }

        public void Configure(
            RunnerGameManager manager,
            Transform parent,
            Material material,
            Material groundMaterial = null,
            float groundWidth = 72f,
            Transform player = null,
            GameObject[] segmentPrefabs = null)
        {
            gameManager = manager;
            segmentParent = parent;
            trackMaterial = material;
            platformMaterial = groundMaterial;
            platformWidth = groundWidth;

            if (player != null)
            {
                playerTransform = player;
            }

            if (!HasValidSegmentPrefab() && segmentPrefabs != null && segmentPrefabs.Length > 0)
            {
                environmentSegmentPrefabs = segmentPrefabs;
            }
        }

        private void BuildInitialSegments()
        {
            if (activeSegments.Count > 0)
            {
                return;
            }

            if (segmentParent == null)
            {
                segmentParent = transform;
            }

            float playerZ = GetPlayerZ();
            float nextStartZ = playerZ - recycleBehindDistance;
            float requiredEndZ = playerZ + spawnAheadDistance;
            int safetyCount = 0;

            while ((activeSegments.Count < initialSegmentCount || nextStartZ < requiredEndZ) && safetyCount < 256)
            {
                EnvironmentSegment segment = CreateEnvironmentSegment(activeSegments.Count);
                MoveSegmentToStart(segment, nextStartZ);
                activeSegments.Enqueue(segment);
                nextStartZ += segment.Length;
                safetyCount++;
            }

            if (activeSegments.Count == 0)
            {
                Debug.LogError("RunnerTrackManager could not create an environment segment.", this);
            }
        }

        private EnvironmentSegment CreateEnvironmentSegment(int index)
        {
            GameObject segmentObject = InstantiateNextPrefab();
            if (segmentObject == null)
            {
                segmentObject = CreateGeneratedSegment();
            }

            segmentObject.name = $"Environment Segment {index + 1:00}";
            Transform segmentTransform = segmentObject.transform;
            segmentTransform.SetParent(segmentParent, false);
            segmentTransform.localPosition = Vector3.zero;
            segmentTransform.localRotation = Quaternion.identity;
            segmentTransform.localScale = Vector3.one;

            DetectSegmentGeometry(segmentTransform, out float localStartZ, out float detectedLength);
            return new EnvironmentSegment(segmentTransform, localStartZ, detectedLength);
        }

        private GameObject InstantiateNextPrefab()
        {
            if (!HasValidSegmentPrefab())
            {
                return null;
            }

            for (int i = 0; i < environmentSegmentPrefabs.Length; i++)
            {
                GameObject prefab = environmentSegmentPrefabs[nextPrefabIndex];
                nextPrefabIndex = (nextPrefabIndex + 1) % environmentSegmentPrefabs.Length;

                if (prefab != null)
                {
                    return Instantiate(prefab);
                }
            }

            return null;
        }

        private bool HasValidSegmentPrefab()
        {
            if (environmentSegmentPrefabs == null)
            {
                return false;
            }

            for (int i = 0; i < environmentSegmentPrefabs.Length; i++)
            {
                if (environmentSegmentPrefabs[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject CreateGeneratedSegment()
        {
            GameObject root = new("Generated Environment Segment");
            CreateSegmentPart(
                root.transform,
                "Track",
                new Vector3(0f, 0f, segmentLength * 0.5f),
                new Vector3(segmentWidth, 0.24f, segmentLength),
                trackMaterial);

            float sideWidth = Mathf.Max(0.1f, (platformWidth - segmentWidth) * 0.5f);
            float sideOffset = segmentWidth * 0.5f + sideWidth * 0.5f;
            CreateSegmentPart(
                root.transform,
                "Left Platform",
                new Vector3(-sideOffset, -0.18f, segmentLength * 0.5f),
                new Vector3(sideWidth, 0.18f, segmentLength),
                platformMaterial);
            CreateSegmentPart(
                root.transform,
                "Right Platform",
                new Vector3(sideOffset, -0.18f, segmentLength * 0.5f),
                new Vector3(sideWidth, 0.18f, segmentLength),
                platformMaterial);

            CreateMarker(root.transform, "Segment Start", 0f);
            CreateMarker(root.transform, "Segment End", segmentLength);
            return root;
        }

        private static void CreateSegmentPart(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            if (material != null && part.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateMarker(Transform parent, string markerName, float localZ)
        {
            GameObject marker = new(markerName);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, 0f, localZ);
        }

        private void DetectSegmentGeometry(Transform root, out float localStartZ, out float detectedLength)
        {
            Transform startMarker = FindDescendant(root, "Segment Start");
            Transform endMarker = FindDescendant(root, "Segment End");

            if (startMarker != null && endMarker != null)
            {
                float startZ = root.InverseTransformPoint(startMarker.position).z;
                float endZ = root.InverseTransformPoint(endMarker.position).z;
                if (endZ > startZ + 0.01f)
                {
                    localStartZ = startZ;
                    detectedLength = endZ - startZ;
                    return;
                }
            }

            if (TryGetRendererBounds(root, true, out float minZ, out float maxZ)
                || TryGetRendererBounds(root, false, out minZ, out maxZ)
                || TryGetColliderBounds(root, out minZ, out maxZ))
            {
                localStartZ = minZ;
                detectedLength = Mathf.Max(0.1f, maxZ - minZ);
                return;
            }

            localStartZ = 0f;
            detectedLength = Mathf.Max(0.1f, segmentLength);
        }

        private static bool TryGetRendererBounds(
            Transform root,
            bool groundPartsOnly,
            out float minZ,
            out float maxZ)
        {
            minZ = float.PositiveInfinity;
            maxZ = float.NegativeInfinity;
            bool foundBounds = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (groundPartsOnly && !IsGroundPart(renderers[i].transform, root))
                {
                    continue;
                }

                EncapsulateBounds(root, renderers[i].bounds, ref minZ, ref maxZ);
                foundBounds = true;
            }

            return foundBounds && maxZ > minZ;
        }

        private static bool TryGetColliderBounds(Transform root, out float minZ, out float maxZ)
        {
            minZ = float.PositiveInfinity;
            maxZ = float.NegativeInfinity;
            bool foundBounds = false;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                EncapsulateBounds(root, colliders[i].bounds, ref minZ, ref maxZ);
                foundBounds = true;
            }

            return foundBounds && maxZ > minZ;
        }

        private static void EncapsulateBounds(Transform root, Bounds bounds, ref float minZ, ref float maxZ)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        float localZ = root.InverseTransformPoint(corner).z;
                        minZ = Mathf.Min(minZ, localZ);
                        maxZ = Mathf.Max(maxZ, localZ);
                    }
                }
            }
        }

        private static bool IsGroundPart(Transform candidate, Transform root)
        {
            Transform current = candidate;
            while (current != null && current != root)
            {
                if (current.name == "Track"
                    || current.name == "Left Platform"
                    || current.name == "Right Platform")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == childName)
                {
                    return descendants[i];
                }
            }

            return null;
        }

        private void RecyclePassedSegments()
        {
            if (activeSegments.Count < 2)
            {
                return;
            }

            float recycleThresholdZ = GetPlayerZ() - recycleBehindDistance;
            while (activeSegments.Count > 1 && activeSegments.Peek().EndZ < recycleThresholdZ)
            {
                EnvironmentSegment oldest = activeSegments.Dequeue();
                float nextStartZ = GetFurthestEndZ();
                MoveSegmentToStart(oldest, nextStartZ);
                activeSegments.Enqueue(oldest);
            }
        }

        private float GetFurthestEndZ()
        {
            float furthestEndZ = float.NegativeInfinity;
            foreach (EnvironmentSegment segment in activeSegments)
            {
                furthestEndZ = Mathf.Max(furthestEndZ, segment.EndZ);
            }

            return furthestEndZ;
        }

        private static void MoveSegmentToStart(EnvironmentSegment segment, float startZ)
        {
            segment.Root.SetPositionAndRotation(
                new Vector3(0f, 0f, startZ - segment.LocalStartZ),
                Quaternion.identity);
        }

        private float GetPlayerZ()
        {
            return playerTransform != null ? playerTransform.position.z : 0f;
        }

        private sealed class EnvironmentSegment
        {
            public EnvironmentSegment(Transform root, float localStartZ, float length)
            {
                Root = root;
                LocalStartZ = localStartZ;
                Length = length;
            }

            public Transform Root { get; }
            public float LocalStartZ { get; }
            public float Length { get; }
            public float EndZ => Root.position.z + LocalStartZ + Length;
        }
    }
}
