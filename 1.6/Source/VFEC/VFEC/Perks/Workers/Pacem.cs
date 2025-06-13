using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace VFEC.Perks.Workers
{
    public class Pacem : PerkWorker
    {
        public Pacem(PerkDef def) : base(def)
        {
        }

        public override IEnumerable<Patch> GetPatches()
        {
            yield return Patch.Postfix(AccessTools.PropertyGetter(typeof(MentalBreaker), "CanDoRandomMentalBreaks"), AccessTools.Method(GetType(), nameof(NoBreaksDrafted)));
        }

        public static void NoBreaksDrafted(Pawn ___pawn, ref bool __result)
        {
            if (__result && ___pawn.Drafted) __result = false;
        }
    }
}