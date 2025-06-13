using RimWorld;
using Verse;
using Verse.AI;
using VFEC.Buildings;

namespace VFEC.Jobs
{
    public class JoyGiver_Stage : JoyGiver_WatchBuilding
    {
        protected override Job TryGivePlayJob(Pawn pawn, Thing bestGame)
        {
            if (bestGame is IBuildingPawns bp && bp.Occupants().Count < 2) return JobMaker.MakeJob(VFEC_DefOf.VFEC_Stage_Performance, bestGame);
            return base.TryGivePlayJob(pawn, bestGame);
        }
    }
}