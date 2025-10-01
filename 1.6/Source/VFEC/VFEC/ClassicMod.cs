using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEC.Buildings;

namespace VFEC;

public class ClassicMod : Mod
{
    public static Harmony Harm;

    public ClassicMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("oskarpotocki.vfe.classical");
        Harm.PatchAll(Assembly.GetExecutingAssembly());
        Harm.Patch(AccessTools.Method(typeof(Toils_LayDown), "ApplyBedThoughts"), postfix: new HarmonyMethod(GetType(), nameof(TellBedAboutThoughts)));
        Harm.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos)),
            postfix: new HarmonyMethod(GetType(), nameof(GetEquipGizmos)));
    }

    public static void TellBedAboutThoughts(Pawn actor)
    {
        if (actor.CurrentBed() is { AllComps: var comps })
            foreach (var comp in comps)
                comp?.Notify_AddBedThoughts(actor);
    }

    public static IEnumerable<Gizmo> GetEquipGizmos(IEnumerable<Gizmo> gizmos, Pawn_EquipmentTracker __instance)
    {
        foreach (var gizmo in gizmos) yield return gizmo;

        foreach (var gizmo in __instance.AllEquipmentListForReading.OfType<IEquipGizmos>().SelectMany(eg => eg.GetEquipGizmos(__instance.pawn)))
        {
            gizmo.Order = 1000;
            yield return gizmo;
        }
    }
}