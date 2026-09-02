# No Overeating

A RimWorld 1.6 mod that stops pawns and animals from eating a piece of food when that
last piece would overflow their stomach. Vanilla wastes the overflow silently: the food
need clamps at its maximum, so the extra nutrition - and the food item that carried it -
is destroyed without any benefit.

## Example

An adult ibex at 0.06/0.32 food finds rice (0.05 nutrition per grain):

- Room in the stomach: 0.32 - 0.06 = 0.26 nutrition = 5.2 grains.
- Vanilla: eats 6 grains, the 6th one overflows and 0.04 is wasted.
- This mod: eats 5 grains (+0.25 to 0.31/0.32). Nothing is wasted; the ibex is simply
  hungry again a bit sooner.

When every available food overflows anyway, overeating stays allowed: an ibex that only
has a simple meal (0.9 nutrition) at 0.26 room still eats one - the alternative is not
eating at all.

## How it works

Vanilla counts food pieces with rounding in two places; the mod floors both, with a
minimum of one piece. All values are read from the game at runtime - the food's
nutrition, the eater's stomach size, its current fullness - so DLC foods, modded foods
and anything that changes an eater's nutrition needs work automatically:

1. **`FoodUtility.StackCountForNutrition` (postfix)** - the pickup side. Vanilla rounds
   to nearest (which rounds up); the mod floors, so only whole pieces that fit get
   reserved, carried or fed.
2. **`Thing.IngestedCalculateAmounts` (postfix)** - the consumption side. Vanilla ceils
   the pieces eaten per meal; the mod floors, so the piece that does not fit is not
   eaten.

It is pure Harmony - no def changes, nothing saved to the game state, safe to add and
remove at any time.

## Compatibility

- Covers every vanilla eating path: humans and animals, map eating, caravans, feeding
  patients, bottle-feeding babies, administering food as medicine.
- Requires the [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) mod.
- RimWorld 1.6.

## Mod settings

- **Enable no overeating** - master toggle (off = exact vanilla behavior).
- **Debug logging** - logs every adjusted pickup and meal with full numbers:

```
[NoOvereating] Pickup: wants 0.26 nutrition, 0.05/piece -> take 5 instead of 6 pieces (saves 0.05).
[NoOvereating] Eat: Ibex (0.06/0.32 food, wants 0.26) Rice 0.05/piece, stack 23 -> eat 5 instead of 6 (+0.25 to 0.31, overflow avoided 0.05).
[NoOvereating] Eat: Ibex (0.06/0.32 food, wants 0.26) MealSimple 0.9/piece, stack 1 -> unavoidable overflow, eating 1 (+0.9 capped at 0.32, 0.64 wasted).
```

## Known limitations

- **Food choice is unchanged.** Pawns still prefer the nicest food they can reach; if
  that food overflows (a simple meal when rice would fit), it is eaten anyway. The mod
  removes the waste, it does not make pawns pick smaller food.
- **Meals end slightly below full** - up to one piece short - so pawns get hungry a
  bit sooner. That is the trade-off for zero waste.
- **Modded thing classes that override the vanilla piece counting keep their own
  behavior.** The mod only patches the base `Thing` implementation.
- **Predator corpse eating and grazing are untouched** - vanilla already handles both
  without overflow (partial body parts, fractional plant growth).
- **Binge eating (mental break) and hemogen packs still consume more than fits** -
  deliberately; their jobs ask for it and are detected via job flags and left alone.

## Technical notes

- Each Harmony patch is applied in its own try/catch: if a game update renames a patch
  target, that patch is skipped with an error in the log while the rest keeps working.
  The mod never crashes and never touches saves.
- A small tolerance compensates float division noise, so a stomach with room for
  exactly N pieces takes exactly N (0.25/0.05 evaluates to 4.9999... in float math).
- Both patch targets run once per meal decision, not per tick; the performance
  overhead is negligible.

## Build from source

Requires the .NET SDK.

```
cd Source/NoOvereating
dotnet build -p:RimWorldDir="C:\Path\To\RimWorld"
```

Add `-p:HarmonyDir="C:\Path\To\Harmony"` if Harmony is not installed at the default
Steam Workshop location
(`...\steamapps\workshop\content\294100\2009463077\Current\Assemblies`).

The output lands in `Assemblies/NoOvereating.dll`; the whole `NoOvereating` folder can
be junctioned or copied into the game's `Mods` directory.
