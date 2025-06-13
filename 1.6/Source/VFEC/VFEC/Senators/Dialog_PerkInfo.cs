using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFEC.Perks;

namespace VFEC.Senators
{
    public class Dialog_PerkInfo : Window
    {
        private readonly List<RepublicData> info;

        public Dialog_PerkInfo()
        {
            info = DefDatabase<RepublicDef>.AllDefs.Select(republicDef => new RepublicData(republicDef)).ToList();
            doCloseButton = true;
            forcePause = true;
            doCloseX = true;
        }

        public override Vector2 InitialSize => new(1000f, info.Sum(v => v.factions.Count * 200f + CloseButSize.y));

        public override void DoWindowContents(Rect inRect)
        {
            var font = Text.Font;
            var anchor = Text.Anchor;
            var y = inRect.y;
            foreach (var (republic, factions, united) in info)
            {
                var initialHeight = y;
                var x = inRect.x;
                foreach (var (faction, perkBg, perks, finalPerk, finalActive) in factions)
                {
                    x = inRect.x;
                    var rect = new Rect(x, y, 500f, 50f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    Widgets.Label(rect, faction.Name);
                    y += 60f;
                    foreach (var (perk, active) in perks) DoPerkInfo(ref x, y, perk, active, perkBg);

                    Widgets.DrawLine(new Vector2(x, y), new Vector2(x, y + 100f), finalActive ? faction.Color : Color.gray, 3f);
                    x += 15f;
                    DoPerkInfo(ref x, y, finalPerk, finalActive, perkBg);
                    y += 110f;
                }

                var middle = (y - initialHeight) / 2;
                var color = united ? Faction.OfPlayer.Color : Color.gray;
                Widgets.DrawLine(new Vector2(x, initialHeight), new Vector2(x + 20f, middle), color, 3f);
                Widgets.DrawLine(new Vector2(x, y), new Vector2(x + 20f, middle), color, 3f);
                x += 30f;
                DoPerkInfo(ref x, middle - 50f, republic.perk, united, SenatorUIUtility.PerkBG_United);
            }

            Text.Font = font;
            Text.Anchor = anchor;
        }

        private static void DoPerkInfo(ref float x, float y, PerkDef perk, bool active, Texture2D perkBG)
        {
            var rect = new Rect(x, y, 100f, 100f);
            Widgets.DrawTextureFitted(rect, active ? perkBG : SenatorUIUtility.PerkBG_Locked, 1f);
            Widgets.DrawTextureFitted(rect, perk.Icon, 1f);
            TooltipHandler.TipRegion(rect, $"{perk.LabelCap}\n\n{perk.description}");
            x += 110f;
        }

        private class RepublicData(RepublicDef republicDef)
        {
            public readonly RepublicDef republic = republicDef;
            public readonly List<FactionData> factions = republicDef.parts.Select(factionDef => new FactionData(factionDef)).Where(data => data.faction != null).ToList();
            public bool United => GameComponent_PerkManager.Instance.ActivePerks.Contains(republic.perk);

            public void Deconstruct(out RepublicDef republic, out List<FactionData> factions, out bool united)
            {
                republic = this.republic;
                factions = this.factions;
                united = United;
            }
        }

        private class FactionData(FactionDef factionDef, FactionExtension_SenatorInfo ext)
        {
            public readonly Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            public readonly Texture2D perkBg = ext.PerkBG;
            public readonly List<PerkData> perks = ext.senatorPerks.Select(perkDef => new PerkData(perkDef)).ToList();
            public readonly PerkDef finalPerk = ext.finalPerk;
            public bool FinalActive => GameComponent_PerkManager.Instance.ActivePerks.Contains(finalPerk);

            public FactionData(FactionDef factionDef) : this(factionDef, factionDef.GetModExtension<FactionExtension_SenatorInfo>())
            {
            }

            public void Deconstruct(out Faction faction, out Texture2D perkBg, out List<PerkData> perks, out PerkDef finalPerk, out bool finalActive)
            {
                faction = this.faction;
                perkBg = this.perkBg;
                perks = this.perks;
                finalPerk = this.finalPerk;
                finalActive = FinalActive;
            }
        }

        private class PerkData(PerkDef perkDef)
        {
            public readonly PerkDef perk = perkDef;
            public bool Active => GameComponent_PerkManager.Instance.ActivePerks.Contains(perk);

            public void Deconstruct(out PerkDef perk, out bool active)
            {
                perk = this.perk;
                active = Active;
            }
        }
    }
}