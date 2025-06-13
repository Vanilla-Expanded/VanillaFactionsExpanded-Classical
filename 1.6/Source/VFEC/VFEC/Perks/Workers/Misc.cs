using RimWorld;
using Verse;

namespace VFEC.Perks.Workers
{
    public class ArsLonga : PerkWorker
    {
        public ArsLonga(PerkDef def) : base(def)
        {
        }

        public override bool ShouldModifyStatsOf(StatRequest req, StatDef stat) =>
            base.ShouldModifyStatsOf(req, stat) && req.HasThing && req.Thing.TryGetComp<CompArt>() is not null;
    }

    public class Pecunia : PerkWorker
    {
        public Pecunia(PerkDef def) : base(def)
        {
        }

        public override bool ShouldModifyStatsOf(StatRequest req, StatDef stat) =>
            base.ShouldModifyStatsOf(req, stat) && req.HasThing && req.Thing.def.IsWeapon;
    }

    public class Corpore : PlayerOnly
    {
        public Corpore(PerkDef def) : base(def)
        {
        }

        public override bool ShouldModifyStatsOf(StatRequest req, StatDef stat) =>
            base.ShouldModifyStatsOf(req, stat) && req.HasThing && req.Thing is Pawn {mindState: {mentalBreaker: var breaker}} && !breaker.BreakMinorIsImminent;
    }

    public class PlayerOnly : PerkWorker
    {
        public PlayerOnly(PerkDef def) : base(def)
        {
        }

        public override bool ShouldModifyStatsOf(StatRequest req, StatDef stat) =>
            base.ShouldModifyStatsOf(req, stat) && (req.Faction is {IsPlayer: true} || req.Thing is {Faction: {IsPlayer: true}});
    }
}