using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEC.Comps;

public class CompTent : ThingComp
{
    public override void Notify_AddBedThoughts(Pawn pawn)
    {
        base.Notify_AddBedThoughts(pawn);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOutside);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOnGround);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInCold);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInHeat);
    }
}

public class CompProperties_Tent : CompProperties
{
   

    public CompProperties_Tent() => compClass = typeof(CompTent);

   
}
