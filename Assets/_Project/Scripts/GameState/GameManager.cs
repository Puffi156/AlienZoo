using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using AlienZoo.Core;
using AlienZoo.Data;
using AlienZoo.Economy;
using AlienZoo.Spawning;

namespace AlienZoo.GameState
{
    /// <summary>
    /// The session brain. Server-authoritative FSM that:
    ///  - drives the <see cref="GamePhase"/> lifecycle,
    ///  - owns the mandatory quota and gates the planet exit until it's met,
    ///  - evaluates the two lose conditions (bankrupt / team wipe).
    /// Lives on a networked "Systems" object in the scene alongside EconomyManager + SpawnerSystem.
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private EconomyManager _economy;
        [SerializeField] private SpawnerSystem _spawner;

        [Header("Config")]
        [SerializeField] private int _startingMoney = 100;

        /// <summary>Current phase, synced so every client can drive UI / music / lighting.</summary>
        public readonly SyncVar<GamePhase> Phase = new SyncVar<GamePhase>();

        /// <summary>True once the current planet's quota is fully satisfied (exit unlocked).</summary>
        public readonly SyncVar<bool> QuotaComplete = new SyncVar<bool>();

        public event Action<GamePhase> OnPhaseChanged;

        // ---- Server-only state ----
        private PlanetDefinition _currentPlanet;
        private readonly Dictionary<AnimalDefinition, int> _remainingQuota = new();
        private int _totalQuotaRemaining;
        private readonly HashSet<int> _alivePlayers = new();

        private void Awake()
        {
            Instance = this;
            Phase.OnChange += HandlePhaseChanged;
        }

        private void OnDestroy()
        {
            Phase.OnChange -= HandlePhaseChanged;
            if (Instance == this) Instance = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_economy != null) _economy.OnBankrupt += HandleBankrupt;

            _economy?.ServerInitialize(_startingMoney);
            SetPhase(GamePhase.Hub);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (_economy != null) _economy.OnBankrupt -= HandleBankrupt;
        }

        private void HandlePhaseChanged(GamePhase prev, GamePhase next, bool asServer)
        {
            OnPhaseChanged?.Invoke(next);
        }

        // ---------------- Day lifecycle (server) ----------------

        /// <summary>Land on a planet: build the quota manifest and kick off both spawners.</summary>
        [Server]
        public void BeginDay(PlanetDefinition planet, int seed)
        {
            _currentPlanet = planet;
            _remainingQuota.Clear();
            _totalQuotaRemaining = 0;

            foreach (var entry in planet.Quota)
            {
                if (entry.Animal == null || entry.Count <= 0) continue;
                _remainingQuota[entry.Animal] = entry.Count;
                _totalQuotaRemaining += entry.Count;
            }

            QuotaComplete.Value = _totalQuotaRemaining == 0;
            SetPhase(GamePhase.DayActive);
            _spawner.BeginDay(planet, seed);
        }

        [Server]
        public void EndDay()
        {
            _spawner.EndDay();
            SetPhase(GamePhase.Returning);
        }

        /// <summary>
        /// Called by a TeleporterPad on the server when a creature is successfully captured.
        /// Pays out instantly, then ticks quota progress if it was a quota species.
        /// </summary>
        [Server]
        public void RegisterCapture(AnimalDefinition animal)
        {
            if (animal == null) return;

            _economy.AddMoney(animal.BasePayout);

            if (animal.Category != AnimalCategory.Quota) return;
            if (!_remainingQuota.TryGetValue(animal, out int remaining) || remaining <= 0) return;

            remaining--;
            _remainingQuota[animal] = remaining;
            _totalQuotaRemaining--;
            RpcQuotaUpdated(animal.Id, remaining);

            if (_totalQuotaRemaining <= 0)
            {
                QuotaComplete.Value = true;
                RpcQuotaComplete();
            }
        }

        /// <summary>Players request to leave. The server only allows it once quota is met.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdRequestLeavePlanet(NetworkConnection conn = null)
        {
            if (Phase.Value != GamePhase.DayActive) return;
            if (!QuotaComplete.Value) return; // the mandatory quota gate
            EndDay();
        }

        // ---------------- Lose conditions ----------------

        /// <summary>Player controllers report life/death here so we can detect a full team wipe.</summary>
        [Server]
        public void ReportPlayerAlive(int clientId, bool alive)
        {
            if (alive) _alivePlayers.Add(clientId);
            else _alivePlayers.Remove(clientId);

            if (_alivePlayers.Count == 0 && Phase.Value == GamePhase.DayActive)
                TriggerGameOver(GameOverReason.TeamWipe);
        }

        private void HandleBankrupt() => TriggerGameOver(GameOverReason.Bankrupt);

        [Server]
        private void TriggerGameOver(GameOverReason reason)
        {
            if (Phase.Value == GamePhase.GameOver) return;
            SetPhase(GamePhase.GameOver);
            RpcGameOver(reason);
        }

        [Server]
        private void SetPhase(GamePhase phase) => Phase.Value = phase;

        // ---------------- Client notifications ----------------

        [ObserversRpc]
        private void RpcQuotaUpdated(int animalId, int remaining) => GameEvents.RaiseQuotaUpdated(animalId, remaining);

        [ObserversRpc]
        private void RpcQuotaComplete() => GameEvents.RaiseQuotaComplete();

        [ObserversRpc]
        private void RpcGameOver(GameOverReason reason) => GameEvents.RaiseGameOver(reason);
    }
}
