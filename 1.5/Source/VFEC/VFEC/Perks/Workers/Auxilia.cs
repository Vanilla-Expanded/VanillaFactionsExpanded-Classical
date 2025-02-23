using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEC.Perks.Workers
{
    public class Auxilia : PerkWorker
    {
        public Auxilia(PerkDef def) : base(def)
        {
        }

        public override IEnumerable<Patch> GetPatches()
        {
            yield return Patch.Postfix(AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker"), AccessTools.Method(GetType(), nameof(DoSupportLegion)));
        }

        public static void DoSupportLegion(IncidentWorker_RaidEnemy __instance, IncidentParms parms, ref bool __result)
        {
            if (__result && parms.target is Map map && Rand.Chance(0.25f))
            {
                var faction = Find.FactionManager.FirstFactionOfDef(VFEC_DefOf.VFEC_WesternRepublic);
                if (faction.HostileTo(parms.faction))
                {
                    var reasons = new List<string>();

                    if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(4f).Includes(map.mapTemperature.SeasonalTemp))
                        reasons.Add("BadTemperature");
                    if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
                        reasons.Add("HostileVisitorsPresent");

                    // Spawn the support legion if nothing is blocking it
                    if (reasons.Empty())
                    {
                        parms.faction = faction;
                        __result = __result && IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
                    }
                    // List the reason the support legion can't spawn
                    else
                    {
                        var reasonsText = reasons
                            .Select(x => x.TranslateSimple())
                            .OrderBy(x => x)
                            .ToCommaList();
                        Messages.Message("VFEC.ReinforcementLegionFailed".Translate(reasonsText), MessageTypeDefOf.NeutralEvent);
                    }
                }
            }
        }
    }
}