using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using VFEC.Buildings;

namespace VFEC.Jobs
{
    public class JobGiver_SetupScorpion : ThinkNode_JobGiver
    {
        public float maxDistFromPoint = -1f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_SetupScorpion) base.DeepCopy(resolve);
            copy.maxDistFromPoint = maxDistFromPoint;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.equipment.Primary is Scorpion scorpion)
            {
                var rect = CellRect.CenteredOn(pawn.GetLord().CurLordToil.FlagLoc, Mathf.FloorToInt(maxDistFromPoint / 2));

                var c = rect.RandomCell;
                var placingRot = Rot4.FromAngleFlat((c - rect.CenterCell).AngleFlat);
                while (!GenConstruct.CanPlaceBlueprintAt(scorpion.InnerThing.def, c, placingRot, pawn.Map))
                {
                    c = rect.RandomCell;
                    placingRot = Rot4.FromAngleFlat((c - rect.CenterCell).AngleFlat);
                }

                var blueprint = GenConstruct.PlaceBlueprintForInstall(scorpion, c, pawn.Map, placingRot, Faction.OfPlayer);
                return JobMaker.MakeJob(VFEC_DefOf.VFEC_SetupScorpion, null, blueprint);
            }

            return null;
        }
    }
}