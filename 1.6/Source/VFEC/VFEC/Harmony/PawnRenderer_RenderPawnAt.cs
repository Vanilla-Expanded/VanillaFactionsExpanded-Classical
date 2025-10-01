
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace VFEC
{
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt", new Type[]
{
    typeof(Vector3),
    typeof(Rot4?),
    typeof(bool)
})]
    public class VFEC_PawnRenderer_RenderPawnAt_Patch
    {
        [HarmonyPrefix]
        private static bool PreFix(Pawn ___pawn)
        {
            if (___pawn?.Map != null) {
                Building_Bed bed = RestUtility.CurrentBed(___pawn);
                if (bed?.def == VFEC_DefOf.VFEC_Tent) {
                    return false;
                
                }
            }return true;
        }
    }
}