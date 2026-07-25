using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using AlienZoo.Core;

namespace AlienZoo.Economy
{
    /// <summary>
    /// The single source of truth for the crew's SHARED wallet. Server-authoritative:
    /// only the server mutates <see cref="TeamMoney"/>, and all payouts / spends flow through here.
    ///
    /// Design rules baked in:
    ///  - Payouts are instant (called the moment a creature is teleported).
    ///  - Shop spends are REFUSED if unaffordable — buying can never bankrupt you.
    ///  - Penalties CAN push the balance below zero, which is a lose condition.
    /// </summary>
    public class EconomyManager : NetworkBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        /// <summary>Shared team balance. Synced to every client for HUD display.</summary>
        public readonly SyncVar<int> TeamMoney = new SyncVar<int>();

        /// <summary>Raised on the server when a penalty drives the balance below zero.</summary>
        public event Action OnBankrupt;

        public int Balance => TeamMoney.Value;

        private void Awake()
        {
            Instance = this;
            TeamMoney.OnChange += HandleMoneyChanged;
        }

        private void OnDestroy()
        {
            TeamMoney.OnChange -= HandleMoneyChanged;
            if (Instance == this) Instance = null;
        }

        private void HandleMoneyChanged(int prev, int next, bool asServer)
        {
            // Fires on server and every client — feed the shared UI event bus.
            GameEvents.RaiseMoneyChanged(next);
        }

        // ---------------- Server API ----------------

        [Server]
        public void ServerInitialize(int startingMoney)
        {
            TeamMoney.Value = startingMoney;
        }

        /// <summary>Instant payout. Called by TeleporterPad via GameManager on capture.</summary>
        [Server]
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            TeamMoney.Value += amount;
        }

        /// <summary>
        /// Attempt a shop purchase. Returns false (and changes nothing) if unaffordable,
        /// so a purchase is never a path to Game Over.
        /// </summary>
        [Server]
        public bool TrySpend(int amount)
        {
            if (amount < 0) return false;
            if (TeamMoney.Value < amount) return false;
            TeamMoney.Value -= amount;
            return true;
        }

        /// <summary>
        /// Apply a penalty that is allowed to go negative. If the balance drops below zero,
        /// the run is lost (fires <see cref="OnBankrupt"/>).
        /// </summary>
        [Server]
        public void ApplyPenalty(int amount)
        {
            if (amount <= 0) return;
            TeamMoney.Value -= amount;
            if (TeamMoney.Value < 0)
                OnBankrupt?.Invoke();
        }

        // ---------------- Client -> Server requests ----------------

        /// <summary>
        /// A client asks to buy something. The server validates funds and resolves delivery.
        /// RequireOwnership = false because any crew member can spend from the shared pot.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdRequestPurchase(int itemId, NetworkConnection buyer = null)
        {
            // TODO (Phase 3): resolve ItemDefinition from an item registry by itemId,
            //   if (!TrySpend(item.Cost)) { RpcPurchaseDenied(buyer); return; }
            //   DeliverySystem.Deliver(item, buyer);
        }
    }
}
