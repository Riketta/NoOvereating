# No Overeating

A RimWorld 1.6 mod that stops pawns and animals from eating a piece of food when that
last piece would overflow their stomach. Vanilla wastes the overflow silently: the food
need clamps at its maximum, so the nutrition (and thus the food item) is destroyed
without benefit.

## Example

An adult ibex at 0.06/0.32 food finds rice (0.05 nutrition per grain):

- Room in the stomach: 0.32 - 0.06 = 0.26 nutrition, i.e. 5.2 grains.
- Vanilla: `CeilToInt(0.26 / 0.05)` = **6 grains** eaten, the 6th one supplies 0.05 but
  only 0.01 fits - 0.04 is wasted.
- This mod: **5 grains** eaten (+0.25 to 0.31/0.32). Nothing is wasted; the ibex is
  simply hungry again a bit sooner.

When every available food overflows anyway, overeating stays allowed: an ibex that only
has a simple meal (0.9 nutrition) at 0.26 room still eats one, because the alternative
is not eating at all. Same for a pawn forced to eat while nearly full.

## Design

Vanilla counts food pieces in two places, both rounding up. The mod floors both, with a
minimum of one piece, and reads every number it uses from the game at runtime - food
nutrition via `FoodUtility.NutritionForEater`, stomach size via `Need_Food.MaxLevel`
(the MaxNutrition stat), current fullness via `Need_Food.CurLevel`. No foods, races,
nutrition values or thresholds are hardcoded, so DLC foods, modded foods, babies with
smaller stomachs and genes/hediffs that resize any of it are all handled automatically.
It is pure Harmony - no def changes, no custom saved data, nothing persisted:

1. **`FoodUtility.StackCountForNutrition` (postfix)** - the pickup side. Vanilla
   rounds to nearest (which rounds up at >= 0.5 remainder, e.g. 0.28/0.05 = 5.6 -> 6);
   the postfix floors (5.6 -> 5). This fixes every caller coherently: `WillIngestStackCountOf`
   (job givers, float menu, inventory eating, feeding patients, bottle-feeding),
   food reservations, caravan food search, animal training feeds and fishing catches.
2. **`Thing.IngestedCalculateAmounts` (postfix)** - the consumption side. Vanilla ceils
   the number of pieces eaten in one ingestion event (the actual waste from the
   example); the postfix floors it, keeping vanilla's own clamps (`stackCount`,
   `maxNumToIngestAtOnce`, minimum 1 piece).

A tiny epsilon (1e-5) compensates float division noise, so a stomach with room for
exactly N pieces takes exactly N, not N-1 (0.25/0.05 computes as 4.9999... in float).

Deliberately untouched:

- **Binge eating** (`Job.overeat`, the food/drug binging mental break): its job asks for
  at least 0.75 nutrition per bite on purpose, and the consumption patch detects the
  flag and leaves those jobs at vanilla amounts.
- **Hemogen packs** (`Job.ingestTotalCount`, Sanguophage): the whole carried stack is
  meant to be consumed; detected by the same flag check and left alone.
- **Corpse eating** (`Corpse` overrides `IngestedCalculateAmounts`): predators bite off
  body parts one at a time until 90% full - already overflow-free by design.
- **Grazing** (`Plant` overrides `IngestedCalculateAmounts`): wild plants are eaten
  fractionally by growth - already overflow-free by design.
- **Scheduled drug taking**: `WillIngestStackCountOf` returns the drug's fixed
  `defaultNumToIngestAtOnce` without ever calling `StackCountForNutrition`, so taking
  a scheduled beer/tea dose works exactly like vanilla.
- **Food choice**: which food a pawn prefers is unchanged (vanilla still picks the
  nicest food it can reach; if that one overflows, it is eaten anyway).
- Any modded class that overrides `IngestedCalculateAmounts` keeps its own logic -
  Harmony patches only the base `Thing` implementation.

## Mod settings

- **Enable no overeating** - master toggle (off = exact vanilla behavior).
- **Debug logging** - every adjusted pickup and meal is logged with full numbers:

```
[NoOvereating] Pickup: wants 0.26 nutrition, 0.05/piece -> take 5 instead of 6 pieces (saves 0.05).
[NoOvereating] Eat: Ibex (0.06/0.32 food, wants 0.26) Rice 0.05/piece, stack 23 -> eat 5 instead of 6 (+0.25 to 0.31, overflow avoided 0.05).
[NoOvereating] Eat: Ibex (0.06/0.32 food, wants 0.26) MealSimple 0.9/piece, stack 1 -> unavoidable overflow, eating 1 (+0.9 capped at 0.32, 0.64 wasted).
```

## Files

```
About/                  mod metadata
Languages/English/      settings UI strings
Source/NoOvereating     C# source + csproj
Assemblies/             compiled NoOvereating.dll
```

Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

## Building

Requires the .NET SDK. The csproj defaults to
`E:\SteamLibrary\steamapps\common\...`; override with your install path:

```
cd Source/NoOvereating
dotnet build -p:RimWorldDir="C:\Path\To\RimWorld" -p:HarmonyDir="C:\Path\To\HarmonyMod\Current\Assemblies"
```

Output lands in `Assemblies/NoOvereating.dll`. The whole `NoOvereating` folder can be
junctioned/copied into the game's `Mods` directory.

## Technical notes

- **Failure isolation**: each Harmony patch is applied in its own try/catch. If a game
  update renames one patch target, that patch is skipped with a red error log entry
  while the other keeps working (pickup without consumption fix just leaves a carried
  leftover, consumption without pickup fix still prevents the waste). Never a crash,
  never a broken save.
- **Savegames**: nothing is written to saves - no GameComponent, no ThingComps, no
  replaced defs. Settings live in the player profile (`ModSettings`), not in saves. The
  mod can be added or removed at any time; meals already in progress simply finish
  under whatever rules are loaded then.
- **Performance**: both patch targets run once per meal decision, not per tick; the
  postfixes are a few float ops and two dictionary-free reads.
