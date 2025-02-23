using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VFEC.Buildings;

[StaticConstructorOnStartup]
public class Beacon : Building, ISizeReporter
{
    private static readonly Texture2D LightTex = ContentFinder<Texture2D>.Get("UI/Gizmos/LightTheBeacon");

    private int litTick = -1;

    private MapComponent_Beacon mapComp;

    private Sustainer sustainer;
    private int ticksTillSmoke = -1;

    public float CurrentSize() => 300f;

    public override IEnumerable<Gizmo> GetGizmos() => base.GetGizmos().Append(LightCommand());

    private Faction GetFaction()
    {
        var factions = GetFactionsInReinforcementOrder().ToList();
        // Find an allied faction that is hostile to player enemies.
        var faction = factions.FirstOrDefault(f => CanFactionReinforce(f, out _));
        // If no faction was found, repeat but skip the check for hostiles.
        // Do this in case our check for hostiles failed.
        faction ??= factions.FirstOrDefault(f => CanFactionReinforce(f, out _, false));

        return faction;
    }

    private string GetFactionUnavailableReasons(bool checkForHostiles = true)
    {
        var factions = GetFactionsInReinforcementOrder()
            .Select(f => (canReinforce: CanFactionReinforce(f, out var reason, checkForHostiles), reason))
            .ToList();

        // Return null, there's an ally available
        if (factions.Any(x => x.canReinforce))
            return null;
        // No allies available at all
        if (factions.All(x => x.reason.NullOrEmpty()))
            return "VFEC.NoAllies".Translate();

        // List all reasons allies can't arrive (in alphabetical order to avoid flickering tooltips).
        var reasons = factions
            .SelectMany(x => x.reason)
            .Distinct()
            .Select(x => x.TranslateSimple())
            .OrderBy(x => x)
            .ToCommaList()
            .CapitalizeFirst();
        return $"{"VFEC.NoReinforce".Translate()} ({reasons}.)";
    }

    private IEnumerable<Faction> GetFactionsInReinforcementOrder()
    {
        // List of all the republics in other they should reinforce.
        // Used to iterate over it and exclude from non-republic faction search.
        var republics = new[]
        {
            VFEC_DefOf.VFEC_WesternRepublic,
            VFEC_DefOf.VFEC_CentralRepublic,
            VFEC_DefOf.VFEC_EasternRepublic,
        };

        // Go through all republics
        foreach (var factionDef in republics)
        {
            var faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction != null)
                yield return faction;
        }

        // Go through all other valid factions in random order
        foreach (var f in Find.FactionManager.GetFactions().Where(f => !republics.Contains(f.def)).InRandomOrder())
            yield return f;
    }

    private bool CanFactionReinforce(Faction faction, out List<string> reason, bool checkForHostiles = true)
    {
        reason = new List<string>();

        // Make sure allied, and don't report any reasons
        if (faction.PlayerRelationKind != FactionRelationKind.Ally)
            return false;

        // Make sure that the allies can arrive based on the map temperature
        if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(4f).Includes(Map.mapTemperature.SeasonalTemp))
            reason.Add("BadTemperature");

        // Make sure there's no lord preventing reinforcements
        if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(Map, faction))
            reason.Add("HostileVisitorsPresent");

        if (checkForHostiles)
        {
            var anyHostiles = Map.attackTargetsCache.TargetsHostileToColony
                .Where(GenHostility.IsActiveThreatToPlayer)
                .Select(x => ((Thing)x)?.Faction)
                .Any(x => x != null && x.HostileTo(faction));
            // Make sure there's any hostiles on the map
            if (!anyHostiles)
                reason.Add("VFEC.NoHostiles");
        }

        // If the list is empty then the faction can reinforce
        return reason.Empty();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        mapComp = map.GetComponent<MapComponent_Beacon>();
    }

    private Command LightCommand()
    {
        var command = new Command_Action
        {
            defaultLabel = "VFEC.LightTheBeacon".Translate(),
            defaultDesc = "VFEC.LightTheBeacon.Desc".Translate(),
            icon = LightTex,
            action = delegate
            {
                litTick = Find.TickManager.TicksGame;
                mapComp.Notify_BeaconLit();
                IncidentDefOf.RaidFriendly.Worker.TryExecute(GetIncidentParms());
            }
        };

        if (!mapComp.CanLightBeacon()) command.Disable("VFEC.Cooldown".Translate());
        else if (GetFactionUnavailableReasons(false) is { } reason) command.Disable(reason);
        // else if (!IncidentDefOf.RaidFriendly.Worker.CanFireNow(GetIncidentParms())) command.Disable("VFEC.NoReinforce".Translate());

        return command;
    }

    private IncidentParms GetIncidentParms() =>
        new()
        {
            faction = GetFaction(),
            target = Map,
            points = StorytellerUtility.DefaultThreatPointsNow(Map)
        };

    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        if (phase == DrawPhase.Draw)
        {
            if (def.drawerType == DrawerType.RealtimeOnly || !Spawned) Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this);

            SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);

            if (litTick > 0) Comps_PostDraw();
        }
    }

    public override void Tick()
    {
        base.Tick();
        if (litTick > 0)
        {
            if (ticksTillSmoke <= 0)
            {
                FleckMaker.ThrowSmoke(DrawPos, Map, 3f);
                FleckMaker.ThrowFireGlow(Position.ToVector3Shifted(), Map, 0.01f);
                ticksTillSmoke = Mathf.RoundToInt(10f * Rand.Value);
            }

            ticksTillSmoke--;

            if (sustainer != null)
                sustainer.Maintain();
            else if (!Position.Fogged(Map))
            {
                var info = SoundInfo.InMap(new(Position, Map), MaintenanceType.PerTick);
                sustainer = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor(this, SoundDefOf.FireBurning, info);
            }

            if (Find.TickManager.TicksGame >= litTick + MapComponent_Beacon.BEACON_COOLDOWN) Destroy(DestroyMode.KillFinalize);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref litTick, "litTick");
        Scribe_Values.Look(ref ticksTillSmoke, "ticksTillSmoke");
    }
}

public class MapComponent_Beacon : MapComponent
{
    public const int BEACON_COOLDOWN = 180000;
    private int lastBeaconTick = -BEACON_COOLDOWN * 2;

    public MapComponent_Beacon(Map map) : base(map)
    {
    }

    public void Notify_BeaconLit()
    {
        lastBeaconTick = Find.TickManager.TicksGame;
    }

    public bool CanLightBeacon() => lastBeaconTick + BEACON_COOLDOWN <= Find.TickManager.TicksGame;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref lastBeaconTick, "lastBeaconTick");
    }
}