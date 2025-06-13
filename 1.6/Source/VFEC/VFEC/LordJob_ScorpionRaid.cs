using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEC
{
    public class LordJob_ScorpionRaid : LordJob
    {
        private Faction faction;
        private IntVec3 setupSpot;
        private bool useTents;

        public LordJob_ScorpionRaid(Faction faction, IntVec3 setupSpot, bool useTents)
        {
            this.faction = faction;
            this.setupSpot = setupSpot;
            this.useTents = useTents;
        }

        public override StateGraph CreateGraph()
        {
            var stateGraph = new StateGraph();
            var travel = stateGraph.AttachSubgraph(new LordJob_Travel(setupSpot).CreateGraph()).StartingToil;
            LordToil main = new LordToil_ScorpionCamp(setupSpot, useTents);
            stateGraph.AddToil(main);
            var exitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true) {useAvoidGrid = true};
            stateGraph.AddToil(exitMap);
            var assault = stateGraph.AttachSubgraph(new LordJob_AssaultColony(faction).CreateGraph()).StartingToil;
            var arrived = new Transition(travel, main);
            arrived.AddTrigger(new Trigger_Memo("TravelArrived"));
            arrived.AddTrigger(new Trigger_TicksPassed(5000));
            stateGraph.AddTransition(arrived);
            var assaultNow = new Transition(main, assault);
            assaultNow.AddTrigger(new Trigger_Memo("NoScorpions"));
            assaultNow.AddTrigger(new Trigger_PawnHarmed(0.08f));
            assaultNow.AddTrigger(new Trigger_FractionPawnsLost(0.3f));
            assaultNow.AddTrigger(new Trigger_TicksPassed((int) (60000f * Rand.Range(1.5f, 3f))));
            assaultNow.AddPreAction(new TransitionAction_Message("MessageSiegersAssaulting".Translate(faction.def.pawnsPlural, faction), MessageTypeDefOf.ThreatBig));
            assaultNow.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(assaultNow);
            var leave = new Transition(main, exitMap);
            leave.AddSource(assault);
            leave.AddSource(travel);
            leave.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            leave.AddPreAction(new TransitionAction_Message("MessageRaidersLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            stateGraph.AddTransition(leave);
            return stateGraph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref useTents, "useTents");
            Scribe_Values.Look(ref setupSpot, "setupSpot");
            Scribe_References.Look(ref faction, "faction");
        }
    }
}