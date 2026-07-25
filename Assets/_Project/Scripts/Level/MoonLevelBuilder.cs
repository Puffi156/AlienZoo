using FishNet.Object;
using UnityEngine;

namespace AlienZoo.Level
{
    /// <summary>
    /// Greybox generator for Moon 01 — "The Alien Farm". Builds a blockout of the approved layout
    /// (ground, farmhouses, hazards, barn/silo/shed/stables, misc props, ship + spawn + teleporter)
    /// out of primitives, tinted to the concept-art palette so it reads as the alien farm at dusk.
    ///
    /// This is a LAYOUT/BLOCKOUT tool, not final art: it exists so the level is instantly playable
    /// and spatially correct. Swap each primitive for real models later (see MOON_01_ART.md).
    ///
    /// Usage: drop this component on an empty GameObject at the world origin, then right-click it
    /// and choose "Build Moon 01 Blockout". Save the scene afterward so the networked farmhouse
    /// doors receive scene IDs.
    /// </summary>
    public class MoonLevelBuilder : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private string _rootName = "Moon01_Blockout";
        [SerializeField] private bool _applyAtmosphere = true;

        // ---- Palette sampled from the concept art ----
        static readonly Color GroundCol   = new Color(0.48f, 0.36f, 0.55f); // mauve rock ground
        static readonly Color BuildingCol = new Color(0.42f, 0.63f, 0.68f); // teal alien dome
        static readonly Color AccentCol   = new Color(0.88f, 0.55f, 0.24f); // orange bio-light
        static readonly Color AcidCol     = new Color(0.56f, 0.90f, 0.25f); // toxic green
        static readonly Color CornCol     = new Color(0.33f, 0.55f, 0.30f); // alien crop
        static readonly Color RockCol     = new Color(0.35f, 0.28f, 0.42f); // dark purple rock
        static readonly Color CrystalCol  = new Color(0.77f, 0.42f, 0.88f); // purple crystal
        static readonly Color MetalCol    = new Color(0.45f, 0.34f, 0.28f); // rusted tractor metal

        // =====================================================================================

        [ContextMenu("Build Moon 01 Blockout")]
        public void Build()
        {
            Clear();
            var root = new GameObject(_rootName).transform;
            root.SetParent(transform, false);

            BuildGround(root);
            BuildLandingZone(root);                                                 // ship + spawn + teleporter (SW)
            BuildFarmhouse(root, "Farmhouse_A", new Vector3(-32, 0,  6),  90f);      // easy — faces east
            BuildFarmhouse(root, "Farmhouse_B", new Vector3(  0, 0, 50), 180f);      // behind cornfield — faces south
            BuildFarmhouse(root, "Farmhouse_C", new Vector3( 48, 0, 42), 225f);      // far NE — faces SW
            BuildBarn(root,  new Vector3( 2, 0,  0));
            BuildSilo(root,  new Vector3(12, 0, -4));
            BuildShed(root,  new Vector3( 6, 0, -34));
            BuildStable(root, "Stable_1", new Vector3(-42, 0, 20));                  // west pasture
            BuildStable(root, "Stable_2", new Vector3( 40, 0, -24));                 // SE, past acid
            BuildAcidLake(root, new Vector3(34, 0, 8), 15f);
            BuildCornfield(root, new Vector3(-8, 0, 40), new Vector3(38, 7, 24));
            BuildRockCluster(root, new Vector3(46, 0, 46), 6, 4f);                   // NE ridge / vantage
            BuildRockCluster(root, new Vector3(20, 0, 12), 3, 3f);                   // land-bridge choke
            BuildTractor(root, new Vector3(-12, 0, -18));
            BuildFences(root);
            BuildCrystals(root);

            if (_applyAtmosphere) ApplyAtmosphere();

            Debug.Log("[MoonLevelBuilder] Built Moon 01 blockout. Now SAVE THE SCENE so the networked farmhouse doors get scene IDs.");
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            var existing = transform.Find(_rootName);
            if (existing != null) DestroyImmediate(existing.gameObject);
        }

        // ===================================== POIs ==========================================

