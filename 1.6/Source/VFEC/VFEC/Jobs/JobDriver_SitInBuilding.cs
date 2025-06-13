using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VFEC.Buildings;

namespace VFEC.Jobs
{
    public class JobDriver_SitInBuilding : JobDriver
    {
        public override Vector3 ForcedBodyOffset => new Vector3(0.75f, 0f, 0f).RotatedBy(pawn.Position.ToVector3().AngleToFlat(TargetThingA.TrueCenter()));

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, job.def.joyMaxParticipants, 0, null, errorOnFailed) &&
                                                                               pawn.Reserve(job.targetB, job, 1, 0, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            var toil = new Toil
            {
                initAction = delegate
                {
                    if (TargetThingA is IBuildingPawns bp) bp.Notify_Entered(pawn);
                },
                tickAction = delegate
                {
                    pawn.rotationTracker.Face(TargetThingA.TrueCenter());
                    pawn.GainComfortFromCellIfPossible(1);
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

        public override object[] TaleParameters()
        {
            return new object[]
            {
                pawn,
                TargetA.Thing.def
            };
        }
    }
}