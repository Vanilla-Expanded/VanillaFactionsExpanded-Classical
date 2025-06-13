using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VFEC.Buildings;

namespace VFEC.Jobs
{
    [StaticConstructorOnStartup]
    public class JobDriver_Performance : JobDriver
    {
        private static readonly Texture2D PerformanceTex = ContentFinder<Texture2D>.Get("UI/Performance/Performance");
        private bool faceOtherPawn;
        private Pawn otherPawn;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 2, 0, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetThingA.OccupiedRect().RandomCell, PathEndMode.OnCell);
            var toil = new Toil
            {
                initAction = delegate
                {
                    if (TargetThingA is not IBuildingPawns bp) return;
                    otherPawn = bp.Occupants().FirstOrDefault();
                    bp.Notify_Entered(pawn);
                },
                tickAction = delegate
                {
                    if (pawn.IsHashIntervalTick(60))
                    {
                        if (TargetThingA is IBuildingPawns bp) otherPawn = bp.Occupants().FirstOrDefault();
                        if (otherPawn is not null && Rand.Bool) faceOtherPawn = !faceOtherPawn;
                        if (Rand.Chance(0.25f)) pawn.pather.StartPath(TargetThingA.OccupiedRect().RandomCell, PathEndMode.OnCell);
                    }

                    if (faceOtherPawn) pawn.rotationTracker.FaceTarget(otherPawn);
                    else pawn.Rotation = TargetThingA.Rotation;

                    if (pawn.IsHashIntervalTick(Rand.Range(25, 50)))
                        if (otherPawn is not null) MoteMaker.MakeInteractionBubble(pawn, otherPawn, ThingDefOf.Mote_Speech, PerformanceTex);
                        else MoteMaker.MakeSpeechBubble(pawn, PerformanceTex);

                    JoyUtility.JoyTickCheckEnd(pawn,1, job.doUntilGatheringEnded ? JoyTickFullJoyAction.None : JoyTickFullJoyAction.EndJob, 1f, (Building) TargetThingA);
                },
                handlingFacing = true,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = job.doUntilGatheringEnded ? job.expiryInterval : job.def.joyDuration
            };
            toil.AddFinishAction(delegate
            {
                if (TargetThingA is IBuildingPawns bp) bp.Notify_Left(pawn);
            });
            yield return toil;
        }
    }
}