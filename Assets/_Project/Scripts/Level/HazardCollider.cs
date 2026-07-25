using System.Collections.Generic;
using FishNet;
using UnityEngine;
using AlienZoo.Player;

namespace AlienZoo.Level
{
    /// <summary>
    /// Acid Lake hazard. A trigger volume that does NOT block movement but:
    ///   - slows any player wading through it (applied locally on that player's owner client), and
    ///   - deals damage-over-time (server-authoritative).
    /// Attach to a GameObject with a trigger Collider. Non-networked scene component.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HazardCollider : MonoBehaviour
    {
        [Header("Slow")]
        [Range(0f, 1f)]
        [Tooltip("Movement speed multiplier while inside (0.45 = 55% slower).")]
        [SerializeField] private float _speedMultiplier = 0.45f;

        [Header("Damage Over Time")]
        [SerializeField] private float _damagePerTick = 5f;
        [SerializeField] private float _tickInterval = 0.5f;

        private readonly HashSet<PlayerHealth> _damageTargets = new();
        private float _tickTimer;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;

            // Movement slow is only meaningful on the machine that OWNS this player.
            if (pc.IsOwner) pc.SetSpeedModifier(this, _speedMultiplier);

            // Damage is decided on the server only.
            if (InstanceFinder.IsServerStarted)
            {
                var hp = other.GetComponentInParent<PlayerHealth>();
                if (hp != null) _damageTargets.Add(hp);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;

            if (pc.IsOwner) pc.ClearSpeedModifier(this);

            if (InstanceFinder.IsServerStarted)
            {
                var hp = other.GetComponentInParent<PlayerHealth>();
                if (hp != null) _damageTargets.Remove(hp);
            }
        }

        private void Update()
        {
            if (!InstanceFinder.IsServerStarted || _damageTargets.Count == 0) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < _tickInterval) return;
            _tickTimer = 0f;

            _damageTargets.RemoveWhere(hp => hp == null); // prune despawned players
            foreach (var hp in _damageTargets)
                hp.ApplyDamage(_damagePerTick);
        }
    }
}
