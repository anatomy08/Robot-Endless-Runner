using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class RunnerBuildingScenery : MonoBehaviour
    {
        [SerializeField] private RunnerGameManager gameManager;
        [SerializeField] private Material[] buildingMaterials;
        [SerializeField] private int buildingsPerSide = 18;
        [SerializeField] private float startZ = 8f;
        [SerializeField] private float spacingZ = 7f;
        [SerializeField] private float recycleZ = -16f;
        [SerializeField] private float sideOffset = 5.6f;
        [SerializeField] private float sideDepthJitter = 2.4f;
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
            if (gameManager == null || gameManager.IsGameOver)
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

        public void Configure(RunnerGameManager manager, Material[] materials)
        {
            gameManager = manager;
            buildingMaterials = materials;
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
    }
}
