using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VFEC.Senators;

namespace VFEC.Perks.Workers
{
    [StaticConstructorOnStartup]
    public class VeniVidiVici : PerkWorker
    {
        public static readonly Texture2D TransferToColonyTex = ContentFinder<Texture2D>.Get("UI/Gizmos/TransferToColony");

        public VeniVidiVici(PerkDef def) : base(def)
        {
        }

        public override IEnumerable<Patch> GetPatches()
        {
            yield return Patch.Postfix(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)), AccessTools.Method(GetType(), nameof(AddGizmo)));
        }

        public static IEnumerable<Gizmo> AddGizmo(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (var gizmo in gizmos) yield return gizmo;

            if (__instance.RaceProps.Humanlike && __instance.Faction is not null && WorldComponent_Senators.Instance.United(__instance.Faction))
                yield return new Command_Action
                {
                    defaultLabel = "VFEC.TransferToColony".Translate(),
                    icon = TransferToColonyTex,
                    action = () => { __instance.SetFaction(Faction.OfPlayer); }
                };
        }
    }
}