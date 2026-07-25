using FishNet.Object;
using UnityEngine;

namespace AlienZoo.Level
{
    /// <summary>
    /// Greybox / assembly generator for Moon 01 — "The Alien Farm". Builds the approved layout
    /// (rolling terrain, farmhouses, hazards, barn/silo/shed/stables, misc props, ship + spawn +
    /// teleporter). Each POI has an optional MODEL PREFAB slot:
    ///   - assign a prefab  → that model is placed at the layout position (real art), and
    ///   - leave it empty   → a palette-tinted, toon-shaded primitive stands in (greybox).
    /// Either way the gameplay wiring (farmhouse doors, acid/foliage triggers, spawns) is created by
    /// the builder, so you can swap in models one at a time without touching the layout or code.
    ///
    /// Usage: put this on an empty GameObject at the world origin, right-click it → "Build Moon 01
    /// Blockout", then SAVE THE SCENE so the networked farmhouse doors receive scene IDs.
    /// </summary>
    public class MoonLevelBuilder : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private string _rootName = "Moon01_Blockout";
        [SerializeField] private bool _applyAtmosphere = true;

        [Header("Terrain (rolling hills — tweak to taste)")]
        [SerializeField] private float _terrainSize = 130f;
        [SerializeField] private int _terrainResolution = 100;
        [SerializeField] private float _hillAmplitude = 3.5f;
        [SerializeField] private float _hillFrequency = 0.045f;
        [SerializeField] private int _terrainSeed = 1337;

        [Header("Model Prefabs (optional — assign to replace the greybox primitive)")]
        [SerializeField] private GameObject _farmhousePrefab;
        [SerializeField] private GameObject _barnPrefab;
        [SerializeField] private GameObject _siloPrefab;
        [SerializeField] private GameObject _shedPrefab;
        [SerializeField] private GameObject _stablePrefab;
        [SerializeField] private GameObject _shipPrefab;
        [SerializeField] private GameObject _teleporterPrefab;
        [SerializeField] private GameObject _acidSurfacePrefab;
        [SerializeField] private GameObject _cornStalkPrefab;
        [SerializeField] private GameObject _rockPrefab;
        [SerializeField] private GameObject _crystalPrefab;
        [SerializeField] private GameObject _tractorPrefab;
        [SerializeField] private GameObject _fencePostPrefab;

        // ---- Palette sampled from the concept art (used for the greybox primitives) ----
        static readonly Color GroundCol   = new Color(0.48f, 0.36f, 0.55f);
        static readonly Color BuildingCol = new Color(0.42f, 0.63f, 0.68f);
        static readonly Color AccentCol   = new Color(0.88f, 0.55f, 0.24f);
        static readonly Color AcidCol     = new Color(0.56f, 0.90f, 0.25f);
        static readonly Color CornCol     = new Color(0.33f, 0.55f, 0.30f);
        static readonly Color RockCol     = new Color(0.35f, 0.28f, 0.42f);
        static readonly Color CrystalCol  = new Color(0.77f, 0.42f, 0.88f);
        static readonly Color MetalCol    = new Color(0.45f, 0.34f, 0.28f);

        private float _noiseOffX, _noiseOffZ;

        // =====================================================================================

        [ContextMenu("Build Moon 01 Blockout")]
        public void Build()
        {
            Clear();
            SeedNoise();

            var root = new GameObject(_rootName).transform;
            root.SetParent(transform, false);

            BuildTerrain(root);
            BuildLandingZone(root);
            BuildFarmhouse(root, "Farmhouse_A", OnGround(-32,  6),  90f);
            BuildFarmhouse(root, "Farmhouse_B", OnGround(  0, 50), 180f);
            BuildFarmhouse(root, "Farmhouse_C", OnGround( 48, 42), 225f);
            BuildBarn(root,   OnGround( 2,  0));
            BuildSilo(root,   OnGround(12, -4));
            BuildShed(root,   OnGround( 6, -34));
            BuildStable(root, "Stable_1", OnGround(-42, 20));
            BuildStable(root, "Stable_2", OnGround( 40, -24));
            BuildAcidLake(root, OnGround(34, 8), 15f);
            BuildCornfield(root, OnGround(-8, 40), new Vector3(38, 7, 24));
            BuildRockCluster(root, OnGround(46, 46), 6, 4f);
            BuildRockCluster(root, OnGround(20, 12), 3, 3f);
            BuildTractor(root, OnGround(-12, -18));
            BuildFences(root);
            BuildCrystals(root);

            if (_applyAtmosphere) ApplyAtmosphere();

            Debug.Log("[MoonLevelBuilder] Built Moon 01. Assign Model Prefabs on this component to replace primitives; re-Build; then SAVE THE SCENE.");
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            var existing = transform.Find(_rootName);
            if (existing != null) DestroyImmediate(existing.gameObject);
        }

