using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VFEC
{
    [StaticConstructorOnStartup]
    public class WorldComponent_RoadBuilding : WorldComponent
    {
        public static WorldComponent_RoadBuilding Instance;
        private List<Caravan> keys;
        private List<WorkInfo> values;
        public Dictionary<Caravan, WorkInfo> WorkInfos = new();

        static WorldComponent_RoadBuilding()
        {
            ClassicMod.Harm.Patch(AccessTools.Method(typeof(Caravan), nameof(Caravan.GetGizmos)),
                postfix: new HarmonyMethod(typeof(WorldComponent_RoadBuilding), nameof(AddRoadGizmos)));
            ClassicMod.Harm.Patch(AccessTools.Method(typeof(Caravan), "TickInterval"), postfix: new HarmonyMethod(typeof(WorldComponent_RoadBuilding), nameof(PostTick)));
            ClassicMod.Harm.Patch(AccessTools.Method(typeof(Caravan), nameof(Caravan.GetInspectString)),
                postfix: new HarmonyMethod(typeof(WorldComponent_RoadBuilding), nameof(AddToString)));
        }

        public WorldComponent_RoadBuilding(World world) : base(world) => Instance = this;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref WorkInfos, "workInfos", LookMode.Reference, LookMode.Deep, ref keys, ref values);
        }

        public static void AddToString(Caravan __instance, ref string __result)
        {
            if (Instance.WorkInfos.TryGetValue(__instance, out var workInfo))
                __result += "\n" + "VFEC.BuildingRoad".Translate(workInfo.ToBuild.LabelCap, (workInfo.WorkDone / (float) workInfo.WorkTotal).ToStringPercent(), workInfo.WorkDone,
                    workInfo.WorkTotal);
        }

        public static void PostTick(Caravan __instance)
        {
            if (__instance.IsHashIntervalTick(250) && Instance.WorkInfos.TryGetValue(__instance, out var workInfo))
            {
                workInfo.WorkDone += __instance.PawnsListForReading.Count(p => p.IsFreeColonist && !p.Dead && !p.Downed && !p.InMentalState);
                Instance.WorkInfos[__instance] = workInfo;
                if (__instance.Tile != workInfo.Tile || __instance.pather.MovingNow)
                    Instance.WorkInfos.Remove(__instance);
                else if (workInfo.WorkDone >= workInfo.WorkTotal)
                {
                    Find.WorldGrid.OverlayRoad(workInfo.Tile, workInfo.To, workInfo.ToBuild);
                    Find.World.renderer.SetAllLayersDirty();
                    Instance.WorkInfos.Remove(__instance);
                    __instance.pather.StartPath(workInfo.To, null, true);
                    Messages.Message("VFEC.RoadFinished".Translate(__instance.Name, workInfo.ToBuild.label), __instance, MessageTypeDefOf.TaskCompletion);
                }
            }
        }

        public static IEnumerable<Gizmo> AddRoadGizmos(IEnumerable<Gizmo> gizmos, Caravan __instance)
        {
            foreach (var gizmo in gizmos) yield return gizmo;

            if (!VFEC_DefOf.VFEC_RoadBuilding.IsFinished) yield break;

            if (__instance.pather.MovingNow) yield break;

            if (Instance.WorkInfos.ContainsKey(__instance)) yield break;

            foreach (var def in DefDatabase<RoadBuildingDef>.AllDefs)
                yield return new Command_Action
                {
                    defaultLabel = def.label,
                    defaultDesc = def.description,
                    icon = def.Icon,
                    action = delegate
                    {
                        Find.WorldTargeter.BeginTargeting(target =>
                        {
                            Instance.WorkInfos.Add(__instance, new WorkInfo
                            {
                                Caravan = __instance,
                                ToBuild = def.road,
                                WorkDone = 0,
                                WorkTotal = def.workRequired,
                                Tile = __instance.Tile,
                                To = target.Tile
                            });
                            return true;
                        }, true, canSelectTarget: target => Find.WorldGrid.IsNeighbor(__instance.Tile, target.Tile));
                    }
                };
        }
    }

    public class WorkInfo : IExposable
    {
        public Caravan Caravan;
        public int Tile;
        public int To;
        public RoadDef ToBuild;
        public int WorkDone;
        public int WorkTotal;

        public void ExposeData()
        {
            Scribe_Values.Look(ref WorkDone, "workDone");
            Scribe_Values.Look(ref WorkTotal, "workTotal");
            Scribe_References.Look(ref Caravan, "caravan");
            Scribe_Defs.Look(ref ToBuild, "toBuild");
            Scribe_Values.Look(ref Tile, "tile", -1);
            Scribe_Values.Look(ref To, "to", -1);
        }
    }

    public class RoadBuildingDef : Def
    {
        public Texture2D Icon;
        public string iconPath;
        public RoadDef road;
        public int workRequired;

        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(() => { Icon = ContentFinder<Texture2D>.Get(iconPath); });
        }
    }
}