namespace AlienZoo.Data
{
    /// <summary>High-level session state, driven by the GameManager FSM and synced to clients.</summary>
    public enum GamePhase
    {
        Bootstrap,   // pre-network boot
        Lobby,       // players joining
        Hub,         // the zoo facility between runs
        InTransit,   // travelling to a planet
        DayActive,   // on-planet, capturing
        Returning,   // quota met, heading home
        GameOver     // bankrupt or team wipe
    }

    /// <summary>Determines which teleporter pad tier can capture a creature.</summary>
    public enum AnimalSize { Small, Medium, Large }

    /// <summary>Quota animals are pre-generated and pay big; nuisances respawn and pay little.</summary>
    public enum AnimalCategory { Quota, Nuisance }

    /// <summary>Server-authoritative AI states. Replicated to clients for visuals only.</summary>
    public enum AnimalState { Idle, Wander, Alert, Flee, Aggro, Subdued, Struggle, Captured }

    /// <summary>Coarse item classification used by the shop / delivery systems.</summary>
    public enum ItemCategory { Trap, Lure, Weapon, Revive, Utility }

    /// <summary>Why the run ended.</summary>
    public enum GameOverReason { Bankrupt, TeamWipe }
}
