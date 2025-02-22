using System.Collections.Generic;
using System.Linq;
using Verse;
using VFEC.Senators;

namespace VFEC.Perks
{
    public class GameComponent_PerkManager : GameComponent
    {
        public static GameComponent_PerkManager Instance;
        public HashSet<PerkDef> ActivePerks = new();

        public Dictionary<TickerType, List<PerkDef>> TickLists = new()
        {
            {TickerType.Normal, new List<PerkDef>()},
            {TickerType.Rare, new List<PerkDef>()},
            {TickerType.Long, new List<PerkDef>()}
        };

        public GameComponent_PerkManager(Game game) => Instance = this;

        public void AddPerk(PerkDef perk)
        {
            ActivePerks.Add(perk);
            perk.Worker.Notify_Added();
            perk.Worker.Initialize();
        }

        public PerkDef FirstPerk<T>() where T : PerkWorker
        {
            return ActivePerks.FirstOrDefault(perk => perk.Worker is T);
        }

        public void RemovePerk(PerkDef perk)
        {
            ActivePerks.Remove(perk);
            perk.Worker.Notify_Removed();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            foreach (var def in TickLists[TickerType.Normal]) def.Worker.Tick();

            if (Find.TickManager.TicksGame % 250 == 3)
                foreach (var def in TickLists[TickerType.Rare])
                    def.Worker.TickRare();

            if (Find.TickManager.TicksGame % 2000 == 6)
                foreach (var def in TickLists[TickerType.Long])
                    def.Worker.TickLong();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Remove the perk for uniting the republics if extra factions through faction
            // discovery were added and now the republics aren't united anymore.
            if (Scribe.mode == LoadSaveMode.Saving)
                foreach (var republicDef in DefDatabase<RepublicDef>.AllDefs)
                    if (ActivePerks.Contains(republicDef.perk) && !republicDef.United)
                        RemovePerk(republicDef.perk);
            Scribe_Collections.Look(ref ActivePerks, "activePerks", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                foreach (var perk in ActivePerks)
                    perk.Worker.Notify_Added();

            foreach (var perkDef in DefDatabase<PerkDef>.AllDefs.Where(def => def.needsSaving))
            {
                Scribe.EnterNode(perkDef.defName);
                try
                {
                    perkDef.Worker.ExposeData();
                }
                finally
                {
                    Scribe.ExitNode();
                }
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            foreach (var perk in ActivePerks.ToList()) RemovePerk(perk);
        }
    }
}