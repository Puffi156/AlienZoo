using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using AlienZoo.GameState;

namespace AlienZoo.Player
{
    /// <summary>
    /// Minimal server-authoritative health. Reports alive/dead to the GameManager so the
    /// team-wipe lose condition works. Ghost visuals + revive UX come in a later pass.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;

        /// <summary>Synced so any client can grey out a dead teammate, show ghost, etc.</summary>
        public readonly SyncVar<bool> IsAlive = new SyncVar<bool>();

        private float _health;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _health = _maxHealth;
            IsAlive.Value = true;
            GameManager.Instance?.ReportPlayerAlive(base.OwnerId, true);
        }

        [Server]
        public void ApplyDamage(float amount)
        {
            if (!IsAlive.Value || amount <= 0f) return;
            _health -= amount;
            if (_health <= 0f) Die();
        }

        [Server]
        private void Die()
        {
            IsAlive.Value = false;
            GameManager.Instance?.ReportPlayerAlive(base.OwnerId, false);
            // TODO: enter ghost mode — disable collision/controller, swap to ghost visuals.
        }

        [Server]
        public void Revive()
        {
            _health = _maxHealth;
            IsAlive.Value = true;
            GameManager.Instance?.ReportPlayerAlive(base.OwnerId, true);
        }
    }
}
