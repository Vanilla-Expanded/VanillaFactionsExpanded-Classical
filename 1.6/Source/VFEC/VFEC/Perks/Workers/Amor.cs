using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEC.Perks.Workers
{
    public class Amor : PerkWorker
    {
        public Amor(PerkDef def) : base(def)
        {
        }

        public override IEnumerable<Patch> GetPatches()
        {
            yield return Patch.Postfix
            (
                AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf)),
                AccessTools.Method(GetType(), nameof(ForceMarriedOpinion))
            );
            yield return Patch.Postfix
            (
                AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionExplanation)),
                AccessTools.Method(GetType(), nameof(ForceMarriedExplain))
            );
        }

        public static void ForceMarriedOpinion(Pawn ___pawn, Pawn other, ref int __result)
        {
            if (___pawn.GetSpouses(false).Contains(other)) __result = 100;
        }

        public static void ForceMarriedExplain(Pawn ___pawn, Pawn other, ref string __result)
        {
            if (___pawn.GetSpouses(false).Contains(other)) __result += $"\n{GameComponent_PerkManager.Instance.FirstPerk<Amor>().LabelCap}: 100";
        }
    }
}