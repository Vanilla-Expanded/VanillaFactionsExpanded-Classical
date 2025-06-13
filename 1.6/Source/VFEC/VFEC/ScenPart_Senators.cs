using System.Linq;
using RimWorld;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace VFEC
{
    public class ScenPart_Senators : ScenPart
    {
        // ReSharper disable once InconsistentNaming
        public int numSenators = 3;

        public override void PostGameStart()
        {
            base.PostGameStart();
            var comp = WorldComponent_Senators.Instance;
            foreach (var (faction, info, pawn) in comp.SenatorInfo.Zip(Find.GameInitData.startingAndOptionalPawns, (pair, pawn) => (pair.Key, pair.Value[0], pawn))
                .Take(numSenators))
            {
                info.Pawn = pawn;
                info.Favored = true;
                var ext = faction.def.GetModExtension<FactionExtension_SenatorInfo>();
                var perk = ext.senatorPerks[0];
                var research = ext.senatorResearch[0];
                GameComponent_PerkManager.Instance.AddPerk(perk);
                if (!research.IsFinished)
                    Find.ResearchManager.FinishProject(research, false, pawn);
            }
        }
    }
}