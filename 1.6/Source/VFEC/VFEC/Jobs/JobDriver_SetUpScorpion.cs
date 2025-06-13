using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using VFEC.Buildings;

namespace VFEC.Jobs
{
    public class JobDriver_SetUpScorpion : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetB, job, 1, 0, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var scorpion = pawn.equipment.Primary as Scorpion;
            this.FailOn(() => scorpion is null);
            yield return Toils_General.Do(() =>
            {
                pawn.equipment.TryTransferEquipmentToContainer(scorpion, pawn.carryTracker.innerContainer);
                if (pawn.GetLord()?.CurLordToil is LordToil_ScorpionCamp {data: LordToilData_ScorpionCamp data})
                    data.scorpions.SetOrAdd(pawn, scorpion?.InnerThing as ScorpionTurret);
            });
            yield return Toils_Haul.CarryHauledThingToContainer();
            yield return Toils_Goto.MoveOffTargetBlueprint(TargetIndex.B);
            yield return Toils_General.Wait(50, TargetIndex.B).WithProgressBarToilDelay(TargetIndex.B);
            yield return Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(TargetIndex.B, TargetIndex.C);
        }
    }
}