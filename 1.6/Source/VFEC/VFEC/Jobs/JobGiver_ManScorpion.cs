using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEC.Jobs
{
    public class JobGiver_ManScorpion : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.GetLord()?.CurLordToil is LordToil_ScorpionCamp {data: LordToilData_ScorpionCamp data} && data.scorpions.TryGetValue(pawn, out var turret))
            {
                var job = JobMaker.MakeJob(JobDefOf.ManTurret, turret);
                job.expiryInterval = 2000;
                job.checkOverrideOnExpire = true;
                return job;
            }

            return null;
        }
    }
}