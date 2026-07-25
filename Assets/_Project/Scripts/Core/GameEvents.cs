using System;
using AlienZoo.Data;

namespace AlienZoo.Core
{
    /// <summary>
    /// A static, client-side event hub. UI, audio and VFX subscribe here instead of holding hard
    /// references to networked managers. The managers raise these in their SyncVar OnChange / RPC
    /// handlers, so everything downstream stays decoupled and network-agnostic.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Team wallet changed. Payload: new balance.</summary>
        public static event Action<int> MoneyChanged;

        /// <summary>A quota species was captured. Payload: (animalId, remaining count).</summary>
        public static event Action<int, int> QuotaUpdated;

        /// <summary>All quota satisfied — the exit is now unlocked.</summary>
        public static event Action QuotaComplete;

        /// <summary>The run ended. Payload: why.</summary>
        public static event Action<GameOverReason> GameOver;

        public static void RaiseMoneyChanged(int balance) => MoneyChanged?.Invoke(balance);
        public static void RaiseQuotaUpdated(int animalId, int remaining) => QuotaUpdated?.Invoke(animalId, remaining);
        public static void RaiseQuotaComplete() => QuotaComplete?.Invoke();
        public static void RaiseGameOver(GameOverReason reason) => GameOver?.Invoke(reason);
    }
}
