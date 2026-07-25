using FishNet.Object;
using UnityEngine;

namespace AlienZoo.Data
{
    /// <summary>
    /// Data-only description of a species. Balancing lives here so tuning never needs a recompile.
    /// One ScriptableObject asset per species under Assets/_Project/ScriptableObjects/Animals.
    /// </summary>
    [CreateAssetMenu(fileName = "Animal", menuName = "AlienZoo/Animal Definition")]
    public class AnimalDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable, unique id. Used to reference this species across the network (RPCs, save data).")]
        public int Id;
        public string DisplayName;
        [Tooltip("Prefab must carry a NetworkObject + an AnimalAI (or subclass).")]
        public NetworkObject Prefab;

        [Header("Classification")]
        public AnimalSize Size = AnimalSize.Small;
        public AnimalCategory Category = AnimalCategory.Nuisance;

        [Header("Economy")]
        [Tooltip("Instant payout on capture. Quota ~100, Nuisance ~10.")]
        public int BasePayout = 10;

        [Header("Combat / Capture")]
        public float MaxHealth = 100f;
        public float MoveSpeed = 3.5f;
        [Tooltip("Subdue progress required before a teleporter can lock this creature in.")]
        public float SubdueThreshold = 100f;
        [Range(0f, 1f)]
        [Tooltip("How hard it fights back. 0 = timid, 1 = relentless.")]
        public float Aggression = 0.5f;
        [Tooltip("Radius at which the creature notices players and reacts.")]
        public float SenseRadius = 12f;
    }
}
