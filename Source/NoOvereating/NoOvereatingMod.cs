using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace NoOvereating
{
    public class NoOvereatingSettings : ModSettings
    {
        public bool enabled = true;

        public bool debugLogging = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
        }
    }

    public class NoOvereatingMod : Mod
    {
        public const string PackageId = "Riketta.NoOvereating";

        public static NoOvereatingSettings Settings;

        /// <summary>Master switch, read by every patch on each call. Null-safe: without
        /// settings the patches stay active rather than silently disabling the mod.</summary>
        public static bool Active => Settings?.enabled ?? true;

        public NoOvereatingMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<NoOvereatingSettings>();
            // Patch each class separately: a game update that renames one target must
            // degrade to "that vanilla behavior stays", never break the other patch.
            Harmony harmony = new Harmony(PackageId);
            PatchSafe(harmony, typeof(Patch_FoodUtility_StackCountForNutrition));
            PatchSafe(harmony, typeof(Patch_Thing_IngestedCalculateAmounts));
            DebugLog.Message("loaded (enabled=" + Settings.enabled.ToString().ToLowerInvariant()
                + ", debugLogging=" + Settings.debugLogging.ToString().ToLowerInvariant() + ").");
        }

        private static void PatchSafe(Harmony harmony, Type patchClass)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
                DebugLog.Message("applied " + patchClass.Name + ".");
            }
            catch (Exception e)
            {
                Log.Error("[NoOvereating] Patch " + patchClass.Name + " could not be applied (game update?). " + e.Message);
            }
        }

        public override string SettingsCategory()
        {
            return "NoOvereating.SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("NoOvereating.Enabled".Translate(), ref Settings.enabled, "NoOvereating.EnabledTip".Translate());
            list.Gap(12f);
            list.CheckboxLabeled("NoOvereating.DebugLogging".Translate(), ref Settings.debugLogging, "NoOvereating.DebugLoggingTip".Translate());
            list.End();
        }
    }
}
