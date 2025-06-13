using RimWorld;
using Verse;

namespace VFEC.Perks
{
    public class ThoughtWorker_Hot_Patched : ThoughtWorker_Hot
    {
        private PerkDef perk;
        public PerkDef Perk => perk ??= def.GetModExtension<PerkExtension>().perk;
        public override float MoodMultiplier(Pawn p) => p.Faction is { IsPlayer: true } && GameComponent_PerkManager.Instance.ActivePerks.Contains(Perk) ? 0.5f : 1f;
    }

    public class ThoughtWorker_Cold_Patched : ThoughtWorker_Cold
    {
        private PerkDef perk;
        public PerkDef Perk => perk ??= def.GetModExtension<PerkExtension>().perk;
        public override float MoodMultiplier(Pawn p) => p.Faction is { IsPlayer: true } && GameComponent_PerkManager.Instance.ActivePerks.Contains(Perk) ? 0.5f : 1f;
    }
}