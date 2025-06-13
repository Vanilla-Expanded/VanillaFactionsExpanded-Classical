using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEC.Jobs
{
    public class JoyGiver_InteractBuildingSitInside : JoyGiver_InteractBuilding
    {
        protected override Job TryGivePlayJob(Pawn pawn, Thing bestGame)
        {
            var rect = bestGame.OccupiedRect();
            return JobMaker.MakeJob(def.jobDef, bestGame, rect.EdgeCells.Where(c => c.GetFirstThing<Pawn>(pawn.Map) is null).RandomElement());
        }
    }
}