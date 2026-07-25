using UnityEngine;
using AlienZoo.Data;

namespace AlienZoo.Spawning
{
    /// <summary>
    /// Thin coordinator over the two spawners. GameManager (server) calls into this; it fans out
    /// to the deterministic quota spawner and the continuous nuisance spawner.
    /// </summary>
    public class SpawnerSystem : MonoBehaviour
    {
        [SerializeField] private QuotaSpawner _quotaSpawner;
        [SerializeField] private NuisanceSpawner _nuisanceSpawner;

        /// <summary>Server-only. Spawn the fixed quota, then start the nuisance loop.</summary>
        public void BeginDay(PlanetDefinition planet, int seed)
        {
            _quotaSpawner.SpawnQuota(planet, seed);
            _nuisanceSpawner.BeginSpawning(planet, seed);
        }

        /// <summary>Server-only. Stop background respawns when the crew leaves.</summary>
        public void EndDay()
        {
            _nuisanceSpawner.StopSpawning();
        }
    }
}
