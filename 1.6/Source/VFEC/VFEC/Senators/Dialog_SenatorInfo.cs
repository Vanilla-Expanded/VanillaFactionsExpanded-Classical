using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using VFEC.Perks;
using VEF.Utils;

namespace VFEC.Senators;

[StaticConstructorOnStartup]
public class Dialog_SenatorInfo : Window
{
    private static readonly Color DisplayBGColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 15);
    private readonly bool canInteract;
    private readonly PerkDef finalPerk;
    private readonly ResearchProjectDef finalResearch;

    private readonly float[] heights = new float[4];

    private readonly Texture2D perkBG_Unlocked;
    private readonly List<AllSenatorInfo> senatorInfo;
    public Caravan Caravan;
    public Faction Faction;
    private int curIdx;

    private float moneyNeeded = 1000f + 0.05f * WorldComponent_Senators.Instance.NumBribes * 0.5f *
        Find.WorldObjects.Settlements.Where(s => s.HasMap && s.Map.IsPlayerHome).Sum(s => s.Map.wealthWatcher.WealthTotal);

    private RenderTexture pawnTexture;

    public Dialog_SenatorInfo(FactionExtension_SenatorInfo senatorInfo1, List<SenatorInfo> senatorInfo2, bool canInteract = true)
    {
        senatorInfo = new();
        finalPerk = senatorInfo1.finalPerk;
        finalResearch = senatorInfo1.finalResearch;
        perkBG_Unlocked = senatorInfo1.PerkBG;
        for (var i = 0; i < senatorInfo1.numSenators; i++)
            senatorInfo.Add(AllSenatorInfo.FromSenatorInfo(senatorInfo2[i], senatorInfo1.senatorPerks[i], senatorInfo1.senatorResearch[i]));
        doCloseButton = false;
        doCloseX = false;
        forcePause = true;
        this.canInteract = canInteract;
    }

    protected override float Margin => 12f;
    public override Vector2 InitialSize => new(1150, 800);

    private Texture2D PerkBG(bool locked) => locked ? SenatorUIUtility.PerkBG_Locked : perkBG_Unlocked;

    public override void DoWindowContents(Rect inRect)
    {
        var font = Text.Font;
        var anchor = Text.Anchor;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(inRect.TakeTopPart(40f), "VFEC.UI.SenatorsOf".Translate(Faction.Name));
        foreach (var (info, rect) in senatorInfo.Zip(UIUtility.Divide(inRect.TakeTopPart(575f), senatorInfo.Count, senatorInfo.Count, 1, false),
                     (info, rect) => (info, rect.ContractedBy(7f, 0))))
            DrawSenatorInfo(info, rect);

        Text.Anchor = TextAnchor.UpperLeft;
        var allInfo = inRect.TakeLeftPart(250f);
        Text.Font = GameFont.Small;
        Widgets.Label(allInfo.TakeTopPart(40f), "VFEC.UI.GainingFavorAll".Translate());
        Text.Anchor = TextAnchor.MiddleLeft;
        DoRewardsInfo(allInfo.TakeTopPart(60f), finalPerk, finalResearch);

        DoPerkInfo(inRect.TakeLeftPart(100f), finalPerk, !senatorInfo.All(info => info.Favored));
        inRect.xMin += 20f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        var textRect = inRect.TakeLeftPart(500f);
        Widgets.Label(textRect.TakeTopPart(20f), "VFEC.UI.SenatorsInFavor".Translate(senatorInfo.Count(info => info.Favored), senatorInfo.Count));
        Text.Font = GameFont.Tiny;
        Widgets.Label(textRect, "VFEC.UI.Info".Translate().Colorize(ColoredText.SubtleGrayColor));

        Text.Font = GameFont.Small;
        if (Widgets.ButtonText(inRect.TakeBottomPart(40f).ContractedBy(10f, 0), "Close".Translate())) Close();

        Text.Font = font;
        Text.Anchor = anchor;
    }

    private void Column() => curIdx = 0;

    private void Label(ref Rect inRect, string text)
    {
        heights[curIdx] = Mathf.Max(heights[curIdx], Text.CalcHeight(text, inRect.width));
        Widgets.Label(inRect.TakeTopPart(heights[curIdx]), text);
        ++curIdx;
    }

    private void DrawSenatorInfo(AllSenatorInfo info, Rect inRect)
    {
        Column();
        var portraitRect = inRect.TakeTopPart(200f);
        Widgets.DrawBoxSolid(portraitRect, DisplayBGColor);
        portraitRect = portraitRect.ContractedBy(3f);
        if (info.Pawn is not null)
        {
            var pawnTex = GetPawnTexture(info.Pawn, portraitRect.size, Rot4.South);
            GUI.DrawTexture(portraitRect, pawnTex);

            Text.Font = GameFont.Medium;
            Label(ref inRect, info.Pawn.Name.ToStringFull);

            Text.Font = GameFont.Tiny;
            Label(ref inRect, "PawnMainDescFactionedWrap".Translate(VFEC_DefOf.VFEC_RepublicSenator.LabelCap, Faction.Name));
        }
        else inRect.yMin += 50f;

        Widgets.DrawLineHorizontal(inRect.x, inRect.y, inRect.width);
        inRect.yMin += 7f;

        Text.Font = GameFont.Small;
        if (info.Favored || !canInteract) inRect.yMin += 80f;
        else
        {
            if (Widgets.ButtonText(inRect.TakeTopPart(40f).ContractedBy(10f, 0f), "VFEC.UI.ReQuest".Translate()))
            {
                var canGetQuest = true;
                if (info.Quest is not null)
                {
                    if (info.Quest.State is not QuestState.Ongoing and not QuestState.NotYetAccepted)
                        info.Quest = null;
                    else canGetQuest = false;
                }

                if (canGetQuest)
                {
                    var info2 = WorldComponent_Senators.Instance.InfoFor(info.Pawn, Faction);
                    info.Quest = info2.Quest = SenatorQuests.GenerateQuestFor(info2, Faction);
                    Find.QuestManager.Add(info2.Quest);
                    QuestUtility.SendLetterQuestAvailable(info2.Quest);
                }
                else
                    Messages.Message("VFEC.UI.AlreadyQuest".Translate(), MessageTypeDefOf.RejectInput, false);
            }

            if (info.CanBribe)
            {
                if (Widgets.ButtonText(inRect.TakeTopPart(40f).ContractedBy(10f, 0f), "VFEC.UI.Bribe".Translate(moneyNeeded)))
                {
                    if (CaravanInventoryUtility.HasThings(Caravan, ThingDefOf.Silver, Mathf.CeilToInt(moneyNeeded)))
                    {
                        if (Rand.Chance(0.15f))
                        {
                            Messages.Message("VFEC.UI.BribeReject".Translate(info.Pawn.Name.ToStringFull), MessageTypeDefOf.RejectInput);
                            info.CanBribe = false;
                            WorldComponent_Senators.Instance.InfoFor(info.Pawn, Faction).CanBribe = false;
                        }
                        else
                        {
                            var remaining = Mathf.CeilToInt(moneyNeeded);
                            CaravanInventoryUtility.TakeThings(Caravan, thing =>
                                {
                                    if (thing.def != ThingDefOf.Silver) return 0;

                                    var num = Mathf.Min(remaining, thing.stackCount);
                                    remaining -= num;
                                    return num;
                                })
                               .ForEach(t => t.Destroy());
                            info.Favored = true;
                            WorldComponent_Senators.Instance.NumBribes++;
                            moneyNeeded = 1000f + 0.05f * WorldComponent_Senators.Instance.NumBribes * 0.5f *
                                Find.WorldObjects.Settlements.Where(s => s.HasMap && s.Map.IsPlayerHome).Sum(s => s.Map.wealthWatcher.WealthTotal);
                            WorldComponent_Senators.Instance.GainFavorOf(info.Pawn, Faction);
                        }
                    }
                    else Messages.Message("VFEC.UI.NotEnoughMoney".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            else inRect.yMin += 40f;
        }

        Text.Font = GameFont.Small;
        Label(ref inRect, "VFEC.UI.GainingFavor".Translate());

        heights[curIdx] = Mathf.Max(heights[curIdx],
            Text.CalcHeight(info.Perk.LabelCap, inRect.width) + 30f + Text.CalcHeight(info.Research.LabelCap, inRect.width));
        DoRewardsInfo(inRect.TakeTopPart(heights[curIdx]), info.Perk, info.Research);

        DoPerkInfo(inRect.TakeTopPart(100f).ContractedBy((inRect.width - 100f) / 2, 0f), info.Perk, !info.Favored);
    }

    private void DoPerkInfo(Rect inRect, PerkDef perk, bool locked)
    {
        Widgets.DrawTextureFitted(inRect, PerkBG(locked), 1f);
        Widgets.DrawTextureFitted(inRect.ContractedBy(10f), perk.Icon, 1f);
        TooltipHandler.TipRegion(inRect, $"{perk.LabelCap}\n\n{perk.description}");
    }

    private void DoRewardsInfo(Rect inRect, PerkDef perk, ResearchProjectDef research)
    {
        Text.Font = GameFont.Tiny;
        Widgets.Label(inRect.TopHalf(), "VFEC.UI.Perk".Translate(perk.LabelCap).Colorize(ColoredText.SubtleGrayColor));
        Widgets.Label(inRect.BottomHalf(), "VFEC.UI.Research".Translate(research.LabelCap).Colorize(ColoredText.SubtleGrayColor));
    }

    public RenderTexture GetPawnTexture(Pawn pawn, Vector2 size, Rot4 rotation, Vector3 cameraOffset = default, float cameraZoom = 1f,
        bool supersample = true,
        bool compensateForUIScale = true, bool renderHeadgear = true, bool renderClothes = true, Dictionary<Apparel, Color> overrideApparelColors = null,
        Color? overrideHairColor = null, bool stylingStation = false)
    {
        if (supersample) size *= 1.25f;

        if (compensateForUIScale) size *= Prefs.UIScale;

        var angle = 0f;
        var positionOffset = default(Vector3);
        if (pawn.Dead || pawn.Downed)
        {
            angle = 85f;
            positionOffset.x -= 0.18f;
            positionOffset.z -= 0.18f;
        }

        var renderTexture = NewRenderTexture(size);
        Find.PawnCacheRenderer.RenderPawn(pawn, renderTexture, cameraOffset, cameraZoom, angle, rotation, pawn.health.hediffSet.HasHead,
            renderHeadgear, renderClothes, false, positionOffset, overrideApparelColors, overrideHairColor, stylingStation);
        return renderTexture;
    }

    private RenderTexture NewRenderTexture(Vector2 size)
    {
        if (pawnTexture is null)
            pawnTexture = new((int)size.x, (int)size.y, 24)
            {
                name = "Portrait",
                useMipMap = false,
                filterMode = FilterMode.Bilinear
            };

        return pawnTexture;
    }

    private class AllSenatorInfo : SenatorInfo
    {
        public PerkDef Perk;
        public ResearchProjectDef Research;

        public static AllSenatorInfo FromSenatorInfo(SenatorInfo info, PerkDef perk, ResearchProjectDef research) =>
            new()
            {
                Pawn = info.Pawn,
                Favored = info.Favored,
                CanBribe = info.CanBribe,
                Quest = info.Quest,
                Perk = perk,
                Research = research
            };
    }
}
