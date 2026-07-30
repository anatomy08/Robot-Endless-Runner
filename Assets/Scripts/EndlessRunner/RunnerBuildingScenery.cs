using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class RunnerBuildingScenery : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Material[] buildingMaterials;
        [SerializeField] private Material windowMaterial;
        [SerializeField] private int buildingsPerSide = 22;
        [SerializeField] private float startZ = 0f;
        [SerializeField] private float spacingZ = 12f;
        [SerializeField] private float recycleZ = -16f;
        [SerializeField] private float sideOffset = 5.2f;
        [SerializeField] private float sideDepthJitter = 1.4f;
        [SerializeField] private Vector2 heightRange = new(4f, 13f);
        [SerializeField] private Vector2 widthRange = new(1.4f, 3.2f);
        [SerializeField] private Vector2 depthRange = new(1.6f, 4f);

        private readonly List<Transform> buildings = new();
        private float furthestZ;

        private void Start()
        {
            BuildScenery();
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsRunActive)
            {
                return;
            }

            float speed = gameManager.CurrentSpeed * Time.deltaTime;
            for (int i = 0; i < buildings.Count; i++)
            {
                Transform building = buildings[i];
                building.position += Vector3.back * speed;

                if (building.position.z < recycleZ)
                {
                    RepositionBuilding(building, furthestZ + spacingZ);
                    furthestZ = building.position.z;
                }
            }
        }

        public void Configure(
            RunnerGameManager manager,
            Material[] materials,
            Material windows = null,
            float buildingSideOffset = 5.2f,
            float buildingDepthJitter = 1.4f)
        {
            gameManager = manager;
            buildingMaterials = materials;
            windowMaterial = windows;
            sideOffset = buildingSideOffset;
            sideDepthJitter = buildingDepthJitter;
        }

        private void BuildScenery()
        {
            if (buildings.Count > 0)
            {
                return;
            }

            for (int i = 0; i < buildingsPerSide; i++)
            {
                float z = startZ + i * spacingZ;
                CreateBuilding(-1, z);
                CreateBuilding(1, z + spacingZ * 0.5f);
                furthestZ = Mathf.Max(furthestZ, z + spacingZ * 0.5f);
            }
        }

        private void CreateBuilding(int side, float z)
        {
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = side < 0 ? "Left Building" : "Right Building";
            building.transform.SetParent(transform);

            if (building.TryGetComponent(out Collider collider))
            {
                Destroy(collider);
            }

            CreateWindowStrips(building.transform);
            buildings.Add(building.transform);
            RepositionBuilding(building.transform, z, side);
        }

        private void RepositionBuilding(Transform building, float z)
        {
            int side = building.position.x < 0f ? -1 : 1;
            RepositionBuilding(building, z, side);
        }

        private void RepositionBuilding(Transform building, float z, int side)
        {
            float width = Random.Range(widthRange.x, widthRange.y);
            float height = Random.Range(heightRange.x, heightRange.y);
            float depth = Random.Range(depthRange.x, depthRange.y);
            float x = side * (sideOffset + Random.Range(0f, sideDepthJitter));

            building.localScale = new Vector3(width, height, depth);
            building.position = new Vector3(x, height * 0.5f - 0.1f, z);

            if (building.TryGetComponent(out Renderer renderer) && buildingMaterials != null && buildingMaterials.Length > 0)
            {
                renderer.sharedMaterial = buildingMaterials[Random.Range(0, buildingMaterials.Length)];
            }
        }

        private void CreateWindowStrips(Transform building)
        {
            for (int i = 0; i < 4; i++)
            {
                CreateWindowStrip(building, i, 0.51f);
                CreateWindowStrip(building, i, -0.51f);
            }
        }

        private void CreateWindowStrip(Transform building, int index, float z)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Window Strip";
            strip.transform.SetParent(building, false);
            strip.transform.localPosition = new Vector3(0f, -0.25f + index * 0.18f, z);
            strip.transform.localScale = new Vector3(0.72f, 0.035f, 0.025f);

            if (strip.TryGetComponent(out Collider collider))
            {
                Destroy(collider);
            }

            if (strip.TryGetComponent(out Renderer renderer) && windowMaterial != null)
            {
                renderer.sharedMaterial = windowMaterial;
            }
        }
    }
}
