using FishNet;
using FishNet.Object;
using UnityEngine;
using AlienZoo.Animals;
using AlienZoo.Data;

namespace AlienZoo.Spawning
{
    /// <summary>
    /// One-shot, DETERMINISTIC spawner for quota animals. Given the same seed it always produces
    /// the same layout (great for co-op consistency and reproducing bugs). These never respawn.
    /// </summary>
    public class QuotaSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] _spawnPoints;

        /// <summary>Server-only. Spawns every quota entry for the planet.</summary>
        public void SpawnQuota(PlanetDefinition planet, int seed)
        {
            if (!InstanceFinder.IsServerStarted) return;
            if (planet.Quota == null) return;

            var rng = new System.Random(seed);

            foreach (var entry in planet.Quota)
            {
                if (entry.Animal == null) continue;
                for (int i = 0; i < entry.Count; i++)
                    SpawnAnimal(entry.Animal, PickSpawnPoint(rng), AnimalCategory.Quota);
            }
        }

        private Transform PickSpawnPoint(System.Random rng)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0) return transform;
            return _spawnPoints[rng.Next(_spawnPoints.Length)];
        }

        private void SpawnAnimal(AnimalDefinition def, Transform point, AnimalCategory category)
        {
            NetworkObject nob = Instantiate(def.Prefab, point.position, point.rotation);

            // Initialize BEFORE Spawn so the starting values replicate with the object.
            var ai = nob.GetComponent<AnimalAI>();
            if (ai != null) ai.Initialize(def, category);

            InstanceFinder.ServerManager.Spawn(nob);
        }
    }
}