        private void BuildGround(Transform root)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground";
            g.transform.SetParent(root, false);
            g.transform.localScale = new Vector3(13f, 1f, 13f); // ~130m across
            Paint(g, GroundCol);
        }

        private void BuildLandingZone(Transform root)
        {
            var zone = new GameObject("LandingZone").transform;
            zone.SetParent(root, false);
            zone.localPosition = new Vector3(-45, 0, -45);

            // Ship body (placeholder — swap for the real ship model).
            var ship = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ship.name = "Ship_Placeholder";
            ship.transform.SetParent(zone, false);
            ship.transform.localScale = new Vector3(10, 5, 14);
            ship.transform.localPosition = new Vector3(0, 2.5f, 0);
            Paint(ship, MetalCol);

            // Player spawn marker (point PlayerSpawner's SpawnPoints at this).
            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(zone, false);
            spawn.transform.localPosition = new Vector3(6, 1, 4);

            // Teleporter pad placeholder — replace with the real TeleporterPad prefab.
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "TeleporterPad_Placeholder";
            pad.transform.SetParent(zone, false);
            pad.transform.localScale = new Vector3(3, 0.1f, 3);
            pad.transform.localPosition = new Vector3(9, 0.1f, 6);
            DestroyImmediate(pad.GetComponent<Collider>());
            Paint(pad, BuildingCol, BuildingCol * 0.8f);
        }

        private void BuildFarmhouse(Transform root, string name, Vector3 pos, float yaw)
        {
            var house = new GameObject(name).transform;
            house.SetParent(root, false);
            house.localPosition = pos;
            house.localRotation = Quaternion.Euler(0, yaw, 0);

            // Dome body (solid).
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Dome";
            dome.transform.SetParent(house, false);
            dome.transform.localScale = new Vector3(12, 8, 12);
            dome.transform.localPosition = new Vector3(0, 3, 0);
            Paint(dome, BuildingCol);
            AddBioLight(dome.transform, new Vector3(0, 0.15f, 0.42f));

            // Door: solid frame + proximity trigger + networked EntranceDoor.
            var door = new GameObject("Door");
            door.transform.SetParent(house, false);
            door.transform.localPosition = new Vector3(0, 1.5f, 6.2f);

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "DoorFrame";
            frame.transform.SetParent(door.transform, false);
            frame.transform.localScale = new Vector3(2.4f, 3f, 0.4f);
            Paint(frame, AccentCol, AccentCol * 0.5f);

            var proximity = door.AddComponent<BoxCollider>(); // interaction range
            proximity.isTrigger = true;
            proximity.size = new Vector3(4.5f, 3f, 4.5f);
            proximity.center = new Vector3(0, 0, 1f);

            door.AddComponent<NetworkObject>();
            var entrance = door.AddComponent<EntranceDoor>();
            // interior scene left blank on purpose (interiors are a later pass).
        }

        private void BuildBarn(Transform root, Vector3 pos)
        {
            var b = Solid(root, "Barn", pos, new Vector3(14, 8, 10), BuildingCol);
            AddBioLight(b.transform, new Vector3(0, 0.1f, 0.5f));
        }

        private void BuildSilo(Transform root, Vector3 pos)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            s.name = "Silo";
            s.transform.SetParent(root, false);
            s.transform.localScale = new Vector3(4, 9, 4); // ~18m tall nav-beacon
            s.transform.localPosition = pos + Vector3.up * 9f;
            Paint(s, BuildingCol);
        }

        private void BuildShed(Transform root, Vector3 pos)
        {
            Solid(root, "Shed", pos, new Vector3(7, 4, 6), BuildingCol);
        }

        private void BuildStable(Transform root, string name, Vector3 pos)
        {
            Solid(root, name, pos, new Vector3(10, 5, 7), BuildingCol);
            // Anchor for quota-animal spawns near the pen.
            var spawn = new GameObject(name + "_AnimalSpawn");
            spawn.transform.SetParent(root, false);
            spawn.transform.localPosition = pos + new Vector3(0, 0, 6);
        }

        private void BuildAcidLake(Transform root, Vector3 pos, float radius)
        {
            var lake = new GameObject("AcidLake").transform;
            lake.SetParent(root, false);
            lake.localPosition = pos;

            // Glowing surface (visual only, no collider).
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surface.name = "AcidSurface";
            surface.transform.SetParent(lake, false);
            surface.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            surface.transform.localPosition = new Vector3(0, 0.2f, 0);
            DestroyImmediate(surface.GetComponent<Collider>());
            Paint(surface, AcidCol, AcidCol * 1.1f);

            // Hazard trigger volume (slow + DoT).
            var trig = lake.gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = new Vector3(radius * 2f, 3f, radius * 2f);
            trig.center = new Vector3(0, 0.6f, 0);
            lake.gameObject.AddComponent<HazardCollider>();
        }

        private void BuildCornfield(Transform root, Vector3 pos, Vector3 size)
        {
            var field = new GameObject("Cornfield").transform;
            field.SetParent(root, false);
            field.localPosition = pos;

            // Foliage trigger volume (soft slow + rustle + sight block).
            var trig = field.gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = size;
            trig.center = new Vector3(0, size.y * 0.5f, 0);
            field.gameObject.AddComponent<FoliageTrigger>();

            // Visual stalks (no colliders — they must not block movement).
            int cols = Mathf.Clamp((int)(size.x / 3f), 2, 12);
            int rows = Mathf.Clamp((int)(size.z / 3f), 2, 8);
            var stalks = new GameObject("Stalks").transform;
            stalks.SetParent(field, false);
            for (int x = 0; x < cols; x++)
            for (int z = 0; z < rows; z++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.transform.SetParent(stalks, false);
                float px = -size.x / 2f + (x + 0.5f) * (size.x / cols);
                float pz = -size.z / 2f + (z + 0.5f) * (size.z / rows);
                float h = size.y * Random.Range(0.7f, 1f);
                s.transform.localPosition = new Vector3(px, h / 2f, pz);
                s.transform.localScale = new Vector3(0.3f, h, 0.3f);
                DestroyImmediate(s.GetComponent<Collider>());
                Paint(s, CornCol);
            }
        }

        private void BuildRockCluster(Transform root, Vector3 pos, int count, float scale)
        {
            var cluster = new GameObject("Rocks").transform;
            cluster.SetParent(root, false);
            cluster.localPosition = pos;
            for (int i = 0; i < count; i++)
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.transform.SetParent(cluster, false);
                Vector3 offset = new Vector3(Random.Range(-scale, scale), 0, Random.Range(-scale, scale));
                float s = Random.Range(scale * 0.6f, scale * 1.4f);
                r.transform.localPosition = offset + Vector3.up * (s * 0.4f);
                r.transform.localScale = new Vector3(s, s * Random.Range(0.8f, 1.6f), s);
                r.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0, 360f), Random.Range(-12f, 12f));
                Paint(r, RockCol);
            }
        }

        private void BuildTractor(Transform root, Vector3 pos)
        {
            var t = new GameObject("AlienTractor").transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, 35f, 0);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(t, false);
            body.transform.localScale = new Vector3(2.4f, 1.6f, 4f);
            body.transform.localPosition = new Vector3(0, 1.4f, 0);
            Paint(body, MetalCol);

            for (int i = 0; i < 4; i++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = "Wheel";
                w.transform.SetParent(t, false);
                bool front = i < 2;
                float sx = (i % 2 == 0) ? -1f : 1f;
                w.transform.localScale = front ? new Vector3(0.9f, 0.3f, 0.9f) : new Vector3(1.4f, 0.4f, 1.4f);
                w.transform.localPosition = new Vector3(sx * 1.4f, front ? 0.9f : 1.2f, front ? 1.5f : -1.3f);
                w.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                Paint(w, RockCol);
            }
        }

        private void BuildFences(Transform root)
        {
            // A broken fence line that channels movement toward a single gate near the barn compound.
            FenceRun(root, new Vector3(-4, 0, -8), new Vector3(18, 0, -8), gapAt: 0.55f);
            FenceRun(root, new Vector3(18, 0, -8), new Vector3(18, 0, 6), gapAt: -1f);
        }

        private void FenceRun(Transform root, Vector3 a, Vector3 b, float gapAt)
        {
            var run = new GameObject("Fence").transform;
            run.SetParent(root, false);
            int posts = 10;
            for (int i = 0; i <= posts; i++)
            {
                float t = i / (float)posts;
                if (gapAt >= 0f && Mathf.Abs(t - gapAt) < 0.12f) continue; // leave a gate gap
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.transform.SetParent(run, false);
                p.transform.localPosition = Vector3.Lerp(a, b, t) + Vector3.up * 0.9f;
                p.transform.localScale = new Vector3(0.2f, 1.8f, 0.2f);
                Paint(p, MetalCol);
            }
        }

        private void BuildCrystals(Transform root)
        {
            Vector3[] spots =
            {
                new Vector3(-48, 0, -30), new Vector3(-40, 0, -38),
                new Vector3(30, 0, 40), new Vector3(52, 0, 20), new Vector3(-20, 0, 52)
            };
            var group = new GameObject("Crystals").transform;
            group.SetParent(root, false);
            foreach (var spot in spots)
            {
                int shards = Random.Range(2, 5);
                for (int i = 0; i < shards; i++)
                {
                    var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.SetParent(group, false);
                    float h = Random.Range(2f, 5f);
                    c.transform.localPosition = spot + new Vector3(Random.Range(-2f, 2f), h * 0.4f, Random.Range(-2f, 2f));
                    c.transform.localScale = new Vector3(0.6f, h, 0.6f);
                    c.transform.localRotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0, 360f), Random.Range(-20f, 20f));
                    DestroyImmediate(c.GetComponent<Collider>());
                    Paint(c, CrystalCol, CrystalCol * 0.7f);
                }
            }
        }

        // =================================== helpers =========================================

        private GameObject Solid(Transform root, string name, Vector3 pos, Vector3 size, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // keeps a solid BoxCollider
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localScale = size;
            go.transform.localPosition = pos + Vector3.up * (size.y * 0.5f);
            Paint(go, col);
            return go;
        }

        private void AddBioLight(Transform parent, Vector3 localPos)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "BioLight";
            w.transform.SetParent(parent, false);
            w.transform.localPosition = localPos;
            w.transform.localScale = new Vector3(0.14f, 0.06f, 0.14f);
            DestroyImmediate(w.GetComponent<Collider>());
            Paint(w, AccentCol, AccentCol);
        }

        private static void Paint(GameObject go, Color color, Color? emission = null)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;

            var m = new Material(Shader.Find("Standard")) { color = color };
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            r.sharedMaterial = m;
        }

        private void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.30f, 0.32f, 0.42f);   // dusk purple haze
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.34f, 0.48f); // purple ambient

            var sun = RenderSettings.sun != null ? RenderSettings.sun : Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.color = new Color(1f, 0.78f, 0.55f);                  // warm low alien sun
                sun.transform.rotation = Quaternion.Euler(18f, -35f, 0f);
                sun.intensity = 1.1f;
            }
        }
    }
}
