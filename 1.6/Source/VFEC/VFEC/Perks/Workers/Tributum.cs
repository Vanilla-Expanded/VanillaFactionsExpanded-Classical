using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEC.Perks.Workers;

public class Tributum : PerkWorker
{
    public const int INTERVAL = 3600000;

    private static bool shouldModify;
    private int lastTickDonated = -1;

    public Tributum(PerkDef def) : base(def)
    {
    }

    public override IEnumerable<Patch> GetPatches()
    {
        yield return new Patch(AccessTools.Method(typeof(Tradeable), "InitPriceDataIfNeeded"), AccessTools.Method(GetType(), nameof(CheckShouldModify)),
            AccessTools.Method(GetType(), nameof(ModifySellPrice)));
        yield return Patch.Postfix(AccessTools.Method(typeof(Tradeable), nameof(Tradeable.GetPriceTooltip)),
            AccessTools.Method(GetType(), nameof(AddToTooltip)));
    }

    public override void Initialize()
    {
        base.Initialize();
        lastTickDonated = Find.TickManager.TicksGame;
    }

    public override void TickLong()
    {
        if (Find.TickManager.TicksGame - lastTickDonated >= INTERVAL)
        {
            var faction = Find.FactionManager.FirstFactionOfDef(VFEC_DefOf.VFEC_CentralRepublic);
            var money = 0.05f * Find.WorldObjects.Settlements.Where(s => s.HasMap && s.Map.IsPlayerHome).Sum(s => s.Map.wealthWatcher.WealthTotal);
            var map = Find.Maps.Where(m => m.IsPlayerHome).RandomElement();
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = Mathf.FloorToInt(money);
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, Gen.YieldSingle(silver), canRoofPunch: false, forbid: false);
            Find.LetterStack.ReceiveLetter("VFEC.Letters.Donation".Translate(faction.Name),
                "VFEC.Letters.Donation.Desc".Translate(faction.Name, money.ToStringMoney()),
                LetterDefOf.PositiveEvent, silver, faction);
            lastTickDonated = Find.TickManager.TicksGame;
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref lastTickDonated, "lastTickDonated");
    }

    public static void CheckShouldModify(float ___pricePlayerBuy)
    {
        shouldModify = ___pricePlayerBuy <= 0f;
    }

    public static void ModifySellPrice(ref float ___pricePlayerBuy, ref float ___pricePlayerSell)
    {
        if (shouldModify && TradeSession.trader?.Faction is { def: var def } && def == VFEC_DefOf.VFEC_CentralRepublic)
            ___pricePlayerBuy *= 0.5f;
        // ___pricePlayerSell *= 2f;
    }

    public static void AddToTooltip(TradeAction action, ref string __result)
    {
        if (TradeSession.trader is not { Faction.def: var factionDef } || factionDef != VFEC_DefOf.VFEC_CentralRepublic || __result == null) return;
        var finalBit = __result.Split('\n').Last();
        __result = __result.Replace(finalBit, "");
        __result += GameComponent_PerkManager.Instance.FirstPerk<Tributum>().LabelCap + ": x" + action switch
        {
            TradeAction.PlayerBuys => 0.5f.ToStringPercent(),
            // TradeAction.PlayerSells => 2f.ToStringPercent(),
            _ => 1f.ToStringPercent()
        };


        __result += "\n" + finalBit;
    }
}