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
    private static bool donePatches;
    private static readonly HashSet<ThingDef> tents = new();

    public CompProperties_Tent() => compClass = typeof(CompTent);

    public static bool HideUnderTent(ref bool __result, Pawn pawn)
    {
        var bed = pawn.CurrentBed();
        if (bed != null && tents.Contains(bed.def))
        {
            __result = true;
            return false;
        }

        return true;
    }

    public override void PostLoadSpecial(ThingDef parent)
    {
        base.PostLoadSpecial(parent);
        tents.Add(parent);
        if (!donePatches)
        {
            ClassicMod.Harm.Patch(AccessTools.Method(typeof(InvisibilityUtility), nameof(InvisibilityUtility.IsHiddenFromPlayer)),
                new(GetType(), nameof(HideUnderTent)));
            donePatches = true;
        }
    }
}
