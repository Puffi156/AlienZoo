# Minimal Playtest Guide

Goal: get from zero to "two players running around a shared world" as fast as possible, then
(optionally) prove the capture → payout → quota loop. Do **Tier 1 first** — that's the real minimal
milestone. Tier 2 is a bonus once movement works.

> Prereq: FishNet v4+ is imported and the project compiles. Uses **legacy Input**, so set
> **Edit → Project Settings → Player → Active Input Handling = Both** (or *Input Manager (Old)*).

---

## Tier 1 — Movement playtest (start here)

### 1. Network scaffolding
- Hierarchy → right-click → **Fish-Networking → NetworkManager**. This drops in a preconfigured
  NetworkManager with the **Tugboat** transport (localhost:7770) — perfect for local testing, no Steam needed yet.
- Create an empty GameObject `_Dev` → add **`DevNetworkStarter`**.
- Create an empty GameObject `PlayerSpawner` → add **`PlayerSpawner`**.

### 2. Ground
- Hierarchy → **3D Object → Plane** (scale it up ~x5). It has a collider by default — that's all we need.

### 3. Player prefab
Build this hierarchy, then drag it into `Assets/_Project/Prefabs/Players/` and **delete it from the scene**:

```
Player                (CharacterController, NetworkObject, NetworkTransform,
 │                     PlayerController, PlayerHealth, PlayerInteractor)
 ├── Body              (Capsule mesh — just a visual; remove its collider)
 └── CameraPivot       (empty, position ~ y=1.6)
      └── Camera       (Camera + AudioListener)
```
Wiring on the prefab:
- **CharacterController**: Height ~2, Center Y ~1.
- **NetworkTransform**: tick **Client Authoritative** (owner drives movement) and sync Position + Rotation.
- **PlayerController** → assign *Camera Pivot* = `CameraPivot`.
- **PlayerInteractor** → assign *Aim Source* = `Camera`.
- FishNet auto-registers NetworkObject prefabs, so no manual prefab list step.

### 4. Hook up the spawner
- Select `PlayerSpawner` → set **Player Prefab** = your Player prefab.
- (Optional) add a few empty GameObjects as spawn points and assign them to **Spawn Points**.

### 5. Kill the duplicate listener
- Delete the scene's default **Main Camera** (or at least remove its AudioListener). Each player brings its own.

### 6. Play
- Press **Play** → click **Host (Server + Client)** in the on-screen panel → you spawn and can walk
  (WASD, mouse look, Space to jump, Esc to free the cursor).
- **Second instance** to see networking: either
  - **Window → Multiplayer → Multiplayer Play Mode** (Unity 6) and enable a virtual player, **or**
  - Build the project, run the build, click **Client → 127.0.0.1** while the editor hosts.
- ✅ Success = both players see each other move in real time.

---

## Tier 2 — Prove the capture loop (optional next step)

You need one animal, one planet, and one pad.

### 1. Animal prefab + definition
- Animal prefab: a Cube/Capsule with a **collider**, a **NetworkObject**, and **`AnimalAI`**.
  Save to `Prefabs/Animals/`.
- Create a definition: right-click in `ScriptableObjects/Animals/` → **Create → AlienZoo → Animal Definition**.
  Set `Id` (e.g. 1), `Category = Quota`, `BasePayout = 100`, `SubdueThreshold = 100`, `Size = Small`,
  and **Prefab** = the animal prefab. (On the prefab's `AnimalAI`, leave `_definition` empty — the spawner injects it.)

### 2. Planet definition
- `ScriptableObjects/Planets/` → **Create → AlienZoo → Planet Definition**.
- Add one **Quota** entry: Animal = your definition, Count = 1.

### 3. Teleporter pad
- A GameObject with a **BoxCollider set to Is Trigger**, a **NetworkObject**, and **`TeleporterPad`**
  (`Max Size = Small`). Place it on the ground. Save to `Prefabs/Environment/` if you like.

### 4. Systems object
- Empty `Systems` GameObject → add **NetworkObject**, **GameManager**, **EconomyManager**,
  **SpawnerSystem**, **QuotaSpawner**, **NuisanceSpawner**.
- Wire: GameManager → Economy + Spawner; SpawnerSystem → QuotaSpawner + NuisanceSpawner.
- On **QuotaSpawner**, set a spawn point **right on top of the teleporter pad** (so the test animal
  lands inside the trigger — we don't have AI movement yet).
- On **`DevNetworkStarter`**, set **Test Planet** = your planet definition.

### 5. Play the loop
1. Press Play → **Host**.
2. Click **Begin Day (test)** → the quota animal spawns on the pad.
3. Look at it and **hold E** to subdue it. When subdue passes the threshold, the pad charges and teleports it.
4. Watch the panel: **Money jumps to $100** and **Quota complete → True**. 🎉

That's the entire core loop proven end-to-end.

---

## Troubleshooting
- **Compile errors about `FishNet.Runtime`** → FishNet isn't imported yet.
- **Can't move / no input** → Active Input Handling isn't set to Both/Old.
- **Two AudioListeners warning** → remove the scene's default Main Camera.
- **Client can't connect** → confirm the host started first and both use the Tugboat transport / same port.
- **Player falls through floor** → the Plane needs a collider and the Player a CharacterController.
