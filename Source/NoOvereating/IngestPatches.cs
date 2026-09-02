using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NoOvereating
{
    /// <summary>
    /// Pickup side: how many pieces of food get reserved, carried or handed over for a
    /// wanted nutrition amount. Vanilla (FoodUtility.StackCountForNutrition) rounds to
    /// nearest, which rounds UP whenever the remainder is half a piece or more
    /// (0.28 wanted / 0.05 rice = 5.6 -&gt; 6 pieces, the 6th one overflows). The postfix
    /// floors it instead: only whole pieces that fit, but never fewer than one - if
    /// nothing fits at all, a single piece is still taken (overeating is then unavoidable).
    /// Every caller is fixed coherently: job givers, food reservations, the float menu,
    /// eating from inventory, caravan eating, feeding patients/babies, animal training.
    /// </summary>
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.StackCountForNutrition))]
    internal static class Patch_FoodUtility_StackCountForNutrition
    {
        private static void Postfix(float wantedNutrition, float singleFoodNutrition, ref int __result)
        {
            if (!NoOvereatingMod.Active || singleFoodNutrition <= 0f || wantedNutrition <= 0.0001f)
            {
                return; // zero-wanted stays 0, zero-nutrition food keeps vanilla handling
            }

            int vanilla = __result; // Mathf.Max(RoundToInt(wanted / perPiece), 1)
            int fit = Mathf.Max(NoOvereatingUtility.WholePiecesThatFit(wantedNutrition, singleFoodNutrition), 1);
            if (fit >= vanilla)
            {
                return;
            }

            __result = fit;
            if (DebugLog.Enabled)
            {
                DebugLog.Message("Pickup: wants " + NoOvereatingUtility.F(wantedNutrition) + " nutrition, "
                    + NoOvereatingUtility.F(singleFoodNutrition) + "/piece -> take " + fit + " instead of " + vanilla
                    + " pieces (saves " + NoOvereatingUtility.F((vanilla - fit) * singleFoodNutrition) + ").");
            }
        }
    }

    /// <summary>
    /// Consumption side: how many pieces one ingestion event actually eats. Vanilla
    /// (Thing.IngestedCalculateAmounts) ceils - wanted 0.26 / 0.05 per rice = 5.2 -&gt; 6
    /// grains, the last 0.04 overflows the stomach and is silently discarded by the food
    /// need. The postfix floors instead: eat only whole pieces that fit, minimum one
    /// (better to overeat than to not eat at all).
    ///
    /// This is the single funnel every vanilla eating path goes through: map eating
    /// (Toils_Ingest.FinalizeIngest), caravan eating, bottle-feeding babies and
    /// administering food as medicine. Jobs flagged overeat (food/drug binging) or
    /// ingestTotalCount (hemogen packs) deliberately ask for more than fits and are
    /// left at vanilla amounts. Corpse and Plant override this method with their
    /// own piece logic (predators bite body parts until 90% full; grazing eats plants
    /// fractionally by growth) - those overrides are untouched, and so are any modded
    /// overrides: they never call into this base implementation.
    /// </summary>
    [HarmonyPatch(typeof(Thing), "IngestedCalculateAmounts")]
    internal static class Patch_Thing_IngestedCalculateAmounts
    {
        private static void Postfix(Thing __instance, Pawn ingester, float nutritionWanted, ref int numTaken, ref float nutritionIngested)
        {
            if (!NoOvereatingMod.Active || ingester?.needs?.food == null)
            {
                return; // no stomach that could overflow - keep vanilla amounts
            }
            Job job = ingester.CurJob;
            if (job != null && (job.overeat || job.ingestTotalCount))
            {
                return; // binging / eat-the-whole-stack jobs ask for more than fits on purpose
            }
            if (numTaken <= 0)
            {
                return; // defensive: numTaken is the divisor below (vanilla ends at Min(n,1))
            }
            // Vanilla computed nutritionIngested = numTaken * NutritionForEater: derive the
            // per-piece value instead of evaluating the stat a second time. This also stays
            // consistent with any other mod's postfix that already adjusted the amounts.
            float perPiece = nutritionIngested / numTaken;
            if (!(perPiece > 0f))
            {
                return; // zero-nutrition ingestibles (some drugs), or NaN from another mod
            }

            int wholeFit = NoOvereatingUtility.WholePiecesThatFit(nutritionWanted, perPiece);
            // Same clamps as vanilla, floor instead of ceil:
            int fit = Mathf.Min(wholeFit, __instance.stackCount);
            int maxAtOnce = __instance.def.ingestible.maxNumToIngestAtOnce;
            if (maxAtOnce > 0)
            {
                fit = Mathf.Min(fit, maxAtOnce);
            }
            fit = Mathf.Max(fit, 1);

            int vanilla = numTaken;
            Need_Food food = ingester.needs.food;
            if (fit < vanilla)
            {
                numTaken = fit;
                nutritionIngested = fit * perPiece;
                if (DebugLog.Enabled)
                {
                    DebugLog.Message("Eat: " + ingester.LabelShortCap + " (" + NoOvereatingUtility.F(food.CurLevel)
                        + "/" + NoOvereatingUtility.F(food.MaxLevel) + " food, wants " + NoOvereatingUtility.F(nutritionWanted)
                        + ") " + __instance.def.defName + " " + NoOvereatingUtility.F(perPiece) + "/piece, stack "
                        + __instance.stackCount + " -> eat " + fit + " instead of " + vanilla + " (+" + NoOvereatingUtility.F(fit * perPiece)
                        + " to " + NoOvereatingUtility.F(food.CurLevel + fit * perPiece) + ", overflow avoided "
                        + NoOvereatingUtility.F((vanilla - fit) * perPiece) + ").");
                }
            }
            else if (wholeFit == 0 && DebugLog.Enabled)
            {
                DebugLog.Message("Eat: " + ingester.LabelShortCap + " (" + NoOvereatingUtility.F(food.CurLevel)
                    + "/" + NoOvereatingUtility.F(food.MaxLevel) + " food, wants " + NoOvereatingUtility.F(nutritionWanted)
                    + ") " + __instance.def.defName + " " + NoOvereatingUtility.F(perPiece) + "/piece, stack "
                    + __instance.stackCount + " -> unavoidable overflow, eating " + fit + " (+" + NoOvereatingUtility.F(fit * perPiece)
                    + " capped at " + NoOvereatingUtility.F(food.MaxLevel) + ", " + NoOvereatingUtility.F(fit * perPiece - nutritionWanted)
                    + " wasted).");
            }
        }
    }
}
