using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using AlienZoo.Core;
using AlienZoo.Data;

namespace AlienZoo.Animals
{
    /// <summary>
    /// Base creature brain. AI is simulated ONLY on the server; the resulting <see cref="State"/> is
    /// replicated to clients purely for animation / audio / VFX. Per-species behaviour is added by
    /// subclassing and overriding the Tick* / On* hooks — the state graph itself stays shared.
    ///
    /// State graph:
    ///   Idle/Wander -> Alert -> [Flee | Aggro] -> Subdued -> Struggle(on pad) -> Captured
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class AnimalAI : NetworkBehaviour
    {
        [SerializeField] private AnimalDefinition _definition;

        /// <summary>Replicated visual state. Set by the server FSM only.</summary>
        public readonly SyncVar<AnimalState> State = new SyncVar<AnimalState>();

        public AnimalDefinition Definition => _definition;
        public AnimalCategory Category { get; private set; }
        public AnimalSize Size => _definition.Size;
        public bool IsSubdued => _subdueProgress >= _definition.SubdueThreshold;

        // ---- Server-only runtime ----
        private StateMachine<AnimalState> _fsm;
        private float _health;
        private float _subdueProgress;

        /// <summary>
        /// Called by a spawner on the server BEFORE NetworkObject.Spawn, so category/definition
        /// replicate with the object. Safe to call again to reconfigure before spawn.
        /// </summary>
        public void Initialize(AnimalDefinition definition, AnimalCategory category)
        {
            _definition = definition;
            Category = category;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _health = _definition.MaxHealth;
            _subdueProgress = 0f;

            _fsm = new StateMachine<AnimalState>(AnimalState.Idle);
            _fsm.StateChanged += (from, to) => State.Value = to;
            State.Value = AnimalState.Idle;
        }

        private void Update()
        {
            if (!base.IsServerInitialized) return; // simulate on the server only
            TickServer(Time.deltaTime);
        }

        /// <summary>Dispatch to the active state's tick. Override to change the whole loop.</summary>
        protected virtual void TickServer(float dt)
        {
            switch (_fsm.Current)
            {
                case AnimalState.Idle:     TickIdle(dt);     break;
                case AnimalState.Wander:   TickWander(dt);   break;
                case AnimalState.Alert:    TickAlert(dt);    break;
                case AnimalState.Flee:     TickFlee(dt);     break;
                case AnimalState.Aggro:    TickAggro(dt);    break;
                case AnimalState.Subdued:  TickSubdued(dt);  break;
                case AnimalState.Struggle: TickStruggle(dt); break;
            }
        }

        // --- Per-state hooks. Override per species for unique behaviour. ---
        protected virtual void TickIdle(float dt) { }
        protected virtual void TickWander(float dt) { }
        protected virtual void TickAlert(float dt) { }
        protected virtual void TickFlee(float dt) { }
        protected virtual void TickAggro(float dt) { }
        protected virtual void TickSubdued(float dt) { }
        protected virtual void TickStruggle(float dt) { }

        // ---------------- Interaction API (server) ----------------

        /// <summary>Weapons/traps call this on the server to hurt the creature.</summary>
        [Server]
        public void ApplyDamage(float amount)
        {
            _health -= amount;
            if (_health <= 0f) { Die(); return; }

            // Getting hit while calm provokes it.
            if (_fsm.Current == AnimalState.Idle || _fsm.Current == AnimalState.Wander)
                _fsm.ChangeState(AnimalState.Aggro);
        }

        /// <summary>Traps/lures build subdue progress; once past the threshold it can be teleported.</summary>
        [Server]
        public void ApplySubdue(float amount)
        {
            _subdueProgress = Mathf.Min(_subdueProgress + amount, _definition.SubdueThreshold);
            if (IsSubdued && _fsm.Current != AnimalState.Subdued)
                _fsm.ChangeState(AnimalState.Subdued);
        }

        /// <summary>Server-side state override, used by the TeleporterPad (Struggle / Captured).</summary>
        [Server]
        public void SetState(AnimalState next) => _fsm.ChangeState(next);

        [Server]
        private void Die()
        {
            // TODO (Phase 3): loot drop + death SFX/VFX before removal.
            base.NetworkObject.Despawn();
        }
    }
}