        // ================================= terrain / height ==================================

        private void SeedNoise()
        {
            var rng = new System.Random(_terrainSeed);
            _noiseOffX = (float)rng.NextDouble() * 1000f;
            _noiseOffZ = (float)rng.NextDouble() * 1000f;
        }

        public float SampleHeight(float x, float z)
        {
            float n1 = Mathf.PerlinNoise((_noiseOffX + x) * _hillFrequency,        (_noiseOffZ + z) * _hillFrequency);
            float n2 = Mathf.PerlinNoise((_noiseOffX + x) * _hillFrequency * 2.3f, (_noiseOffZ + z) * _hillFrequency * 2.3f);
            float h = n1 * 0.7f + n2 * 0.3f;
            return (h - 0.5f) * 2f * _hillAmplitude;
        }

        private Vector3 OnGround(float x, float z) => new Vector3(x, SampleHeight(x, z), z);

        private void BuildTerrain(Transform root)
        {
            int res = Mathf.Clamp(_terrainResolution, 8, 250);
            float size = _terrainSize;
            float half = size * 0.5f;
            int side = res + 1;

            var verts = new Vector3[side * side];
            var uvs = new Vector2[verts.Length];
            var tris = new int[res * res * 6];

            for (int z = 0; z <= res; z++)
            for (int x = 0; x <= res; x++)
            {
                float wx = -half + (x / (float)res) * size;
                float wz = -half + (z / (float)res) * size;
                int i = z * side + x;
                verts[i] = new Vector3(wx, SampleHeight(wx, wz), wz);
                uvs[i]   = new Vector2(x / (float)res, z / (float)res);
            }

            int t = 0;
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                int i = z * side + x;
                tris[t++] = i;
                tris[t++] = i + side;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + side;
                tris[t++] = i + side + 1;
            }

