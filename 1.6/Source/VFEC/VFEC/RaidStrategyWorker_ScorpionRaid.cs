using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEC
{
    public class RaidStrategyWorker_ScorpionRaid : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            var siegeSpot = RCellFinder.FindSiegePositionFrom(parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld, map);
            return new LordJob_ScorpionRaid(parms.faction, siegeSpot, Rand.Bool);
        }

        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind) => base.CanUseWith(parms, groupKind) && parms.faction.def.pawnGroupMakers.Any(pgm =>
            pgm.kindDef == groupKind && pgm.options.Any(UsesScorpion));

        public override List<Pawn> SpawnThreats(IncidentParms parms)
        {
            var group = parms.faction.def.pawnGroupMakers.Where(pgm =>
                    pgm.kindDef == PawnGroupKindDefOf.Combat && pgm.options.Any(UsesScorpion))
                .RandomElementByWeight(gm => gm.commonality);
            var option = group.options.Where(UsesScorpion)
                .RandomElementByWeight(opt => opt.selectionWeight);
            parms.points /= 2;
            var pawns = new List<Pawn>();
            pawns.AddRange(group.GeneratePawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms)));
            for (var i = 0; i < parms.points / option.Cost; i++)
                pawns.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(option.kind, parms.faction, PawnGenerationContext.NonPlayer, -1, mustBeCapableOfViolence: true,
                    allowFood: def.pawnsCanBringFood, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance)));

            parms.raidArrivalMode.Worker.Arrive(pawns, parms);

            return pawns;
        }

        private static bool UsesScorpion(PawnGenOption option) =>
            option?.kind?.weaponTags is not null && option.kind.weaponTags.Count == 1 && option.kind.weaponTags.Contains("ClassicalScorpion");
    }
}