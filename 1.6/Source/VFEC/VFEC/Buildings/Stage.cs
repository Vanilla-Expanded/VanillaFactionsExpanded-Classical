using System.Collections.Generic;
using Verse;

namespace VFEC.Buildings
{
    public class Stage : Building, IBuildingPawns
    {
        private List<Pawn> performers = new();
        public List<Pawn> Occupants() => performers;

        public void Notify_Entered(Pawn pawn)
        {
            performers.Add(pawn);
        }

        public void Notify_Left(Pawn pawn)
        {
            performers.Remove(pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref performers, "performers", LookMode.Reference);
        }
    }
}