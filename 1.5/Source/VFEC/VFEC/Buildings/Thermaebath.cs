using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEC.Buildings
{
    public class Thermaebath : Building, IBuildingPawns
    {
        private Building_SteamGeyser geyser;

        private List<Pawn> occupants = new();

        public List<Pawn> Occupants() => occupants;

        public void Notify_Entered(Pawn pawn)
        {
            occupants.Add(pawn);
        }

        public void Notify_Left(Pawn pawn)
        {
            occupants.Remove(pawn);
        }

        public override void Tick()
        {
            base.Tick();
            geyser ??= (Building_SteamGeyser) Map.thingGrid.ThingAt(Position, ThingDefOf.SteamGeyser);

            if (geyser != null) geyser.harvester = this;

            if (this.IsHashIntervalTick(Rand.Range(5, 10)))
            {
                var rect = this.OccupiedRect();
                FleckMaker.ThrowAirPuffUp(new Vector3(Rand.Range(rect.minX, rect.maxX + 1f), DrawPos.y, Rand.Range(rect.minZ, rect.maxZ + 1f)), Map);
            }

            if (occupants.Count > 5 && this.IsHashIntervalTick(250))
            {
                var pawn = occupants.RandomElement();
                var partner = occupants.Except(pawn).RandomElement();
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                FleckMaker.ThrowMetaIcon(partner.Position, partner.Map, FleckDefOf.Heart);
                var lovinMemory = (Thought_Memory) ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                if (pawn.health is {hediffSet: { }} && pawn.health.hediffSet.hediffs.Any(h => h.def == HediffDefOf.LoveEnhancer) || partner.health?.hediffSet != null &&
                    partner.health.hediffSet.hediffs.Any(h => h.def == HediffDefOf.LoveEnhancer)) lovinMemory.moodPowerFactor = 1.5f;

                pawn.needs.mood?.thoughts.memories.TryGainMemory(lovinMemory, partner);

                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner)
                    ? HistoryEventDefOf.GotLovin_Spouse
                    : HistoryEventDefOf.GotLovin_NonSpouse, pawn.Named(HistoryEventArgsNames.Doer)));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref occupants, "occupants", LookMode.Reference);
        }
    }

    public interface IBuildingPawns
    {
        public List<Pawn> Occupants();
        public void Notify_Entered(Pawn pawn);
        public void Notify_Left(Pawn pawn);
    }
}