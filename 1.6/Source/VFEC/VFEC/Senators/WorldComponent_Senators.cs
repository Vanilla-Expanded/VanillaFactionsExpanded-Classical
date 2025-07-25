using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using VFEC.Perks;

namespace VFEC.Senators;

[StaticConstructorOnStartup]
public class WorldComponent_Senators : WorldComponent
{
    public static WorldComponent_Senators Instance;

    private List<FactionInfos> infos = new();
    private bool initialized;
    public int NumBribes;
    public Dictionary<Faction, bool> Permanent = new();

    public Dictionary<Faction, List<SenatorInfo>> SenatorInfo = new();

    private HashSet<Faction> united = new();

    static WorldComponent_Senators()
    {
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(Settlement), nameof(Settlement.GetFloatMenuOptions)),
            postfix: new HarmonyMethod(typeof(WorldComponent_Senators), nameof(AddSenatorsOption)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
            postfix: new HarmonyMethod(typeof(WorldComponent_Senators), nameof(Notify_PawnDied)));
        ClassicMod.Harm.Patch(AccessTools.PropertyGetter(typeof(Faction), nameof(Faction.Color)),
            postfix: new HarmonyMethod(typeof(WorldComponent_Senators), nameof(OverrideColor)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.GoodwillWith)),
            postfix: new HarmonyMethod(typeof(WorldComponent_Senators), nameof(LockGoodwill1)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.CanChangeGoodwillFor)),
            postfix: new HarmonyMethod(typeof(WorldComponent_Senators), nameof(LockGoodwill2)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.RelationKindWith)),
            postfix: new HarmonyMethod(typeof(WorldComponent_Senators), nameof(ForceAlly)));
    }

    public WorldComponent_Senators(World world) : base(world) => Instance = this;

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        if (!initialized) InitFromZero();
    }

    public void InitFromZero()
    {
        SenatorInfo.Clear();
        Permanent.Clear();
        CheckInit();
        initialized = true;
    }

    public void CheckInit()
    {
        foreach (var faction in world.factionManager.AllFactions)
            if (faction.ShouldHaveSenators())
            {
                if (SenatorInfo.ContainsKey(faction) != Permanent.ContainsKey(faction))
                {
                    Log.Warning("[VFE - Classical] SenatorInfo and Permanent dictionaries are out of sync! Clearing data.");
                    SenatorInfo.Remove(faction);
                    Permanent.Remove(faction);
                }
                else if (SenatorInfo.ContainsKey(faction)) continue;

                SenatorInfo.Add(faction, Enumerable.Repeat((false, true), faction.def.GetModExtension<FactionExtension_SenatorInfo>().numSenators).Select(
                    info =>
                        new SenatorInfo
                        {
                            Pawn = GenerateSenator(faction),
                            Favored = info.Item1,
                            CanBribe = info.Item2,
                            Quest = null
                        }).ToList());
                Permanent.Add(faction, false);
            }
    }

    public Pawn GenerateSenator(Faction faction)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(VFEC_DefOf.VFEC_RepublicSenator, faction, forceGenerateNewPawn: true));
        world.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        return pawn;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref NumBribes, "numBribes");
        Scribe_Values.Look(ref initialized, "initialized");
        Scribe_Collections.Look(ref united, "united", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            infos.Clear();
            infos.AddRange(SenatorInfo.Select(kv => new FactionInfos
            {
                Faction = kv.Key,
                Infos = kv.Value,
                Permanent = Permanent[kv.Key]
            }));
        }

        Scribe_Collections.Look(ref infos, "infos", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            SenatorInfo = infos.ToDictionary(info => info.Faction, info => info.Infos);
            Permanent = infos.ToDictionary(info => info.Faction, info => info.Permanent);
        }
    }

    public static IEnumerable<FloatMenuOption> AddSenatorsOption(IEnumerable<FloatMenuOption> options, Settlement __instance, Caravan caravan) =>
        __instance.Faction.ShouldHaveSenators() && __instance.Tile == caravan.Tile
            ? options.Append(new FloatMenuOption("VFEC.Senators.Open".Translate(), delegate
            {
                Instance.CheckInit();
                Find.WindowStack.Add(new Dialog_SenatorInfo(__instance.Faction.def.GetModExtension<FactionExtension_SenatorInfo>(),
                    Instance.SenatorInfo[__instance.Faction])
                {
                    Caravan = caravan,
                    Faction = __instance.Faction
                });
            }))
            : options;

    public void GainFavorOf(Pawn pawn, Faction faction)
    {
        var info = InfoFor(pawn, faction);
        info.Favored = true;
        var ext = faction.def.GetModExtension<FactionExtension_SenatorInfo>();
        var perk = ext.senatorPerks[SenatorInfo[faction].IndexOf(info)];
        var research = ext.senatorResearch[SenatorInfo[faction].IndexOf(info)];
        var letterLabel = "VFEC.Letters.SenatorJoins".Translate(pawn.Name.ToStringFull);
        var letterDesc = "VFEC.Letters.SenatorJoins.Desc".Translate(pawn.Name.ToStringFull, faction.Name, perk.LabelCap);
        GameComponent_PerkManager.Instance.AddPerk(perk);
        if (!research.IsFinished)
        {
            Find.ResearchManager.FinishProject(research, false, pawn);
            letterDesc += " ";
            letterDesc += "VFEC.Letters.SenatorJoins.Desc.Research".Translate(research.LabelCap);
        }

        if (SenatorInfo[faction].All(i => i.Favored))
        {
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 1000, reason: VFEC_DefOf.VFEC_GainedFavor);
            Permanent[faction] = true;
            var finalPerk = ext.finalPerk;
            var finalResearch = ext.finalResearch;
            GameComponent_PerkManager.Instance.AddPerk(finalPerk);
            letterDesc += " ";
            letterDesc += "VFEC.Letters.SenatorJoins.Desc.All".Translate(faction.Name, finalPerk.LabelCap);
            if (!finalResearch.IsFinished)
            {
                Find.ResearchManager.FinishProject(finalResearch, false, pawn);
                letterDesc += " ";
                letterDesc += "VFEC.Letters.SenatorJoins.Desc.All.Research".Translate(ext.numSenators, finalResearch.LabelCap);
            }

            if (faction.ideos is not null && Faction.OfPlayer.ideos is not null) faction.ideos.SetPrimary(Faction.OfPlayer.ideos.PrimaryIdeo);

            foreach (var republicDef in DefDatabase<RepublicDef>.AllDefs)
                if (republicDef.parts.Contains(faction.def) && republicDef.United)
                {
                    GameComponent_PerkManager.Instance.AddPerk(republicDef.perk);
                    Find.LetterStack.ReceiveLetter(republicDef.letterLabel,
                        republicDef.letterText + "\n" + "VFEC.PerkUnlocked".Translate(republicDef.perk.LabelCap),
                        LetterDefOf.PositiveEvent);
                    foreach (var factionDef in republicDef.parts) united.Add(Find.FactionManager.FirstFactionOfDef(factionDef));
                }

            var cachedMat = AccessTools.FieldRefAccess<Settlement, Material>("cachedMat");
            foreach (var settlement in Find.WorldObjects.Settlements.Where(settlement => settlement.Faction == faction))
                cachedMat(settlement) = null;
        }

        pawn.SetFaction(Faction.OfPlayer);
        if (pawn.ideo is not null && Faction.OfPlayer.ideos is not null)
            pawn.ideo.SetIdeo(Faction.OfPlayer.ideos.PrimaryIdeo);

        var parms = new IncidentParms { target = Find.Maps.Where(m => m.IsPlayerHome && m.Tile.LayerDef.isSpace is false).RandomElement(), spawnCenter = IntVec3.Invalid };
        PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(parms);
        PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(new List<Pawn> { pawn }, parms);

        Find.LetterStack.ReceiveLetter(letterLabel, letterDesc, LetterDefOf.PositiveEvent, pawn, faction, info.Quest);
    }

    public bool United(Faction faction) => united.Contains(faction);

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % 60000 == 25)
            foreach (var (faction, senatorInfos) in SenatorInfo)
            {
                var perm = Permanent[faction];
                foreach (var senatorInfo in senatorInfos)
                {
                    if (!senatorInfo.Favored && senatorInfo.Pawn == null)
                    {
                        Log.Warning("[VFE - Classical] Found senator that is not favored with no pawn, fixing");
                        senatorInfo.Pawn = GenerateSenator(faction);
                    }

                    if (!perm && senatorInfo.Favored && (senatorInfo.Pawn == null || senatorInfo.Pawn.Dead))
                    {
                        Log.Warning("[VFE - Classical] Found dead favored senator but not permanent, fixing");
                        senatorInfo.Pawn = GenerateSenator(faction);
                        senatorInfo.Favored = false;
                        var ext = faction.def.GetModExtension<FactionExtension_SenatorInfo>();
                        var perk = ext.senatorPerks[SenatorInfo[faction].IndexOf(senatorInfo)];
                        GameComponent_PerkManager.Instance.RemovePerk(perk);
                        Find.LetterStack.ReceiveLetter("VFEC.Letters.SenatorLost".Translate("VFEC.Unknown".Translate()),
                            "VFEC.Letters.SenatorLost.Desc".Translate("VFEC.Unknown".Translate(), faction.Name, perk.LabelCap), LetterDefOf.NegativeEvent,
                            null,
                            faction);
                    }
                }
            }
    }

    public static void Notify_PawnDied(Pawn __instance)
    {
        var info = Instance.SenatorInfo.SelectMany(kv => kv.Value.Where(senator => __instance == senator.Pawn)).FirstOrDefault();
        if (info is null) return;
        var faction = Instance.SenatorInfo.FirstOrDefault(kv => kv.Value.Contains(info)).Key;
        if (Instance.Permanent[faction])
            info.Pawn = null;
        else
        {
            info.Pawn = Instance.GenerateSenator(faction);
            info.Favored = false;
            var ext = faction.def.GetModExtension<FactionExtension_SenatorInfo>();
            var perk = ext.senatorPerks[Instance.SenatorInfo[faction].IndexOf(info)];
            GameComponent_PerkManager.Instance.RemovePerk(perk);
            Find.LetterStack.ReceiveLetter("VFEC.Letters.SenatorLost".Translate(__instance.Name.ToStringFull),
                "VFEC.Letters.SenatorLost.Desc".Translate(__instance.Name.ToStringFull, faction.Name, perk.LabelCap), LetterDefOf.NegativeEvent, __instance,
                faction);
        }
    }

    public SenatorInfo InfoFor(Pawn pawn, Faction faction)
    {
        return SenatorInfo[faction].Find(i => i.Pawn == pawn);
    }

    public static void OverrideColor(Faction __instance, ref Color __result)
    {
        if (Instance.Permanent.TryGetValue(__instance, out var perm) && perm) __result = Faction.OfPlayer.Color;
    }

    public static void LockGoodwill1(Faction __instance, Faction other, ref int __result)
    {
        if (__instance.IsPlayer && Instance.Permanent.TryGetValue(other, out var perm1) && perm1) __result = 100;
        if (other.IsPlayer && Instance.Permanent.TryGetValue(__instance, out var perm2) && perm2) __result = 100;
    }

    public static void LockGoodwill2(Faction __instance, Faction other, ref bool __result)
    {
        if (__instance.IsPlayer && Instance.Permanent.TryGetValue(other, out var perm1) && perm1) __result = false;
        if (other.IsPlayer && Instance.Permanent.TryGetValue(__instance, out var perm2) && perm2) __result = false;
    }

    public static void ForceAlly(Faction __instance, Faction other, ref FactionRelationKind __result)
    {
        if (__instance.IsPlayer && Instance.Permanent.TryGetValue(other, out var perm1) && perm1) __result = FactionRelationKind.Ally;
        if (other.IsPlayer && Instance.Permanent.TryGetValue(__instance, out var perm2) && perm2) __result = FactionRelationKind.Ally;
    }

    private class FactionInfos : IExposable
    {
        public Faction Faction;
        public List<SenatorInfo> Infos;
        public bool Permanent;

        public void ExposeData()
        {
            Scribe_References.Look(ref Faction, "faction");
            Scribe_Collections.Look(ref Infos, "infos", LookMode.Deep);
            Scribe_Values.Look(ref Permanent, "permanent");
        }
    }
}

public class RepublicDef : Def
{
    public string letterLabel;
    public string letterText;
    public List<FactionDef> parts;
    public PerkDef perk;

    public bool United
    {
        get
        {
            if (WorldComponent_Senators.Instance == null || Find.FactionManager == null)
                return false;

            var activeFactions = parts
                    .Select(part => Find.FactionManager.FirstFactionOfDef(part))
                    .Where(faction => faction != null)
                    .ToList();

            // True if there's at least 1 faction and all the factions are united
            return activeFactions.Any() &&
                   activeFactions.All(faction => WorldComponent_Senators.Instance.Permanent.TryGetValue(faction, out var perm) && perm);
        }
    }
}

public class SenatorInfo : IExposable
{
    public bool CanBribe;
    public bool Favored;
    public Pawn Pawn;
    public Quest Quest;

    public void ExposeData()
    {
        Scribe_References.Look(ref Pawn, "pawn");
        Scribe_References.Look(ref Quest, "quest");
        Scribe_Values.Look(ref Favored, "favored");
        Scribe_Values.Look(ref CanBribe, "canBribe");
    }
}