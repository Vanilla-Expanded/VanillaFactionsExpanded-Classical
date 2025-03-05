using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEC.Senators;

[StaticConstructorOnStartup]
public static class SenatorUIUtility
{
    public static Texture2D PerkBG_Locked = ContentFinder<Texture2D>.Get("UI/Perks/PerkBG_Locked");
    public static Texture2D PerkBG_United = ContentFinder<Texture2D>.Get("UI/Perks/PerkBG_United");

    static SenatorUIUtility()
    {
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(FactionUIUtility), nameof(FactionUIUtility.DoWindowContents)),
            postfix: new HarmonyMethod(typeof(SenatorUIUtility), nameof(DoPerkButton)));
        ClassicMod.Harm.Patch(AccessTools.Method(typeof(FactionUIUtility), "DrawFactionRow"),
            new HarmonyMethod(typeof(SenatorUIUtility), nameof(DoSenatorInfoButton)));
    }

    public static void DoPerkButton()
    {
        var anyPerk = DefDatabase<RepublicDef>.AllDefs
            .Any(republicDef => republicDef.parts
                .Any(factionDef => Find.FactionManager.FirstFactionOfDef(factionDef) != null));

        if (anyPerk && Widgets.ButtonText(new Rect(0, 10f, 120f, 30f), "VFEC.UI.ViewPerks".Translate())) Find.WindowStack.Add(new Dialog_PerkInfo());
    }

    public static void DoSenatorInfoButton(Faction faction, ref Rect fillRect, float rowY)
    {
        if (faction.ShouldHaveSenators())
        {
            fillRect.width -= 130f;
            if (Widgets.ButtonText(new Rect(fillRect.width + 5f, rowY + 25f, 120f, 30f), "VFEC.UI.ViewSenators".Translate()))
            {
                WorldComponent_Senators.Instance.CheckInit();
                Find.WindowStack.Add(new Dialog_SenatorInfo(faction.def.GetModExtension<FactionExtension_SenatorInfo>(),
                    WorldComponent_Senators.Instance.SenatorInfo[faction],
                    false)
                {
                    Faction = faction
                });
            }
        }
    }

    public static bool ShouldHaveSenators(this Faction faction) =>
        faction.def.HasModExtension<FactionExtension_SenatorInfo>() && !faction.Hidden && !faction.temporary;
}