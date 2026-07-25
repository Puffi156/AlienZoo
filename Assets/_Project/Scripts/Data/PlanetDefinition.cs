using System;
using UnityEngine;

namespace AlienZoo.Data
{
    /// <summary>
    /// Everything a planet needs: its scene, the fixed quota manifest, and the nuisance respawn rules.
    /// The dual-spawn design lives entirely in this data — one asset per planet.
    /// </summary>
    [CreateAssetMenu(fileName = "Planet", menuName = "AlienZoo/Planet Definition")]
    public class PlanetDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName;
        [Tooltip("Scene loaded when the crew lands here.")]
        public string SceneName;

        [Header("Quota — pre-generated at day start, no respawn")]
        public QuotaEntry[] Quota;

        [Header("Nuisance — continuous background respawn")]
        public NuisanceSettings Nuisance;

        [Serializable]
        public struct QuotaEntry
        {
            public AnimalDefinition Animal;
            [Min(1)] public int Count;
        }

        [Serializable]
        public struct NuisanceSettings
        {
            [Tooltip("Species eligible to respawn as nuisances on this planet.")]
            public AnimalDefinition[] Pool;
            [Min(0f)]
            [Tooltip("Seconds between spawn attempts.")]
            public float SpawnInterval;
            [Min(0)]
            [Tooltip("Maximum live nuisances allowed at once.")]
            public int MaxPopulation;
        }
    }
}
