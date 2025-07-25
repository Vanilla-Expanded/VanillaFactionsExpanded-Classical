using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace VFEC.Senators;

[StaticConstructorOnStartup]
public static class SenatorQuests
{
    private static SenatorInfoWithFaction? info;

    private static readonly Func<QuestNode_GetPawn, Pawn, Slate, bool> isGoodPawn = (Func<QuestNode_GetPawn, Pawn, Slate, bool>)AccessTools
       .Method(typeof(QuestNode_GetPawn), "IsGoodPawn")
       .CreateDelegate(typeof(Func<QuestNode_GetPawn, Pawn, Slate, bool>));

    private static readonly List<QuestScriptDef> ValidQuests = DefDatabase<QuestScriptDef>.AllDefs.Where(root =>
            root.root is QuestNode_Sequence { nodes: var nodes } && root.IsRootRandomSelected && HasValidNode(nodes, IsAsker))
       .ToList();

    static SenatorQuests()
    {
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(RewardsGenerator), "DoGenerate"), postfix: new(typeof(SenatorQuests), nameof(AddFavorReward)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(QuestNode_GetPawn), "RunInt"), new(typeof(SenatorQuests), nameof(SetSenatorPawn)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(QuestNode_GetPawn), "TestRunInt"), new(typeof(SenatorQuests), nameof(CheckSenatorPawn)));
        ClassicMod.Harm.Patch(AccessTools.PropertyGetter(typeof(Faction), nameof(Faction.LeaderTitle)), new(typeof(SenatorQuests), nameof(ForceTitle)));
    }

    private static bool HasOrIsValidNode(QuestNode node, Func<QuestNode, bool> isValidNode)
    {
        if (node == null) return false;
        if (isValidNode(node)) return true;

        return node switch
        {
            QuestNode_Sequence s when s.nodes != null => HasValidNode(s.nodes, isValidNode),
            QuestNode_RandomNode r when r.nodes != null => HasValidNode(r.nodes, isValidNode),
            QuestNode_Chance c => HasOrIsValidNode(c.node, isValidNode) || HasOrIsValidNode(c.elseNode, isValidNode),
            QuestNode_HasRoyalTitleInCurrentFaction r => HasOrIsValidNode(r.node, isValidNode) || HasOrIsValidNode(r.elseNode, isValidNode),
            QuestNode_IsSet iset => HasOrIsValidNode(iset.node, isValidNode) || HasOrIsValidNode(iset.elseNode, isValidNode),
            QuestNode_Signal sig => HasOrIsValidNode(sig.node, isValidNode),
            QuestNode_Delay del => HasOrIsValidNode(del.node, isValidNode),
            QuestNode_IsTrue it => HasOrIsValidNode(it.node, isValidNode),
            QuestNode_AllSignals asig => HasOrIsValidNode(asig.node, isValidNode),
            _ => false
        };
    }

    private static bool HasValidNode(List<QuestNode> nodes, Func<QuestNode, bool> isValidNode) => nodes.Any(node => HasOrIsValidNode(node, isValidNode));

    private static bool IsAsker(QuestNode node) => node is QuestNode_GetPawn { storeAs: var storeAs } && storeAs.ToString() == "asker";

    public static Quest GenerateQuestFor(List<QuestScriptDef> quests, Slate slate, SenatorInfo senatorInfo, Faction faction)
    {
        var pawn = senatorInfo.Pawn;
        var leader = faction.leader;
        faction.leader = pawn;
        info = new SenatorInfoWithFaction { Info = senatorInfo, Faction = faction };
        var quest = QuestGen.Generate(quests.RandomElement(), slate);
        info = null;
        faction.leader = leader;
        return quest;
    }

    public static void AddFavorReward(ref List<Reward> __result)
    {
        if (info is not null) __result.Add(new Reward_SenatorFavor(info.Value));
    }

    public static bool SetSenatorPawn(QuestNode_GetPawn __instance)
    {
        if (info is null) return true;
        if (__instance.storeAs.GetValue(QuestGen.slate) != "asker") return true;
        QuestGen.slate.Set(__instance.storeAs.GetValue(QuestGen.slate), info.Value.Info.Pawn);
        return false;
    }

    public static bool CheckSenatorPawn(QuestNode_GetPawn __instance, Slate slate, ref bool __result)
    {
        if (info is null) return true;
        if (__instance.storeAs.GetValue(QuestGen.slate) != "asker") return true;
        __result = isGoodPawn(__instance, info.Value.Info.Pawn, slate);
        return false;
    }

    public static bool ForceTitle(Faction __instance, ref string __result)
    {
        if (info is null) return true;
        if (__instance != info.Value.Faction) return true;
        __result = info.Value.Info.Pawn.KindLabel;
        return false;
    }

    public static List<QuestScriptDef> GetValidQuestsFrom(Pawn pawn, out Slate slate)
    {
        var slate2 = new Slate();
        slate2.Set(SlateNames.Asker, pawn);
        slate2.Set("askerIsNull", var: false);
        slate2.Set(SlateNames.Points, StorytellerUtility.DefaultSiteThreatPointsNow());
        slate = slate2;
        var validQuests = new List<QuestScriptDef>();
        foreach (var root in ValidQuests)
        {
            if (root.CanRun(slate2, Find.World))
            {
                validQuests.Add(root);
            }
        }
        return validQuests;
    }

    public struct SenatorInfoWithFaction
    {
        public SenatorInfo Info;
        public Faction Faction;
    }
}

public class Reward_SenatorFavor : Reward
{
    private Faction faction;

    private Pawn pawn;

    public Reward_SenatorFavor(SenatorQuests.SenatorInfoWithFaction info)
    {
        pawn = info.Info.Pawn;
        faction = info.Faction;
    }

    public Reward_SenatorFavor() { }

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements =>
        Gen.YieldSingle(QuestPartUtility.GetStandardRewardStackElement(
            "VFEC.Senators.FavorOf".Translate(pawn.Name.ToStringFull),
            rect => GUI.DrawTexture(rect, PortraitsCache.Get(pawn, rect.size, Rot4.South)),
            () => "VFEC.Senators.FavorTip".Translate(pawn.Name.ToStringFull),
            () => Find.WindowStack.Add(new Dialog_InfoCard(pawn))));

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        valueActuallyUsed = pawn.MarketValue;
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText,
        RulePack customLetterLabelRules,
        RulePack customLetterTextRules)
    {
        yield return new QuestPart_GainFavor(pawn, faction);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_References.Look(ref faction, "faction");
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "VFEC.Senators.GainFavor".Translate(pawn.Name.ToStringFull, faction.Name);
}

public class QuestPart_GainFavor : QuestPart
{
    private Faction faction;

    private string inSignal;
    private Pawn pawn;

    public QuestPart_GainFavor() { }

    public QuestPart_GainFavor(Pawn pawn, Faction faction)
    {
        this.pawn = pawn;
        this.faction = faction;
        inSignal = QuestGen.slate.Get<string>("inSignal");
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref faction, "faction");
        Scribe_References.Look(ref pawn, "pawn");
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal) WorldComponent_Senators.Instance.GainFavorOf(pawn, faction);
    }
}