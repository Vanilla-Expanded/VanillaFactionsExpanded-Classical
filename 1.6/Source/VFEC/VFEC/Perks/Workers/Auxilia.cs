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

        public static void DoSupportLegion(IncidentWorker_RaidEnemy __instance, IncidentParms parms, bool __result)
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
                        // Create the parms ourselves as re-using the ones used by the raid
                        // may end up with unintended side effects, like having "canSteal"
                        // or "canKidnap" set to true. Similarly, the raid we're copying
                        // could have forced biocoded apparel/weapons, which won't make
                        // much sense given western republic's tech level.
                        var friendlyParms = new IncidentParms
                        {
                            target = parms.target,
                            faction = faction,
                            points = parms.points,
                        };

                        if (!IncidentDefOf.RaidFriendly.Worker.TryExecute(friendlyParms))
                            Log.Warning($"Auxilia perk attempted to spawn reinforcements but failed.\nOriginal raid parms: {parms}\nFriendly raid parms: {friendlyParms}");
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