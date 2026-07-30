using UnityEngine;

namespace EndlessRunner
{
    public static class RunnerProceduralPropFactory
    {
        public static GameObject Create(
            RunnerEnvironmentPropCategory category,
            RunnerEnvironmentPropPalette palette)
        {
            GameObject root = new(category.ToString());
            palette ??= new RunnerEnvironmentPropPalette();

            switch (category)
            {
                case RunnerEnvironmentPropCategory.Building:
                    BuildBuilding(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.Tree:
                    BuildTree(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.ParkedCar:
                    BuildCar(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.Streetlight:
                    BuildStreetlight(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.TrafficLight:
                    BuildTrafficLight(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.Billboard:
                    BuildBillboard(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.Bench:
                    BuildBench(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.TrashBin:
                    BuildTrashBin(root.transform, palette);
                    break;
                case RunnerEnvironmentPropCategory.RoadSign:
                    BuildRoadSign(root.transform, palette);
                    break;
                default:
                    BuildMiscellaneous(root.transform, palette);
                    break;
            }

            return root;
        }

        private static void BuildBuilding(Transform root, RunnerEnvironmentPropPalette palette)
        {
            float width = Random.Range(2.5f, 3.8f);
            float height = Random.Range(5f, 11f);
            float depth = Random.Range(2.6f, 4f);
            Material bodyMaterial = palette.GetRandomBuildingMaterial();

            CreatePart(
                root,
                PrimitiveType.Cube,
                "Building Body",
                new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, depth),
                Vector3.zero,
                bodyMaterial);

            int windowRows = Mathf.Clamp(Mathf.FloorToInt(height / 1.7f), 2, 6);
            for (int i = 0; i < windowRows; i++)
            {
                float y = 1.2f + i * 1.45f;
                CreatePart(
                    root,
                    PrimitiveType.Cube,
                    "Window Band",
                    new Vector3(0f, y, depth * 0.5f + 0.025f),
                    new Vector3(width * 0.68f, 0.16f, 0.05f),
                    Vector3.zero,
                    palette.WindowMaterial);
                CreatePart(
                    root,
                    PrimitiveType.Cube,
                    "Window Band",
                    new Vector3(0f, y, -depth * 0.5f - 0.025f),
                    new Vector3(width * 0.68f, 0.16f, 0.05f),
                    Vector3.zero,
                    palette.WindowMaterial);
            }
        }

        private static void BuildTree(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(
                root,
                PrimitiveType.Cylinder,
                "Trunk",
                new Vector3(0f, 1.15f, 0f),
                new Vector3(0.28f, 1.15f, 0.28f),
                Vector3.zero,
                palette.TrunkMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Foliage",
                new Vector3(0f, 2.8f, 0f),
                new Vector3(1.8f, 1.8f, 1.8f),
                new Vector3(0f, 35f, 0f),
                palette.FoliageMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Foliage",
                new Vector3(0.35f, 3.7f, -0.1f),
                new Vector3(1.35f, 1.35f, 1.35f),
                new Vector3(18f, 12f, 8f),
                palette.FoliageMaterial);
        }

        private static void BuildCar(Transform root, RunnerEnvironmentPropPalette palette)
        {
            Material bodyMaterial = palette.VehicleMaterial ?? palette.GetRandomBuildingMaterial();
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Car Body",
                new Vector3(0f, 0.48f, 0f),
                new Vector3(1.75f, 0.58f, 3.1f),
                Vector3.zero,
                bodyMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Car Cabin",
                new Vector3(0f, 0.95f, -0.15f),
                new Vector3(1.35f, 0.62f, 1.55f),
                Vector3.zero,
                palette.WindowMaterial);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    CreatePart(
                        root,
                        PrimitiveType.Cylinder,
                        "Wheel",
                        new Vector3(x * 0.86f, 0.3f, z * 1f),
                        new Vector3(0.3f, 0.14f, 0.3f),
                        new Vector3(0f, 0f, 90f),
                        palette.MetalMaterial);
                }
            }
        }

        private static void BuildStreetlight(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(
                root,
                PrimitiveType.Cylinder,
                "Pole",
                new Vector3(0f, 2f, 0f),
                new Vector3(0.08f, 2f, 0.08f),
                Vector3.zero,
                palette.MetalMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Lamp Arm",
                new Vector3(0f, 3.95f, 0.35f),
                new Vector3(0.1f, 0.1f, 0.8f),
                Vector3.zero,
                palette.MetalMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Lamp",
                new Vector3(0f, 3.85f, 0.72f),
                new Vector3(0.34f, 0.22f, 0.38f),
                Vector3.zero,
                palette.YellowMaterial);
        }

        private static void BuildTrafficLight(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(
                root,
                PrimitiveType.Cylinder,
                "Pole",
                new Vector3(0f, 1.65f, 0f),
                new Vector3(0.08f, 1.65f, 0.08f),
                Vector3.zero,
                palette.MetalMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Signal Housing",
                new Vector3(0f, 3.45f, 0f),
                new Vector3(0.55f, 1.4f, 0.38f),
                Vector3.zero,
                palette.MetalMaterial);
            CreatePart(root, PrimitiveType.Sphere, "Red Signal", new Vector3(0f, 3.88f, 0.22f), Vector3.one * 0.26f, Vector3.zero, palette.RedMaterial);
            CreatePart(root, PrimitiveType.Sphere, "Yellow Signal", new Vector3(0f, 3.45f, 0.22f), Vector3.one * 0.26f, Vector3.zero, palette.YellowMaterial);
            CreatePart(root, PrimitiveType.Sphere, "Green Signal", new Vector3(0f, 3.02f, 0.22f), Vector3.one * 0.26f, Vector3.zero, palette.GreenMaterial);
        }

        private static void BuildBillboard(Transform root, RunnerEnvironmentPropPalette palette)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePart(
                    root,
                    PrimitiveType.Cylinder,
                    "Billboard Post",
                    new Vector3(side * 1.05f, 1.5f, 0f),
                    new Vector3(0.08f, 1.5f, 0.08f),
                    Vector3.zero,
                    palette.MetalMaterial);
            }

            CreatePart(
                root,
                PrimitiveType.Cube,
                "Billboard Face",
                new Vector3(0f, 3.1f, 0f),
                new Vector3(3f, 1.55f, 0.18f),
                Vector3.zero,
                palette.VehicleMaterial ?? palette.GreenMaterial);
            CreatePart(
                root,
                PrimitiveType.Cube,
                "Billboard Stripe",
                new Vector3(0f, 3.1f, 0.1f),
                new Vector3(2.2f, 0.24f, 0.04f),
                Vector3.zero,
                palette.YellowMaterial);
        }

        private static void BuildBench(Transform root, RunnerEnvironmentPropPalette palette)
        {
            Material material = palette.FurnitureMaterial ?? palette.TrunkMaterial;
            CreatePart(root, PrimitiveType.Cube, "Seat", new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 0.18f, 0.62f), Vector3.zero, material);
            CreatePart(root, PrimitiveType.Cube, "Back", new Vector3(0f, 1.15f, -0.26f), new Vector3(1.9f, 0.85f, 0.14f), new Vector3(-8f, 0f, 0f), material);
            CreatePart(root, PrimitiveType.Cube, "Leg", new Vector3(-0.7f, 0.3f, 0f), new Vector3(0.16f, 0.6f, 0.48f), Vector3.zero, palette.MetalMaterial);
            CreatePart(root, PrimitiveType.Cube, "Leg", new Vector3(0.7f, 0.3f, 0f), new Vector3(0.16f, 0.6f, 0.48f), Vector3.zero, palette.MetalMaterial);
        }

