# Moon 01 — "The Alien Farm" · Art & Build Bible

How to take the greybox `MoonLevelBuilder` produces and dress it into the look of the concept art
(vibrant, hand-inked, scary-goofy alien farm at dusk).

## Palette (sampled from the concept art)

| Role | Hex | Use |
|---|---|---|
| Ground rock (mauve) | `#7A5C8E` | terrain base |
| Ground shadow | `#4E3A63` | crevices, ambient occlusion |
| Ground highlight | `#9B7BB0` | cracked rock edges |
| Dome body (teal) | `#6BA1AE` | farmhouses, barn, silo, stables |
| Bio-light (orange) | `#E08A3C` | windows, door glows, panel accents — **emissive** |
| Bio-light (teal) | `#37C9B0` | secondary panel glow — **emissive** |
| Acid green | `#8FE33F` core `#B6FF3A` | lake surface — **emissive**, animated |
| Acid gas | `#A6F55E` @ ~30% alpha | rising particle fog |
| Alien crop | `#5CA04A` / `#3E7A4C` | cornfield stalks |
| Rock formations | `#4E3A63` | boulders, ridge |
| Crystal (purple) | `#C56BE0` / `#8E4FD0` | crystal shards — **emissive + translucent** |
| Rust metal | `#73523F` | tractor, ship hull, fences |
| Sky top | `#2FD3C6` → horizon `#F6D9A6` | gradient skybox |
| Aurora | `#B06CC8` | sky ribbon |
| Twin moons | `#EAF6F2` | pale crescents |

## Atmosphere (the builder already sets a first pass)
- **Fog:** exponential², purple haze `#4C526B`, low density — sells depth + dread.
- **Ambient:** flat purple `#615778`.
- **Sun:** warm peach `#FFC78C`, low angle (~18°) for long creepy shadows.
- **Next:** a **gradient skybox** (teal→peach) with **two crescent moons** and an **aurora** ribbon; a faint green **point light** under the acid lake so it glows from within.

## Per-element art pass (replace primitives with models)

| Greybox object | Becomes | Key art notes |
|---|---|---|
| `Farmhouse_A/B/C → Dome` | Organic dome hut | Blobby, bioluminescent seams, too many mismatched windows. Door = orange-glowing orifice. |
| `Barn` | Large domed barn | Same family, bigger; add hanging pipes between it and the Silo. |
| `Silo` | Tall ribbed tank | Keep it the **tallest** thing — it's the map's nav-beacon. |
| `Stable_1/2` | Open animal pens | Bone-white polymer fencing; this is where quota creatures graze. |
| `AcidSurface` | Glowing acid pool | Emissive shader with slow UV scroll + rising gas particles + bubbling. |
| `Cornfield/Stalks` | 3m fleshy "corn" | Dense, sways with no wind; eyeball-pods; **must fully block the camera**. |
| `Rocks` | Cracked purple boulders | Crystal veins; the NE ridge is a climbable vantage. |
| `AlienTractor` | Rusted alien tractor | Half-sunk, headlight still flickering — great goofy landmark + cover. |
| `Crystals` | Translucent purple shards | Emissive; cluster at map edges to frame the space. |
| `Ship_Placeholder` | The crew's drop-ship | Safe hub; ramp faces the farm. Keep the `PlayerSpawn` + real `TeleporterPad` here. |

## Material recipe (Built-In Standard shader → later URP upgrade)
1. **Toon/flat look:** the painted style wants **flat shading + inked outlines**. Two cheap routes:
   - Add a **Standard material with low smoothness** + a post-process outline (or an inverted-hull outline shader) for the comic edge.
   - Or migrate the project to **URP** and use a toon/ramp shader + the built-in outline renderer feature. (URP is the eventual move; not required to keep building.)
2. **Emissives** (acid, bio-lights, crystals): enable Emission, HDR color from the palette, and let Bloom (post-process) do the glowing.
3. **Vertex-color / gradient** on terrain for the mauve→shadow variation.

## Vibe checklist (scary-goofy)
- Everything is **slightly wrong**: asymmetric, too many eyes/windows, fleshy where it should be metal.
- **Readable danger:** acid = unmistakable toxic green; cornfield = a wall of "you can't see in there."
- **Comedy props** (the sunken tractor, wobbly fences) undercut the dread — that Content Warning tonal seesaw.
- **Silhouette first:** the Silo, the domes, and the ridge should be recognizable as black shapes against the dusk sky.

> The greybox is spatially final (it matches the approved layout). Art replacement is per-object, so
> you can swap one model at a time and keep playtesting the whole time.