            var mesh = new Mesh { name = "MoonTerrain" };
            mesh.indexFormat = verts.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Terrain");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            PaintTerrain(go);
        }

        // ===================================== POIs ==========================================

        private void BuildLandingZone(Transform root)
        {
            var basePos = OnGround(-45, -45);
            var zone = new GameObject("LandingZone").transform;
            zone.SetParent(root, false);
            zone.localPosition = basePos;

            if (_shipPrefab != null)
                PlaceSolid(_shipPrefab, zone, Vector3.zero, Quaternion.identity, "Ship");
            else
            {
                var ship = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ship.name = "Ship_Placeholder";
                ship.transform.SetParent(zone, false);
                ship.transform.localScale = new Vector3(10, 5, 14);
                ship.transform.localPosition = new Vector3(0, 2.2f, 0);
                Paint(ship, MetalCol);
            }

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(zone, false);
            spawn.transform.localPosition = new Vector3(6, 1.2f, 4);

            if (_teleporterPrefab != null)
                PlaceProp(_teleporterPrefab, zone, new Vector3(9, 0.1f, 6), Quaternion.identity, "TeleporterPad");
            else
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "TeleporterPad_Placeholder";
                pad.transform.SetParent(zone, false);
                pad.transform.localScale = new Vector3(3, 0.1f, 3);
                pad.transform.localPosition = new Vector3(9, 0.1f, 6);
                DestroyImmediate(pad.GetComponent<Collider>());
                Paint(pad, BuildingCol, BuildingCol * 0.8f);
            }
        }

        private void BuildFarmhouse(Transform root, string name, Vector3 pos, float yaw)
        {
            var house = new GameObject(name).transform;
            house.SetParent(root, false);
            house.localPosition = pos;
            house.localRotation = Quaternion.Euler(0, yaw, 0);

            if (_farmhousePrefab != null)
            {
                PlaceSolid(_farmhousePrefab, house, Vector3.zero, Quaternion.identity, "Model");
            }
            else
            {
                var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = "Dome";
                dome.transform.SetParent(house, false);
                dome.transform.localScale = new Vector3(12, 8, 12);
                dome.transform.localPosition = new Vector3(0, 2.6f, 0);
                Paint(dome, BuildingCol);
                AddBioLight(dome.transform, new Vector3(0, 0.15f, 0.42f));
            }

            // Interaction rig (always): invisible proximity trigger + networked door.
            var door = new GameObject("Door");
            door.transform.SetParent(house, false);
            door.transform.localPosition = new Vector3(0, 1.5f, 6.2f);

            if (_farmhousePrefab == null)
            {
                var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frame.name = "DoorFrame";
                frame.transform.SetParent(door.transform, false);
                frame.transform.localScale = new Vector3(2.4f, 3f, 0.4f);
                Paint(frame, AccentCol, AccentCol * 0.5f);
            }

            var proximity = door.AddComponent<BoxCollider>();
            proximity.isTrigger = true;
            proximity.size = new Vector3(4.5f, 3f, 4.5f);
            proximity.center = new Vector3(0, 0, 1f);

            door.AddComponent<NetworkObject>();
            door.AddComponent<EntranceDoor>();
        }

        private void BuildBarn(Transform root, Vector3 pos)
        {
            if (_barnPrefab != null) { PlaceSolid(_barnPrefab, root, pos, Quaternion.identity, "Barn"); return; }
            var b = Solid(root, "Barn", pos, new Vector3(14, 8, 10), BuildingCol);
            AddBioLight(b.transform, new Vector3(0, 0.1f, 0.5f));
        }

        private void BuildSilo(Transform root, Vector3 pos)
        {
            if (_siloPrefab != null) { PlaceSolid(_siloPrefab, root, pos, Quaternion.identity, "Silo"); return; }
            var s = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            s.name = "Silo";
            s.transform.SetParent(root, false);
            s.transform.localScale = new Vector3(4, 9, 4);
            s.transform.localPosition = pos + Vector3.up * (9f - 0.5f);
            Paint(s, BuildingCol);
        }

        private void BuildShed(Transform root, Vector3 pos)
        {
            if (_shedPrefab != null) { PlaceSolid(_shedPrefab, root, pos, Quaternion.identity, "Shed"); return; }
            Solid(root, "Shed", pos, new Vector3(7, 4, 6), BuildingCol);
        }

        private void BuildStable(Transform root, string name, Vector3 pos)
        {
            if (_stablePrefab != null) PlaceSolid(_stablePrefab, root, pos, Quaternion.identity, name);
            else Solid(root, name, pos, new Vector3(10, 5, 7), BuildingCol);

            var spawn = new GameObject(name + "_AnimalSpawn");
            spawn.transform.SetParent(root, false);
            spawn.transform.localPosition = OnGround(pos.x, pos.z + 6f) + Vector3.up * 0.5f;
        }

        private void BuildAcidLake(Transform root, Vector3 pos, float radius)
        {
            var lake = new GameObject("AcidLake").transform;
            lake.SetParent(root, false);
            lake.localPosition = pos + Vector3.down * 0.4f;

            if (_acidSurfacePrefab != null)
                PlaceProp(_acidSurfacePrefab, lake, new Vector3(0, 0.2f, 0), Quaternion.identity, "AcidSurface");
            else
            {
                var surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                surface.name = "AcidSurface";
                surface.transform.SetParent(lake, false);
                surface.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
                surface.transform.localPosition = new Vector3(0, 0.2f, 0);
                DestroyImmediate(surface.GetComponent<Collider>());
                Paint(surface, AcidCol, AcidCol * 2.4f);
            }

            // Real light bleed so the pool glows onto the surrounding hills.
            var glow = new GameObject("AcidGlow");
            glow.transform.SetParent(lake, false);
            glow.transform.localPosition = new Vector3(0, 2.5f, 0);
            var gl = glow.AddComponent<Light>();
            gl.type = LightType.Point;
            gl.color = AcidCol;
            gl.range = radius * 2f;
            gl.intensity = 2.4f;

            // Hazard trigger volume (slow + DoT) — always, regardless of visual.
            var trig = lake.gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = new Vector3(radius * 2f, 3f, radius * 2f);
            trig.center = new Vector3(0, 0.8f, 0);
            lake.gameObject.AddComponent<HazardCollider>();
        }

        private void BuildCornfield(Transform root, Vector3 pos, Vector3 size)
        {
            var field = new GameObject("Cornfield").transform;
            field.SetParent(root, false);
            field.localPosition = pos;

            // Foliage trigger volume (soft slow + rustle + sight block) — always.
            var trig = field.gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = size;
            trig.center = new Vector3(0, size.y * 0.5f, 0);
            field.gameObject.AddComponent<FoliageTrigger>();

            int cols = Mathf.Clamp((int)(size.x / 3f), 2, 12);
            int rows = Mathf.Clamp((int)(size.z / 3f), 2, 8);
            var stalks = new GameObject("Stalks").transform;
            stalks.SetParent(field, false);
            for (int x = 0; x < cols; x++)
            for (int z = 0; z < rows; z++)
            {
                float px = -size.x / 2f + (x + 0.5f) * (size.x / cols);
                float pz = -size.z / 2f + (z + 0.5f) * (size.z / rows);
                float groundY = SampleHeight(pos.x + px, pos.z + pz) - pos.y;

                if (_cornStalkPrefab != null)
                {
                    PlaceProp(_cornStalkPrefab, stalks, new Vector3(px, groundY, pz),
                        Quaternion.Euler(0, Random.Range(0, 360f), 0), "Stalk");
                }
                else
                {
                    float h = size.y * Random.Range(0.7f, 1f);
                    var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    s.transform.SetParent(stalks, false);
                    s.transform.localPosition = new Vector3(px, groundY + h / 2f, pz);
                    s.transform.localScale = new Vector3(0.3f, h, 0.3f);
                    DestroyImmediate(s.GetComponent<Collider>());
                    Paint(s, Vary(CornCol, 0.15f));
                }
            }
        }

        private void BuildRockCluster(Transform root, Vector3 pos, int count, float scale)
        {
            var cluster = new GameObject("Rocks").transform;
            cluster.SetParent(root, false);
            cluster.localPosition = pos;
            for (int i = 0; i < count; i++)
            {
                float ox = Random.Range(-scale, scale);
                float oz = Random.Range(-scale, scale);
                float groundY = SampleHeight(pos.x + ox, pos.z + oz) - pos.y;
                float s = Random.Range(scale * 0.6f, scale * 1.4f);
                var rot = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0, 360f), Random.Range(-12f, 12f));

                if (_rockPrefab != null)
                {
                    PlaceSolid(_rockPrefab, cluster, new Vector3(ox, groundY, oz), rot, "Rock");
                }
                else
                {
                    var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    r.transform.SetParent(cluster, false);
                    r.transform.localPosition = new Vector3(ox, groundY + s * 0.4f, oz);
                    r.transform.localScale = new Vector3(s, s * Random.Range(0.8f, 1.6f), s);
                    r.transform.localRotation = rot;
                    Paint(r, Vary(RockCol, 0.18f));
                }
            }
        }

        private void BuildTractor(Transform root, Vector3 pos)
        {
            if (_tractorPrefab != null)
            {
                PlaceSolid(_tractorPrefab, root, pos, Quaternion.Euler(0, 35f, 0), "AlienTractor");
                return;
            }

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
            FenceRun(root, new Vector3(-4, 0, -8), new Vector3(18, 0, -8), gapAt: 0.55f);
            FenceRun(root, new Vector3(18, 0, -8), new Vector3(18, 0, 6),  gapAt: -1f);
        }

        private void FenceRun(Transform root, Vector3 a, Vector3 b, float gapAt)
        {
            var run = new GameObject("Fence").transform;
            run.SetParent(root, false);
            int posts = 10;
            for (int i = 0; i <= posts; i++)
            {
                float f = i / (float)posts;
                if (gapAt >= 0f && Mathf.Abs(f - gapAt) < 0.12f) continue;
                Vector3 flat = Vector3.Lerp(a, b, f);
                float y = SampleHeight(flat.x, flat.z);

                if (_fencePostPrefab != null)
                {
                    PlaceSolid(_fencePostPrefab, run, new Vector3(flat.x, y, flat.z), Quaternion.identity, "Post");
                }
                else
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    p.transform.SetParent(run, false);
                    p.transform.localPosition = new Vector3(flat.x, y + 0.9f, flat.z);
                    p.transform.localScale = new Vector3(0.2f, 1.8f, 0.2f);
                    Paint(p, MetalCol);
                }
            }
        }

        private void BuildCrystals(Transform root)
        {
            Vector2[] spots =
            {
                new Vector2(-48, -30), new Vector2(-40, -38),
                new Vector2(30, 40), new Vector2(52, 20), new Vector2(-20, 52)
            };
            var group = new GameObject("Crystals").transform;
            group.SetParent(root, false);
            foreach (var spot in spots)
            {
                int shards = Random.Range(2, 5);
                for (int i = 0; i < shards; i++)
                {
                    float ox = Random.Range(-2f, 2f);
                    float oz = Random.Range(-2f, 2f);
                    float y = SampleHeight(spot.x + ox, spot.y + oz);
                    var rot = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0, 360f), Random.Range(-20f, 20f));

                    if (_crystalPrefab != null)
                    {
                        PlaceProp(_crystalPrefab, group, new Vector3(spot.x + ox, y, spot.y + oz), rot, "Crystal");
                    }
                    else
                    {
                        float h = Random.Range(2f, 5f);
                        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.SetParent(group, false);
                        c.transform.localPosition = new Vector3(spot.x + ox, y + h * 0.4f, spot.y + oz);
                        c.transform.localScale = new Vector3(0.6f, h, 0.6f);
                        c.transform.localRotation = rot;
                        DestroyImmediate(c.GetComponent<Collider>());
                        Paint(c, Vary(CrystalCol, 0.12f), CrystalCol * 1.8f);
                    }
                }

                var cg = new GameObject("CrystalGlow");
                cg.transform.SetParent(group, false);
                cg.transform.localPosition = new Vector3(spot.x, SampleHeight(spot.x, spot.y) + 1.5f, spot.y);
                var cl = cg.AddComponent<Light>();
                cl.type = LightType.Point;
                cl.color = CrystalCol;
                cl.range = 9f;
                cl.intensity = 1.5f;
            }
        }

        // =============================== prefab placement ====================================

        private GameObject PlaceSolid(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, string name)
        {
            var go = Instantiate(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            EnsureCollider(go);
            return go;
        }

        private GameObject PlaceProp(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, string name)
        {
            var go = Instantiate(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            StripColliders(go); // props must not block movement / capture
            return go;
        }

        private static void EnsureCollider(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>() != null) return;
            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        private static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                DestroyImmediate(c);
        }

        // =================================== helpers =========================================

        private GameObject Solid(Transform root, string name, Vector3 pos, Vector3 size, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localScale = size;
            go.transform.localPosition = pos + Vector3.up * (size.y * 0.5f - 0.5f);
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
            Paint(w, AccentCol, AccentCol * 2.6f);
        }

        private static void Paint(GameObject go, Color color, Color? emission = null)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;

            var toon = Shader.Find("AlienZoo/Toon");
            if (toon != null)
            {
                var tm = new Material(toon);
                tm.SetColor("_Color", color);
                if (emission.HasValue) tm.SetColor("_EmissionColor", emission.Value);
                r.sharedMaterial = tm;
                return;
            }

            var m = new Material(Shader.Find("Standard")) { color = color };
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            r.sharedMaterial = m;
        }

        private void PaintTerrain(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;

            var shader = Shader.Find("AlienZoo/TerrainGradient");
            if (shader == null) { Paint(go, GroundCol); return; }

            var m = new Material(shader);
            m.SetColor("_LowColor",  new Color(0.30f, 0.22f, 0.40f));
            m.SetColor("_HighColor", new Color(0.62f, 0.46f, 0.66f));
            m.SetFloat("_MinH", -_hillAmplitude);
            m.SetFloat("_MaxH",  _hillAmplitude);
            r.sharedMaterial = m;
        }

        private static Color Vary(Color c, float amt)
        {
            float f = 1f + Random.Range(-amt, amt);
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
        }

        private void ApplyAtmosphere()
        {
            var skyShader = Shader.Find("AlienZoo/GradientSkybox");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_TopColor",     new Color(0.18f, 0.80f, 0.78f));
                sky.SetColor("_HorizonColor", new Color(0.97f, 0.82f, 0.60f));
                sky.SetColor("_BottomColor",  new Color(0.28f, 0.18f, 0.34f));
                sky.SetFloat("_Exponent", 1.3f);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.40f, 0.62f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.55f, 0.42f);
            RenderSettings.ambientGroundColor  = new Color(0.26f, 0.20f, 0.34f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.55f, 0.48f, 0.52f);
            RenderSettings.fogDensity = 0.009f;

            var sun = RenderSettings.sun != null ? RenderSettings.sun : Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.color = new Color(1f, 0.80f, 0.58f);
                sun.transform.rotation = Quaternion.Euler(16f, -35f, 0f);
                sun.intensity = 1.15f;
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}
