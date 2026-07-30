using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public enum RunnerEnvironmentPropCategory
    {
        Building,
        Tree,
        ParkedCar,
        Streetlight,
        TrafficLight,
        Billboard,
        Bench,
        TrashBin,
        RoadSign,
        Miscellaneous
    }

    [Serializable]
    public sealed class RunnerEnvironmentPropWeight
    {
        [SerializeField] private RunnerEnvironmentPropCategory category;
        [SerializeField, Min(0f)] private float weight;

        public RunnerEnvironmentPropWeight(RunnerEnvironmentPropCategory category, float weight)
        {
            this.category = category;
            this.weight = weight;
        }

        public RunnerEnvironmentPropCategory Category => category;
        public float Weight => Mathf.Max(0f, weight);
    }

    [Serializable]
    public sealed class RunnerEnvironmentPropPalette
    {
        [SerializeField] private Material[] buildingMaterials;
        [SerializeField] private Material windowMaterial;
        [SerializeField] private Material foliageMaterial;
        [SerializeField] private Material trunkMaterial;
        [SerializeField] private Material vehicleMaterial;
        [SerializeField] private Material metalMaterial;
        [SerializeField] private Material furnitureMaterial;
        [SerializeField] private Material redMaterial;
        [SerializeField] private Material yellowMaterial;
        [SerializeField] private Material greenMaterial;

        public Material WindowMaterial => windowMaterial;
        public Material FoliageMaterial => foliageMaterial;
        public Material TrunkMaterial => trunkMaterial;
        public Material VehicleMaterial => vehicleMaterial;
        public Material MetalMaterial => metalMaterial;
        public Material FurnitureMaterial => furnitureMaterial;
        public Material RedMaterial => redMaterial;
        public Material YellowMaterial => yellowMaterial;
        public Material GreenMaterial => greenMaterial;

        public void Configure(
            Material[] buildings,
            Material windows,
            Material foliage,
            Material trunk,
            Material vehicle,
            Material metal,
            Material furniture,
            Material red,
            Material yellow,
            Material green)
        {
            buildingMaterials = buildings;
            windowMaterial = windows;
            foliageMaterial = foliage;
            trunkMaterial = trunk;
            vehicleMaterial = vehicle;
            metalMaterial = metal;
            furnitureMaterial = furniture;
            redMaterial = red;
            yellowMaterial = yellow;
            greenMaterial = green;
        }

        public Material GetRandomBuildingMaterial()
        {
            if (buildingMaterials == null || buildingMaterials.Length == 0)
            {
                return metalMaterial;
            }

            return buildingMaterials[UnityEngine.Random.Range(0, buildingMaterials.Length)];
        }
    }

    public sealed class RunnerEnvironmentDecorator : MonoBehaviour
    {
        [Header("Prefab Categories")]
        [SerializeField] private GameObject[] buildingPrefabs;
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] parkedCarPrefabs;
        [SerializeField] private GameObject[] streetlightPrefabs;
        [SerializeField] private GameObject[] trafficLightPrefabs;
        [SerializeField] private GameObject[] billboardPrefabs;
        [SerializeField] private GameObject[] benchPrefabs;
        [SerializeField] private GameObject[] trashBinPrefabs;
        [SerializeField] private GameObject[] roadSignPrefabs;
        [Tooltip("Fire hydrants, bus stops, fences, barriers, and planters can be assigned here.")]
        [SerializeField] private GameObject[] miscellaneousPropPrefabs;

        [Header("Weighted Spawn Chances")]
        [SerializeField] private List<RunnerEnvironmentPropWeight> spawnWeights = new()
        {
            new(RunnerEnvironmentPropCategory.Building, 45f),
            new(RunnerEnvironmentPropCategory.Tree, 15f),
            new(RunnerEnvironmentPropCategory.ParkedCar, 10f),
            new(RunnerEnvironmentPropCategory.Streetlight, 10f),
            new(RunnerEnvironmentPropCategory.Billboard, 5f),
            new(RunnerEnvironmentPropCategory.TrafficLight, 5f),
            new(RunnerEnvironmentPropCategory.Bench, 3f),
            new(RunnerEnvironmentPropCategory.TrashBin, 3f),
            new(RunnerEnvironmentPropCategory.RoadSign, 2f),
            new(RunnerEnvironmentPropCategory.Miscellaneous, 2f)
        };
        [SerializeField, Min(1)] private int maximumConsecutiveCategory = 3;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float leftSideOffset = 6.5f;
        [SerializeField, Min(0f)] private float rightSideOffset = 6.5f;
        [SerializeField, Min(0.1f)] private float spawnPointDistance = 6f;
        [SerializeField, Min(0.1f)] private float minimumSpacing = 4f;
        [SerializeField, Min(0.1f)] private float maximumSpacing = 8f;
        [SerializeField] private Vector2 positionVariation = new(0.75f, 0.8f);
        [SerializeField, Range(0f, 30f)] private float rotationVariation = 8f;
        [SerializeField] private Vector2 scaleVariation = new(0.9f, 1.1f);
        [SerializeField] private float groundHeightOffset = -0.08f;
        [SerializeField, Min(1)] private int maximumPropsPerSegment = 4;
        [SerializeField, Range(0f, 1f)] private float spawnChancePerPoint = 0.82f;

        [Header("Placement Safety")]
        [SerializeField, Min(0f)] private float trackClearance = 3.8f;
        [SerializeField, Min(0f)] private float overlapPadding = 0.35f;
        [SerializeField, Min(1)] private int placementAttempts = 4;

        [Header("Fallback Visuals")]
        [SerializeField] private bool useProceduralFallbacks = true;
        [SerializeField] private RunnerEnvironmentPropPalette proceduralPalette = new();

        private readonly Dictionary<GameObject, Stack<GameObject>> prefabPools = new();
        private readonly Dictionary<RunnerEnvironmentPropCategory, Stack<GameObject>> generatedPools = new();
        private readonly Dictionary<GameObject, PropMetadata> propMetadata = new();
        private readonly Dictionary<Transform, List<ActiveProp>> activePropsBySegment = new();
        private readonly SideHistory leftHistory = new();
        private readonly SideHistory rightHistory = new();

        private Transform poolRoot;

        public void Configure(RunnerEnvironmentPropPalette palette)
        {
            if (palette != null)
            {
                proceduralPalette = palette;
            }
        }

        public void PopulateSegment(Transform segment, float localStartZ, float segmentLength)
        {
            if (segment == null || segmentLength <= 0f)
            {
                return;
            }

            ReleaseSegment(segment);
            EnsurePoolRoot();
            EnsureSegmentContainers(segment);

            List<ActiveProp> segmentProps = new();
            activePropsBySegment[segment] = segmentProps;

            float edgePadding = Mathf.Min(1f, segmentLength * 0.1f);
            float endZ = localStartZ + segmentLength - edgePadding;
            float leftZ = localStartZ + edgePadding + UnityEngine.Random.Range(0f, Mathf.Min(spawnPointDistance, segmentLength * 0.35f));
            float rightZ = localStartZ + edgePadding + UnityEngine.Random.Range(0f, Mathf.Min(spawnPointDistance, segmentLength * 0.35f));
            int spawnedCount = 0;
            int safetyCount = 0;

            while (spawnedCount < maximumPropsPerSegment
                && (leftZ <= endZ || rightZ <= endZ)
                && safetyCount < maximumPropsPerSegment * 6)
            {
                bool useLeft = leftZ <= endZ && (rightZ > endZ || leftZ <= rightZ);
                int side = useLeft ? -1 : 1;
                float z = useLeft ? leftZ : rightZ;

                if (UnityEngine.Random.value <= spawnChancePerPoint
                    && TrySpawnProp(segment, segmentProps, side, z))
                {
                    spawnedCount++;
                }

                if (useLeft)
                {
                    leftZ += GetNextSpacing();
                }
                else
                {
                    rightZ += GetNextSpacing();
                }

                safetyCount++;
            }
        }

        public void RefreshSegment(Transform segment, float localStartZ, float segmentLength)
        {
            PopulateSegment(segment, localStartZ, segmentLength);
        }

        private bool TrySpawnProp(Transform segment, List<ActiveProp> segmentProps, int side, float localZ)
        {
            SideHistory history = side < 0 ? leftHistory : rightHistory;
            RunnerEnvironmentPropCategory category = SelectCategory(history);
            GameObject prefab = SelectPrefab(category, history.LastPrefab);
            ActiveProp activeProp = AcquireProp(category, prefab);
            if (activeProp == null)
            {
                return false;
            }

            Transform container = GetOrCreateContainer(segment, category);
            activeProp.Instance.transform.SetParent(container, false);
            activeProp.Instance.SetActive(true);

            for (int attempt = 0; attempt < Mathf.Max(1, placementAttempts); attempt++)
            {
                ApplyPlacement(activeProp, segment, side, localZ);
                if (!TryGetWorldBounds(activeProp.Instance, out Bounds candidateBounds)
                    || IsPlacementValid(segment, activeProp.Instance.transform, candidateBounds, side))
                {
                    segmentProps.Add(activeProp);
                    history.Record(category, prefab);
                    return true;
                }
            }

            ReleaseProp(activeProp);
            return false;
        }

        private RunnerEnvironmentPropCategory SelectCategory(SideHistory history)
        {
            if (spawnWeights == null || spawnWeights.Count == 0)
            {
                return GetFallbackCategory(history);
            }

            int consecutiveLimit = Mathf.Max(1, maximumConsecutiveCategory);
            float totalWeight = 0f;
            for (int i = 0; i < spawnWeights.Count; i++)
            {
                if (HasSource(spawnWeights[i].Category)
                    && !(history.ConsecutiveCount >= consecutiveLimit
                        && spawnWeights[i].Category == history.LastCategory))
                {
                    totalWeight += spawnWeights[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return GetFallbackCategory(history);
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            for (int i = 0; i < spawnWeights.Count; i++)
            {
                RunnerEnvironmentPropWeight entry = spawnWeights[i];
                if (!HasSource(entry.Category)
                    || (history.ConsecutiveCount >= consecutiveLimit
                        && entry.Category == history.LastCategory))
                {
                    continue;
                }

                roll -= entry.Weight;
                if (roll <= 0f)
                {
                    return entry.Category;
                }
            }

            return GetFallbackCategory(history);
        }

        private RunnerEnvironmentPropCategory GetFallbackCategory(SideHistory history)
        {
            int consecutiveLimit = Mathf.Max(1, maximumConsecutiveCategory);
            Array categories = Enum.GetValues(typeof(RunnerEnvironmentPropCategory));
            for (int i = 0; i < categories.Length; i++)
            {
                RunnerEnvironmentPropCategory category = (RunnerEnvironmentPropCategory)categories.GetValue(i);
                if (HasSource(category)
                    && !(history.ConsecutiveCount >= consecutiveLimit
                        && category == history.LastCategory))
                {
                    return category;
                }
            }

            return RunnerEnvironmentPropCategory.Building;
        }

        private bool HasSource(RunnerEnvironmentPropCategory category)
        {
            return useProceduralFallbacks || CountValidPrefabs(GetPrefabs(category)) > 0;
        }

        private GameObject SelectPrefab(RunnerEnvironmentPropCategory category, GameObject lastPrefab)
        {
            GameObject[] prefabs = GetPrefabs(category);
            int validCount = CountValidPrefabs(prefabs);
            if (validCount == 0)
            {
                return null;
            }

            GameObject selected = null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                selected = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
                if (selected != null && (validCount == 1 || selected != lastPrefab))
                {
                    return selected;
                }
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null && (validCount == 1 || prefabs[i] != lastPrefab))
                {
                    return prefabs[i];
                }
            }

            return selected;
        }

        private GameObject[] GetPrefabs(RunnerEnvironmentPropCategory category)
        {
            return category switch
            {
                RunnerEnvironmentPropCategory.Building => buildingPrefabs,
                RunnerEnvironmentPropCategory.Tree => treePrefabs,
                RunnerEnvironmentPropCategory.ParkedCar => parkedCarPrefabs,
                RunnerEnvironmentPropCategory.Streetlight => streetlightPrefabs,
                RunnerEnvironmentPropCategory.TrafficLight => trafficLightPrefabs,
                RunnerEnvironmentPropCategory.Billboard => billboardPrefabs,
                RunnerEnvironmentPropCategory.Bench => benchPrefabs,
                RunnerEnvironmentPropCategory.TrashBin => trashBinPrefabs,
                RunnerEnvironmentPropCategory.RoadSign => roadSignPrefabs,
                RunnerEnvironmentPropCategory.Miscellaneous => miscellaneousPropPrefabs,
                _ => null
            };
        }

        private ActiveProp AcquireProp(RunnerEnvironmentPropCategory category, GameObject prefab)
        {
            if (prefab == null && !useProceduralFallbacks)
            {
                return null;
            }

            Stack<GameObject> pool = GetPool(category, prefab);
            GameObject instance;

            if (pool.Count > 0)
            {
                instance = pool.Pop();
            }
            else
            {
                instance = prefab != null
                    ? Instantiate(prefab, poolRoot, false)
                    : RunnerProceduralPropFactory.Create(category, proceduralPalette);

                if (instance == null)
                {
                    return null;
                }

                instance.name = prefab != null ? prefab.name : $"Procedural {category}";
                propMetadata[instance] = new PropMetadata(
                    instance.transform.localRotation,
                    instance.transform.localScale);
            }

            if (!propMetadata.TryGetValue(instance, out PropMetadata metadata))
            {
                metadata = new PropMetadata(instance.transform.localRotation, instance.transform.localScale);
                propMetadata[instance] = metadata;
            }

            return new ActiveProp(instance, category, prefab, metadata);
        }

        private Stack<GameObject> GetPool(RunnerEnvironmentPropCategory category, GameObject prefab)
        {
            if (prefab != null)
            {
                if (!prefabPools.TryGetValue(prefab, out Stack<GameObject> pool))
                {
                    pool = new Stack<GameObject>();
                    prefabPools.Add(prefab, pool);
                }

                return pool;
            }

            if (!generatedPools.TryGetValue(category, out Stack<GameObject> generatedPool))
            {
                generatedPool = new Stack<GameObject>();
                generatedPools.Add(category, generatedPool);
            }

            return generatedPool;
        }

        private void ApplyPlacement(ActiveProp prop, Transform segment, int side, float localZ)
        {
            PropMetadata metadata = prop.Metadata;
            float scale = UnityEngine.Random.Range(
                Mathf.Min(scaleVariation.x, scaleVariation.y),
                Mathf.Max(scaleVariation.x, scaleVariation.y));
            prop.Instance.transform.localScale = metadata.BaseScale * Mathf.Max(0.05f, scale);
            prop.Instance.transform.localRotation = GetCategoryRotation(prop.Category, metadata.BaseRotation, side);

            float sideOffset = side < 0 ? -leftSideOffset : rightSideOffset;
            float xVariation = UnityEngine.Random.Range(-Mathf.Abs(positionVariation.x), Mathf.Abs(positionVariation.x));
            float zVariation = UnityEngine.Random.Range(-Mathf.Abs(positionVariation.y), Mathf.Abs(positionVariation.y));
            prop.Instance.transform.localPosition = new Vector3(sideOffset + xVariation, 0f, localZ + zVariation);

            if (TryGetWorldBounds(prop.Instance, out Bounds bounds))
            {
                float groundY = segment.TransformPoint(new Vector3(0f, groundHeightOffset, 0f)).y;
                prop.Instance.transform.position += Vector3.up * (groundY - bounds.min.y);
            }
        }

        private Quaternion GetCategoryRotation(
            RunnerEnvironmentPropCategory category,
            Quaternion baseRotation,
            int side)
        {
            if (category == RunnerEnvironmentPropCategory.Building)
            {
                return baseRotation;
            }

            float variation = UnityEngine.Random.Range(-rotationVariation, rotationVariation);
            float faceTrackYaw = side < 0 ? 90f : -90f;
            float yaw = category switch
            {
                RunnerEnvironmentPropCategory.ParkedCar => (UnityEngine.Random.value < 0.5f ? 0f : 180f)
                    + Mathf.Clamp(variation, -5f, 5f),
                RunnerEnvironmentPropCategory.Streetlight => faceTrackYaw + variation,
                RunnerEnvironmentPropCategory.TrafficLight => faceTrackYaw + variation,
                RunnerEnvironmentPropCategory.Billboard => faceTrackYaw + variation,
                RunnerEnvironmentPropCategory.Bench => faceTrackYaw + variation,
                RunnerEnvironmentPropCategory.RoadSign => faceTrackYaw + variation,
                _ => UnityEngine.Random.Range(0f, 360f)
            };

            return Quaternion.Euler(0f, yaw, 0f) * baseRotation;
        }

        private bool IsPlacementValid(
            Transform segment,
            Transform candidate,
            Bounds candidateBounds,
            int side)
        {
            float trackCenterX = segment.position.x;
            if (side < 0 && candidateBounds.max.x > trackCenterX - trackClearance)
            {
                return false;
            }

            if (side > 0 && candidateBounds.min.x < trackCenterX + trackClearance)
            {
                return false;
            }

            Bounds paddedCandidate = candidateBounds;
            paddedCandidate.Expand(overlapPadding * 2f);

            Renderer[] segmentRenderers = segment.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < segmentRenderers.Length; i++)
            {
                Renderer renderer = segmentRenderers[i];
                if (renderer.transform.IsChildOf(candidate) || IsGroundPart(renderer.transform, segment))
                {
                    continue;
                }

                if (paddedCandidate.Intersects(renderer.bounds))
                {
                    return false;
                }
            }

            foreach (List<ActiveProp> props in activePropsBySegment.Values)
            {
                for (int i = 0; i < props.Count; i++)
                {
                    if (props[i].Instance == null
                        || !props[i].Instance.activeInHierarchy
                        || props[i].Instance.transform.IsChildOf(segment))
                    {
                        continue;
                    }

                    if (TryGetWorldBounds(props[i].Instance, out Bounds existingBounds)
                        && paddedCandidate.Intersects(existingBounds))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ReleaseSegment(Transform segment)
        {
            if (!activePropsBySegment.TryGetValue(segment, out List<ActiveProp> props))
            {
                return;
            }

            for (int i = 0; i < props.Count; i++)
            {
                ReleaseProp(props[i]);
            }

            props.Clear();
            activePropsBySegment.Remove(segment);
        }

        private void ReleaseProp(ActiveProp prop)
        {
            if (prop == null || prop.Instance == null)
            {
                return;
            }

            prop.Instance.SetActive(false);
            prop.Instance.transform.SetParent(poolRoot, false);
            GetPool(prop.Category, prop.SourcePrefab).Push(prop.Instance);
        }

        private Transform GetOrCreateContainer(Transform segment, RunnerEnvironmentPropCategory category)
        {
            string containerName = category switch
            {
                RunnerEnvironmentPropCategory.Building => "Buildings",
                RunnerEnvironmentPropCategory.Tree => "Trees",
                RunnerEnvironmentPropCategory.ParkedCar => "Vehicles",
                RunnerEnvironmentPropCategory.Billboard => "Billboards",
                RunnerEnvironmentPropCategory.Miscellaneous => "Miscellaneous",
                _ => "StreetProps"
            };

            Transform container = segment.Find(containerName);
            if (container != null)
            {
                return container;
            }

            GameObject containerObject = new(containerName);
            containerObject.transform.SetParent(segment, false);
            return containerObject.transform;
        }

        private void EnsureSegmentContainers(Transform segment)
        {
            GetOrCreateContainer(segment, RunnerEnvironmentPropCategory.Building);
            GetOrCreateContainer(segment, RunnerEnvironmentPropCategory.Tree);
            GetOrCreateContainer(segment, RunnerEnvironmentPropCategory.ParkedCar);
            GetOrCreateContainer(segment, RunnerEnvironmentPropCategory.Streetlight);
            GetOrCreateContainer(segment, RunnerEnvironmentPropCategory.Billboard);
            GetOrCreateContainer(segment, RunnerEnvironmentPropCategory.Miscellaneous);
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            GameObject poolObject = new("Environment Prop Pool");
            poolObject.transform.SetParent(transform, false);
            poolRoot = poolObject.transform;
        }

        private float GetNextSpacing()
        {
            float min = Mathf.Max(0.1f, Mathf.Min(minimumSpacing, maximumSpacing));
            float max = Mathf.Max(min, Mathf.Max(minimumSpacing, maximumSpacing));
            float nominal = Mathf.Clamp(spawnPointDistance, min, max);
            float halfRange = Mathf.Min(nominal - min, max - nominal);

            if (halfRange <= 0.01f)
            {
                return UnityEngine.Random.Range(min, max);
            }

            return UnityEngine.Random.Range(nominal - halfRange, nominal + halfRange);
        }

        private static int CountValidPrefabs(GameObject[] prefabs)
        {
            if (prefabs == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetWorldBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static bool IsGroundPart(Transform candidate, Transform segment)
        {
            Transform current = candidate;
            while (current != null && current != segment)
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

        private sealed class SideHistory
        {
            public RunnerEnvironmentPropCategory LastCategory { get; private set; }
            public int ConsecutiveCount { get; private set; }
            public GameObject LastPrefab { get; private set; }

            public void Record(RunnerEnvironmentPropCategory category, GameObject prefab)
            {
                ConsecutiveCount = category == LastCategory ? ConsecutiveCount + 1 : 1;
                LastCategory = category;
                LastPrefab = prefab;
            }
        }

        private sealed class PropMetadata
        {
            public PropMetadata(Quaternion baseRotation, Vector3 baseScale)
            {
                BaseRotation = baseRotation;
                BaseScale = baseScale;
            }

            public Quaternion BaseRotation { get; }
            public Vector3 BaseScale { get; }
        }

        private sealed class ActiveProp
        {
            public ActiveProp(
                GameObject instance,
                RunnerEnvironmentPropCategory category,
                GameObject sourcePrefab,
                PropMetadata metadata)
            {
                Instance = instance;
                Category = category;
                SourcePrefab = sourcePrefab;
                Metadata = metadata;
            }

            public GameObject Instance { get; }
            public RunnerEnvironmentPropCategory Category { get; }
            public GameObject SourcePrefab { get; }
            public PropMetadata Metadata { get; }
        }
    }
}
