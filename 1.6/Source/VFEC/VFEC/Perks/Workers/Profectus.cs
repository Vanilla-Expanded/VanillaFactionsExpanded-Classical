using System.Linq;
using RimWorld;
using Verse;

namespace VFEC.Perks.Workers
{
    public class Profectus : PerkWorker
    {
        public const int INTERVAL = 60000;
        private int nextResearchTick = -1;

        private int researchesUnlocked = -1;

        public Profectus(PerkDef def) : base(def)
        {
        }

        private int TicksTillNextResearch => (5 + researchesUnlocked) * INTERVAL;

        public override void Initialize()
        {
            base.Initialize();
            researchesUnlocked = 0;
            nextResearchTick = Find.TickManager.TicksGame + TicksTillNextResearch;
        }

        public override void TickLong()
        {
            if (Find.TickManager.TicksGame >= nextResearchTick) DoResearch();
        }

        private void DoResearch()
        {
            if (DefDatabase<ResearchProjectDef>.AllDefs.Where(CanResearch).TryRandomElement(out var research))
            {
                Find.ResearchManager.FinishProject(research);
                var faction = Find.FactionManager.FirstFactionOfDef(VFEC_DefOf.VFEC_EasternRepublic);
                Find.LetterStack.ReceiveLetter("VFEC.Letters.Researched".Translate(research.LabelCap),
                    "VFEC.Letters.Researched.Desc".Translate(research.LabelCap,
                        faction?.Name ?? VFEC_DefOf.VFEC_EasternRepublic.LabelCap),
                    LetterDefOf.PositiveEvent, null, faction);
                researchesUnlocked++;
                nextResearchTick = Find.TickManager.TicksGame + TicksTillNextResearch;
            }
            else
                nextResearchTick = Find.TickManager.TicksGame + 60000; // Check each day for new research
        }

        private static bool CanResearch(ResearchProjectDef proj) =>
            proj.TechprintCount <= 0 &&
            proj.RequiredAnalyzedThingCount <= 0 &&
            !proj.requiresMechanitor &&
            proj.knowledgeCategory == null &&
            proj.CanStartNow;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextResearchTick, "nextResearchTick");
            Scribe_Values.Look(ref researchesUnlocked, "researchesUnlocked");
        }
    }
}