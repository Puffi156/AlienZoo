using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;
using AlienZoo.Animals;
using AlienZoo.Data;

namespace AlienZoo.Spawning
{
    /// <summary>
    /// Continuous background spawner for hostile, low-value nuisance animals. Respawns forever up to
    /// a live population cap. This is the pressure valve that powers the "Walk of Shame" grind:
    /// stranded players can always farm these to claw back enough money for revives / traps.
    /// </summary>
    public class NuisanceSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] _spawnPoints;

        private PlanetDefinition _planet;
        private System.Random _rng;
        private Coroutine _loop;
        private readonly List<AnimalAI> _liveNuisances = new();

        /// <summary>Server-only. Starts the respawn loop for this planet.</summary>
        public void BeginSpawning(PlanetDefinition planet, int seed)
        {
            if (!InstanceFinder.IsServerStarted) return;

            _planet = planet;
            // Offset the seed so nuisance placement is decorrelated from quota placement.
            _rng = new System.Random(seed ^ unchecked((int)0x5f3759df));
            StopSpawning();
            _loop = StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        private IEnumerator SpawnLoop()
        {
            var s = _planet.Nuisance;

            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(0.1f, s.SpawnInterval));

                PruneDead();

                bool canSpawn =
                    _liveNuisances.Count < s.MaxPopulation &&
                    s.Pool != null && s.Pool.Length > 0 &&
                    _spawnPoints != null && _spawnPoints.Length > 0;

                if (!canSpawn) continue;

                AnimalDefinition def = s.Pool[_rng.Next(s.Pool.Length)];
                Transform point = _spawnPoints[_rng.Next(_spawnPoints.Length)];
                SpawnNuisance(def, point);
            }
        }

        private void PruneDead() => _liveNuisances.RemoveAll(a => a == null);

        private void SpawnNuisance(AnimalDefinition def, Transform point)
        {
            NetworkObject nob = Instantiate(def.Prefab, point.position, point.rotation);

            var ai = nob.GetComponent<AnimalAI>();
            if (ai != null) ai.Initialize(def, AnimalCategory.Nuisance);

            InstanceFinder.ServerManager.Spawn(nob);
            if (ai != null) _liveNuisances.Add(ai);
        }
    }
}
