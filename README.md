# ALIEN ZOO

A co-op sci-fi comedy-horror creature-extraction game. Goofy but scary — think *Lethal Company* meets a robotic zoo that decided you'd make better contractors than exhibits.

## Tech Stack

| Layer | Choice |
|---|---|
| Engine | **Unity 6 LTS** |
| Networking | **FishNet** (v4+), server-authoritative host-client |
| Transport | **Steam** (FishySteamworks / FishyFacepunch) + local transport for editor testing |
| Topology | Player-hosted listen server (no dedicated servers) |
| Players | 2–4 (architected to scale to ~8) |
| Wallet | Single **shared** team wallet |

## First-time setup (do this before the scripts compile)

1. Create a Unity 6 LTS project at this folder (or copy `Assets/` into an existing one).
2. **Import FishNet v4+** (Asset Store or GitHub). This is required — `AlienZoo.asmdef`
   references `FishNet.Runtime`, so the code won't compile until FishNet is present.
3. Add a **NetworkManager** to your bootstrap scene (FishNet → create default NetworkManager).
4. Create a **"Systems" GameObject** with a `NetworkObject` and attach:
   `GameManager`, `EconomyManager`, `SpawnerSystem` — then wire their inspector references
   (GameManager → Economy + Spawner; SpawnerSystem → QuotaSpawner + NuisanceSpawner).
5. Register your animal/item prefabs in the NetworkManager's Spawnable Prefabs list.
6. (Later) Add the Steam transport package when you're ready to test over the internet.

## Architecture at a glance

```
GameManager (FSM, quota gate, win/lose)  ──drives──►  SpawnerSystem
      │                                                    ├── QuotaSpawner   (deterministic, no respawn)
      │                                                    └── NuisanceSpawner (continuous respawn)
      ├──reads/writes──►  EconomyManager (shared wallet, instant payout, penalties)
      ▲
TeleporterPad ──on capture──► GameManager.RegisterCapture() ──► payout + quota tick
AnimalAI (server FSM, synced visual state)
```

**Golden rule:** the host is authoritative. Clients *request* via `ServerRpc`; the server validates,
mutates state, and replicates via `SyncVar` / `ObserversRpc`. UI/audio listen to `Core/GameEvents`
so they never hold hard references to networked managers.

## Folder layout

```
Assets/_Project/
├── Art, Audio, Prefabs, ScriptableObjects, Scenes, Settings   (content)
└── Scripts/
    ├── Core/       StateMachine<T>, GameEvents
    ├── Data/       ScriptableObject definitions + enums
    ├── GameState/  GameManager
    ├── Economy/    EconomyManager
    ├── Spawning/   SpawnerSystem, QuotaSpawner, NuisanceSpawner
    ├── Animals/    AnimalAI (base creature brain)
    ├── Capture/    TeleporterPad
    ├── Networking, Player, Gear, UI, Utils   (Phase 3+)
```

> **Note on assembly definitions:** the foundation ships with a **single** `AlienZoo.asmdef` rather
> than one-per-module. The module dependency graph has natural cycles (Animals ↔ Capture,
> Spawning ↔ Animals) that would deadlock separate asmdefs. We'll split into per-module assemblies
> once the boundaries settle and we can break the cycles with interfaces in `Core`.

## What's implemented (Phase 2)

- ✅ `GameManager` — full: phase FSM, quota manifest + mandatory gate, both lose conditions.
- ✅ `EconomyManager` — full: shared wallet, instant payout, affordable-only spends, penalties → bankruptcy.
- ✅ `SpawnerSystem` / `QuotaSpawner` / `NuisanceSpawner` — dual-spawn logic, deterministic quota, capped respawns.
- ✅ `AnimalAI` — base FSM with per-state override hooks, damage/subdue API, synced visual state.
- ✅ `TeleporterPad` — size-tiered, subdue-gated capture → payout.

## Next (Phase 3 candidates)

Player controller + ghost/revive · Interaction/grab system · Gear (traps, lures, weapons) ·
Shop + drop-pod delivery · Item registry for purchases · NavMesh movement in AnimalAI · Lobby/session flow.
