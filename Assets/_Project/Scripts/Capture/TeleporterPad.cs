using FishNet.Object;
using UnityEngine;
using AlienZoo.Animals;
using AlienZoo.Data;
using AlienZoo.GameState;

namespace AlienZoo.Capture
{
    /// <summary>
    /// A size-tiered capture pad. When a sufficiently-subdued creature of a fitting size sits on the
    /// pad long enough, it teleports away: instant payout + quota progress via the GameManager.
    /// All logic is server-authoritative; clients only receive the teleport FX.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TeleporterPad : NetworkBehaviour
    {
        [Header("Capacity")]
        [Tooltip("Largest creature size this pad can teleport.")]
        [SerializeField] private AnimalSize _maxSize = AnimalSize.Small;

        [Header("Sequence")]
        [Tooltip("Seconds the creature must stay subdued on the pad before it teleports.")]
        [SerializeField] private float _teleportChargeTime = 1.5f;

        private AnimalAI _occupant;
        private float _charge;

        private void OnTriggerEnter(Collider other)
        {
            if (!base.IsServerInitialized || _occupant != null) return;

            var ai = other.GetComponentInParent<AnimalAI>();
            if (ai != null && FitsPad(ai))
                _occupant = ai;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!base.IsServerInitialized) return;

            var ai = other.GetComponentInParent<AnimalAI>();
            if (ai != null && ai == _occupant)
                ClearOccupant();
        }

        private void Update()
        {
            if (!base.IsServerInitialized || _occupant == null) return;

            // It must stay subdued to keep charging; otherwise it thrashes and resets progress.
            if (!_occupant.IsSubdued)
            {
                _occupant.SetState(AnimalState.Struggle);
                _charge = 0f;
                return;
            }

            _charge += Time.deltaTime;
            if (_charge >= _teleportChargeTime)
                Teleport(_occupant);
        }

        private bool FitsPad(AnimalAI ai) => (int)ai.Size <= (int)_maxSize;

        [Server]
        private void Teleport(AnimalAI ai)
        {
            AnimalDefinition def = ai.Definition;
            ai.SetState(AnimalState.Captured);

            // Instant payout + quota tick, then remove the creature from the world.
            GameManager.Instance?.RegisterCapture(def);
            RpcPlayTeleportFx();
            ai.NetworkObject.Despawn();

            ClearOccupant();
        }

        private void ClearOccupant()
        {
            _occupant = null;
            _charge = 0f;
        }

        [ObserversRpc]
        private void RpcPlayTeleportFx()
        {
            // TODO (Phase 3): beam VFX + SFX flourish on all clients.
        }
    }
}
