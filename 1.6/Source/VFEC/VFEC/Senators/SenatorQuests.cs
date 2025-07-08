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
            root.root is QuestNode_Sequence { nodes: var nodes } && HasValidNode(nodes, IsAsker) && HasValidNode(nodes, IsRewards))
       .ToList();

    static SenatorQuests()
    {
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(RewardsGenerator), "DoGenerate"), postfix: new(typeof(SenatorQuests), nameof(AddFavorReward)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(QuestNode_GetPawn), "RunInt"), new(typeof(SenatorQuests), nameof(SetSenatorPawn)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(QuestNode_GetPawn), "TestRunInt"), new(typeof(SenatorQuests), nameof(CheckSenatorPawn)));
        ClassicMod.Harm.Patch(AccessTools.PropertyGetter(typeof(Faction), nameof(Faction.LeaderTitle)), new(typeof(SenatorQuests), nameof(ForceTitle)));
    }

    private static bool HasOrIsValidNode(QuestNode node, Func<QuestNode, bool> isValidNode) =>
        isValidNode(node) || (node is QuestNode_Sequence { nodes: { } nodes } && HasValidNode(nodes, isValidNode)) ||
        (node is QuestNode_HasRoyalTitleInCurrentFaction { node: { } trueNode, elseNode: { } falseNode } &&
         (HasOrIsValidNode(trueNode, isValidNode) || HasOrIsValidNode(falseNode, isValidNode)));

    private static bool HasValidNode(List<QuestNode> nodes, Func<QuestNode, bool> isValidNode) => nodes.Any(isValidNode);

    private static bool IsAsker(QuestNode node) => node is QuestNode_GetPawn { storeAs: var storeAs } && storeAs.ToString() == "asker";

    private static bool IsRewards(QuestNode node) => node is QuestNode_GiveRewards || (node.TryGetChild(out var child) && HasOrIsValidNode(child, IsRewards));

    private static bool TryGetChild(this QuestNode node, out QuestNode child)
    {
        switch (node)
        {
            case QuestNode_Signal { node: { } signal }:
                child = signal;
                return true;
            case QuestNode_Delay { node: { } delay }:
                child = delay;
                return true;
            case QuestNode_IsTrue { node: { } isTrue }:
                child = isTrue;
                return true;
            case QuestNode_AllSignals { node: { } allSignals }:
                child = allSignals;
                return true;
            default:
                child = null;
                return false;
        }
    }

    public static Quest GenerateQuestFor(SenatorInfo senatorInfo, Faction faction)
    {
        var pawn = senatorInfo.Pawn;
        var leader = faction.leader;
        faction.leader = pawn;
        info = new SenatorInfoWithFaction { Info = senatorInfo, Faction = faction };
        var slate = new Slate();
        slate.Set(SlateNames.Asker, pawn);
        slate.Set(SlateNames.Points, StorytellerUtility.DefaultSiteThreatPointsNow());
        var quest = QuestGen.Generate(ValidQuests.Where(root => root.CanRun(slate, Find.World)).RandomElement(), slate);
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
