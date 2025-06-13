using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace VFEC.Perks
{
    public static class PerkPatches
    {
        public static IEnumerable<PerkWorker.Patch> GetPatches()
        {
            yield return PerkWorker.Patch.Postfix(
                AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
                AccessTools.Method(typeof(PerkPatches), nameof(PerkModifyStat))
            );
            yield return PerkWorker.Patch.Postfix
            (
                AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized)),
                AccessTools.Method(typeof(PerkPatches), nameof(PerkModifyExplanation))
            );
        }

        public static void PerkModifyStat(StatDef ___stat, StatRequest req, ref float __result)
        {
            if (!PerkWorker.ShouldModifyStatEver(___stat)) return;
            foreach (var perk in GameComponent_PerkManager.Instance.ActivePerks) perk.Worker.ModifyStat(req, ___stat, ref __result);
        }

        public static void PerkModifyExplanation(StatWorker __instance, StatDef ___stat, StatRequest req, ref string __result)
        {
            if (!PerkWorker.ShouldModifyStatEver(___stat)) return;
            foreach (var perk in GameComponent_PerkManager.Instance.ActivePerks) perk.Worker.ModifyStatExplain(req, ___stat, __instance, ref __result);
        }
    }
}