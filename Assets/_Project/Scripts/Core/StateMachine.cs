using System;
using System.Collections.Generic;

namespace AlienZoo.Core
{
    /// <summary>
    /// Minimal, allocation-free, enum-driven state machine.
    /// Used server-side by AnimalAI; reusable by any other system that wants a clean FSM.
    /// It does not tick anything itself — the owner ticks based on <see cref="Current"/>.
    /// </summary>
    public class StateMachine<TState> where TState : struct, Enum
    {
        /// <summary>Fired after a transition: (previous, next).</summary>
        public event Action<TState, TState> StateChanged;

        public TState Current { get; private set; }

        public StateMachine(TState initial)
        {
            Current = initial;
        }

        public void ChangeState(TState next)
        {
            if (EqualityComparer<TState>.Default.Equals(Current, next))
                return;

            TState prev = Current;
            Current = next;
            StateChanged?.Invoke(prev, next);
        }
    }
}