        private static void BuildTrashBin(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(root, PrimitiveType.Cube, "Bin", new Vector3(0f, 0.55f, 0f), new Vector3(0.72f, 1.1f, 0.72f), Vector3.zero, palette.FurnitureMaterial);
            CreatePart(root, PrimitiveType.Cube, "Lid", new Vector3(0f, 1.14f, 0f), new Vector3(0.82f, 0.12f, 0.82f), Vector3.zero, palette.MetalMaterial);
        }

        private static void BuildRoadSign(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(root, PrimitiveType.Cylinder, "Sign Pole", new Vector3(0f, 1.2f, 0f), new Vector3(0.07f, 1.2f, 0.07f), Vector3.zero, palette.MetalMaterial);
            CreatePart(root, PrimitiveType.Cube, "Sign Face", new Vector3(0f, 2.35f, 0f), new Vector3(1.1f, 0.8f, 0.12f), Vector3.zero, palette.RedMaterial);
            CreatePart(root, PrimitiveType.Cube, "Sign Mark", new Vector3(0f, 2.35f, 0.07f), new Vector3(0.62f, 0.13f, 0.03f), Vector3.zero, palette.YellowMaterial);
        }

        private static void BuildMiscellaneous(Transform root, RunnerEnvironmentPropPalette palette)
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    BuildFireHydrant(root, palette);
                    break;
                case 1:
                    BuildBusStop(root, palette);
                    break;
                case 2:
                    BuildFence(root, palette);
                    break;
                default:
                    BuildPlanter(root, palette);
                    break;
            }
        }

        private static void BuildFireHydrant(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(root, PrimitiveType.Cylinder, "Hydrant Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.28f, 0.55f, 0.28f), Vector3.zero, palette.RedMaterial);
            CreatePart(root, PrimitiveType.Cylinder, "Hydrant Cap", new Vector3(0f, 1.12f, 0f), new Vector3(0.38f, 0.12f, 0.38f), Vector3.zero, palette.YellowMaterial);
            CreatePart(root, PrimitiveType.Cylinder, "Hydrant Side", new Vector3(0.38f, 0.62f, 0f), new Vector3(0.18f, 0.28f, 0.18f), new Vector3(0f, 0f, 90f), palette.RedMaterial);
        }

        private static void BuildBusStop(Transform root, RunnerEnvironmentPropPalette palette)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePart(root, PrimitiveType.Cylinder, "Shelter Post", new Vector3(side * 1.15f, 1.2f, 0f), new Vector3(0.07f, 1.2f, 0.07f), Vector3.zero, palette.MetalMaterial);
            }

            CreatePart(root, PrimitiveType.Cube, "Shelter Roof", new Vector3(0f, 2.42f, 0f), new Vector3(2.65f, 0.14f, 1.2f), Vector3.zero, palette.GreenMaterial);
            CreatePart(root, PrimitiveType.Cube, "Shelter Back", new Vector3(0f, 1.3f, -0.52f), new Vector3(2.45f, 2.15f, 0.08f), Vector3.zero, palette.WindowMaterial);
            CreatePart(root, PrimitiveType.Cube, "Shelter Bench", new Vector3(0f, 0.62f, -0.2f), new Vector3(1.8f, 0.16f, 0.52f), Vector3.zero, palette.FurnitureMaterial);
        }

        private static void BuildFence(Transform root, RunnerEnvironmentPropPalette palette)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePart(root, PrimitiveType.Cube, "Fence Post", new Vector3(side * 1.25f, 0.65f, 0f), new Vector3(0.16f, 1.3f, 0.16f), Vector3.zero, palette.MetalMaterial);
            }

            CreatePart(root, PrimitiveType.Cube, "Fence Rail", new Vector3(0f, 0.42f, 0f), new Vector3(2.6f, 0.14f, 0.14f), Vector3.zero, palette.YellowMaterial);
            CreatePart(root, PrimitiveType.Cube, "Fence Rail", new Vector3(0f, 0.92f, 0f), new Vector3(2.6f, 0.14f, 0.14f), Vector3.zero, palette.YellowMaterial);
        }

        private static void BuildPlanter(Transform root, RunnerEnvironmentPropPalette palette)
        {
            CreatePart(root, PrimitiveType.Cube, "Planter", new Vector3(0f, 0.35f, 0f), new Vector3(1.35f, 0.7f, 1.1f), Vector3.zero, palette.FurnitureMaterial);
            CreatePart(root, PrimitiveType.Cube, "Plant", new Vector3(-0.25f, 1f, 0f), new Vector3(0.55f, 0.9f, 0.55f), new Vector3(0f, 25f, 0f), palette.FoliageMaterial);
            CreatePart(root, PrimitiveType.Cube, "Plant", new Vector3(0.3f, 1.08f, 0.08f), new Vector3(0.5f, 1.05f, 0.5f), new Vector3(0f, -25f, 0f), palette.FoliageMaterial);
        }

        private static GameObject CreatePart(
            Transform parent,
            PrimitiveType primitiveType,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEulerAngles,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles);
            part.transform.localScale = localScale;

            if (part.TryGetComponent(out Collider collider))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }

            if (material != null && part.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }
    }
}
