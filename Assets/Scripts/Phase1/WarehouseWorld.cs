#if WAREHOUSE_LEGACY_FREE_WALK
// Historical walking prototype. This symbol is intentionally absent in all current profiles.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WarehouseSimulator.Phase1
{
    /// <summary>
    /// Procedural blockout: all meshes share a small material pool and unnecessary
    /// MeshColliders/BoxColliders are removed. Placeholder only, not final art.
    /// </summary>
    public static class WarehouseWorld
    {
        private static Transform _root;
        private static readonly Dictionary<Color32, Material> Materials = new Dictionary<Color32, Material>();
        private static readonly Color32 Concrete = new Color32(105, 118, 127, 255);
        private static readonly Color32 Wall = new Color32(194, 202, 204, 255);
        private static readonly Color32 Steel = new Color32(45, 54, 64, 255);
        private static readonly Color32 Yellow = new Color32(244, 180, 63, 255);
        private static readonly Color32 Orange = new Color32(214, 91, 46, 255);
        private static readonly Color32 Cardboard = new Color32(175, 128, 82, 255);
        private static readonly Color32 White = new Color32(237, 233, 219, 255);
        private static readonly Color32 Belt = new Color32(55, 65, 71, 255);
        private static readonly Color32 Glass = new Color32(74, 112, 132, 255);

        public static void Build(Transform parent)
        {
            Materials.Clear();
            var world = new GameObject("WarehouseEnvironment");
            world.transform.SetParent(parent, false);
            _root = world.transform;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.61f, 0.66f, 0.72f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(_root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(52f, -34f, 0f);
            sun.intensity = 1.6f;
            // No realtime shadows on the Phase 1 mobile baseline.
            // Production will use baked lighting and selectively enabled shadows.
            sun.shadows = LightShadows.None;

            Box("WarehouseFloor", new Vector3(0f, -0.15f, 0f), new Vector3(34f, 0.3f, 27f), Concrete, true);
            Box("RearWall", new Vector3(0f, 4f, -13.6f), new Vector3(34f, 8f, .35f), Wall, true);
            Box("LeftWall", new Vector3(-17f, 4f, 0f), new Vector3(.35f, 8f, 27f), Wall, true);
            Box("RightWall", new Vector3(17f, 4f, 0f), new Vector3(.35f, 8f, 27f), Wall, true);
            Box("Ceiling", new Vector3(0f, 8.25f, 0f), new Vector3(34f, .35f, 27f), Steel);
            // Loading-bay facade: open spaces between posts, no full front wall.
            for (int x = -13; x <= 13; x += 6)
                Box("LoadingBayPost", new Vector3(x, 4f, 13.4f), new Vector3(.4f, 8f, .5f), Steel, true);
            Box("LoadingBayHeader", new Vector3(0f, 7.6f, 13.4f), new Vector3(34f, .8f, .5f), Orange);

            for (int x = -12; x <= 12; x += 6)
                Box("FloorLaneMark", new Vector3(x, .015f, 0f), new Vector3(.08f, .025f, 24f), White);
            for (int x = -13; x <= 13; x += 13)
                Box("SafetyMark", new Vector3(x, .02f, 8f), new Vector3(2.8f, .03f, .18f), Yellow);
            for (int x = -12; x <= 12; x += 8)
                Box("CeilingLight", new Vector3(x, 8.02f, -2f), new Vector3(3.2f, .05f, .28f), White);

            Conveyor(-6.5f, -2.5f, 10f);
            Conveyor(1.5f, -3f, 8f);
            Conveyor(8.5f, -1.5f, 9f);

            for (int i = 0; i < 8; i++)
            {
                float x = i < 4 ? -6.5f : (i < 6 ? 1.5f : 8.5f);
                float z = i < 4 ? -5f + i * 2f : (i < 6 ? -5f + (i - 4) * 2f : -5f + (i - 6) * 2f);
                Parcel(new Vector3(x, 1.35f, z), i);
            }
            for (int x = -13; x <= 13; x += 26)
                for (int z = -7; z <= 8; z += 5) Rack(x, z);

            // External yard and a stationary visual placeholder for future truck logistics.
            Box("Yard", new Vector3(0f, -.19f, 28f), new Vector3(50f, .3f, 36f), Steel, true);
            Box("YardLaneStripe", new Vector3(0f, 0f, 30f), new Vector3(32f, .03f, .12f), Yellow);
            TruckPlaceholder(7f, 27f);
        }

        private static Material MaterialFor(Color32 color)
        {
            if (Materials.TryGetValue(color, out var material)) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = color;
            material.enableInstancing = true;
            Materials[color] = material;
            return material;
        }

        private static GameObject Box(string name, Vector3 pos, Vector3 size, Color32 color, bool solid = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(_root, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            obj.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (!solid) Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }

        private static void Conveyor(float x, float z, float length)
        {
            Box("ConveyorBelt", new Vector3(x, .94f, z), new Vector3(1.8f, .16f, length), Belt, true);
            for (float dz = -length * .5f + .3f; dz < length * .5f; dz += .55f)
                Box("Roller", new Vector3(x, 1.035f, z + dz), new Vector3(1.7f, .035f, .08f), Concrete);
            foreach (var side in new[] { -.94f, .94f })
            {
                Box("SideRail", new Vector3(x + side, 1.1f, z), new Vector3(.1f, .27f, length), Steel);
                foreach (var end in new[] { -length * .45f, length * .45f })
                    Box("Leg", new Vector3(x + side, .46f, z + end), new Vector3(.12f, .9f, .12f), Steel);
            }
        }

        private static void Parcel(Vector3 pos, int index)
        {
            var sizes = new[] { new Vector3(.65f, .55f, .68f), new Vector3(.85f, .47f, .7f), new Vector3(.6f, .7f, .56f) };
            var size = sizes[index % sizes.Length];
            var box = Box("Parcel_VisualOnly", pos, size, Cardboard);
            box.transform.localRotation = Quaternion.Euler(0f, (index % 3) * 7f, 0f);
            Box("ParcelLabel", pos + Vector3.up * (size.y * .5f + .006f), new Vector3(size.x * .7f, .012f, size.z * .23f), White);
        }

        private static void Rack(float x, float z)
        {
            for (int level = 0; level < 3; level++)
                Box("RackShelf", new Vector3(x, .25f + level * 1.05f, z), new Vector3(2.2f, .13f, 1.75f), Steel, true);
            foreach (float dx in new[] { -1.02f, 1.02f })
                foreach (float dz in new[] { -.8f, .8f })
                    Box("RackPost", new Vector3(x + dx, 1.65f, z + dz), new Vector3(.11f, 3.3f, .11f), Steel, true);
        }

        private static void TruckPlaceholder(float x, float z)
        {
            Box("TruckCargo_VisualOnly", new Vector3(x, 1.9f, z - 2f), new Vector3(3.3f, 3.15f, 6f), White);
            Box("TruckCab_VisualOnly", new Vector3(x, 1.48f, z + 2.65f), new Vector3(3f, 2.7f, 2.4f), Orange);
            Box("TruckWindshield", new Vector3(x, 2.05f, z + 3.9f), new Vector3(2.45f, .8f, .09f), Glass);
            foreach (float side in new[] { -1.66f, 1.66f })
            {
                foreach (float offset in new[] { -4.2f, -1.5f, 2.9f })
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "TruckWheel";
                    wheel.transform.SetParent(_root, false);
                    wheel.transform.localPosition = new Vector3(x + side, .54f, z + offset);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale = new Vector3(.98f, .19f, .98f);
                    wheel.GetComponent<Renderer>().sharedMaterial = MaterialFor(Steel);
                    wheel.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Object.Destroy(wheel.GetComponent<Collider>());
                }
            }
        }
    }
}

#endif
