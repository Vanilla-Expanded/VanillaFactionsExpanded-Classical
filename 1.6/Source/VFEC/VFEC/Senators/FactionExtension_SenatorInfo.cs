using System.Collections.Generic;
using UnityEngine;
using Verse;
using VFEC.Perks;

namespace VFEC.Senators
{
    public class FactionExtension_SenatorInfo : DefModExtension
    {
        public PerkDef finalPerk;
        public ResearchProjectDef finalResearch;
        public int numSenators;
        private Texture2D perkBG;
        public string perkBGPath;
        public List<PerkDef> senatorPerks;
        public List<ResearchProjectDef> senatorResearch;
        public Texture2D PerkBG => perkBG ??= ContentFinder<Texture2D>.Get(perkBGPath);

        public override IEnumerable<string> ConfigErrors()
        {
            if (senatorPerks is null) yield return "senatorPerks must not be null";
            if (senatorResearch is null) yield return "senatorResearch must not be null";
            if (numSenators <= 0) yield return "numSenators must be greater than 0";
            if (finalPerk is null) yield return "finalPerk must not be null";
            if (finalResearch is null) yield return "finalResearch must not be null";
            if (perkBGPath.NullOrEmpty()) yield return "Must provide perkBGPath";
            if (senatorPerks is not null && senatorPerks.Count != numSenators) yield return "senatorPerks.Count must be equal to numSenators";
            if (senatorResearch is not null && senatorResearch.Count != numSenators) yield return "senatorResearch.Count must be equal to numSenators";
        }
    }
}