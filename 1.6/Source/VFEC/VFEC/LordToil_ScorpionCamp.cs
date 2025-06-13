using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using VFEC.Buildings;

namespace VFEC
{
    public class LordToil_ScorpionCamp : LordToil
    {
        private static readonly FloatRange MealCountRangePerRaider = new(1f, 3f);

        public LordToil_ScorpionCamp(IntVec3 setupSpot, bool useTents)
        {
            data = new LordToilData_ScorpionCamp();
            Data.setupSpot = setupSpot;
            Data.useTents = useTents;
        }

        private LordToilData_ScorpionCamp Data => data as LordToilData_ScorpionCamp;

        public override IntVec3 FlagLoc => Data.setupSpot;

        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
                if (pawn.equipment.Primary is Scorpion)
                {
                    pawn.mindState.duty = new PawnDuty(VFEC_DefOf.VFEC_ScorpionOperator, Data.setupSpot) {radius = 16f};
                    Data.scorpionOperators.Add(pawn);
                }
                else if (Data.scorpionOperators.Contains(pawn))
                    pawn.mindState.duty = new PawnDuty(VFEC_DefOf.VFEC_ScorpionOperator, Data.setupSpot) {radius = 16f};
                else
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, Data.setupSpot) {radius = 16f};
        }

        public override void Init()
        {
            base.Init();
            var pods = new List<List<Thing>>();
            var num2 = Mathf.RoundToInt(MealCountRangePerRaider.RandomInRange * lord.ownedPawns.Count);
            var food = new List<Thing>();
            for (var l = 0; l < num2; l++)
            {
                var item = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                food.Add(item);
            }

            pods.Add(food);
            DropPodUtility.DropThingGroupsNear(Data.setupSpot, Map, pods);
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            if (GenTicks.TicksGame % 200 == 0)
                if (!GenRadial.RadialDistinctThingsAround(Data.setupSpot, lord.Map, 16f, true).OfType<ScorpionTurret>().Any() &&
                    !Data.scorpionOperators.Any(p => p.equipment.Primary is Scorpion || p.carryTracker.CarriedThing is Scorpion) ||
                    Data.scorpionOperators.All(p => p.Dead || p.Downed))
                    lord.ReceiveMemo("NoScorpions");
        }
    }

    // ReSharper disable InconsistentNaming
    public class LordToilData_ScorpionCamp : LordToilData
    {
        private List<Pawn> keys;
        public HashSet<Pawn> scorpionOperators = new();
        public Dictionary<Pawn, ScorpionTurret> scorpions = new();
        public IntVec3 setupSpot;
        public bool useTents;
        private List<ScorpionTurret> values;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref setupSpot, "setupSpot");
            Scribe_Values.Look(ref useTents, "useTents");
            Scribe_Collections.Look(ref scorpionOperators, "scorpionOperators", LookMode.Reference);
            Scribe_Collections.Look(ref scorpions, "scorpions", LookMode.Reference, LookMode.Reference, ref keys, ref values);
        }
    }
}