using System.Globalization;
using UnityEngine;
using Verse;

namespace NoOvereating
{
    internal static class NoOvereatingUtility
    {
        /// <summary>
        /// Tolerance for float noise: 0.25 wanted / 0.05 per piece evaluates to
        /// 4.9999... in 32-bit float math. Without this a pawn with room for exactly
        /// N whole pieces would take N-1.
        /// </summary>
        public const float Epsilon = 1E-05f;

        /// <summary>
        /// How many whole pieces of food fit into <paramref name="nutritionWanted"/>
        /// without overflowing. 0 when not even a single piece fits; callers decide
        /// what to do then (eating one anyway is the accepted fallback).
        /// </summary>
        public static int WholePiecesThatFit(float nutritionWanted, float nutritionPerPiece)
        {
            return Mathf.FloorToInt((nutritionWanted + Epsilon) / nutritionPerPiece);
        }

        /// <summary>Culture-invariant formatting so logs read the same on every locale.</summary>
        public static string F(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Optional per-decision logging, off by default (see mod settings).</summary>
    internal static class DebugLog
    {
        private const string Prefix = "[NoOvereating] ";

        public static bool Enabled => NoOvereatingMod.Settings?.debugLogging ?? false;

        public static void Message(string message)
        {
            if (Enabled)
            {
                Verse.Log.Message(Prefix + message);
            }
        }
    }
}
