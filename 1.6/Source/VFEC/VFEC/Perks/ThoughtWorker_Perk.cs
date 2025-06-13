using RimWorld;
using Verse;

namespace VFEC.Perks
{
    public class ThoughtWorker_Perk : ThoughtWorker
    {
        private PerkDef perk;
        public PerkDef Perk => perk ??= def.GetModExtension<PerkExtension>().perk;

        protected override ThoughtState CurrentStateInternal(Pawn p) =>
            GameComponent_PerkManager.Instance.ActivePerks.Contains(Perk) ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
    }

    public class PerkExtension : DefModExtension
    {
        public PerkDef perk;
    }
}