using FishNet.Object;
using UnityEngine;

namespace AlienZoo.Data
{
    /// <summary>
    /// A purchasable piece of gear (trap, lure, weapon, revive kit...). Priced here for the shop.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "AlienZoo/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable, unique id used by shop purchase RPCs.")]
        public int Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("Shop")]
        public ItemCategory Category = ItemCategory.Utility;
        [Min(0)] public int Cost = 25;

        [Header("Spawning")]
        [Tooltip("Networked object delivered to the crew when purchased.")]
        public NetworkObject Prefab;
    }
}
