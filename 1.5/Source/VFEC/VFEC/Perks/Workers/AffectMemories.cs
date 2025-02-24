using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEC.Perks.Workers
{
    public abstract class AffectMemories : PerkWorker
    {
        protected AffectMemories(PerkDef def) : base(def)
        {
        }

        public override IEnumerable<Patch> GetPatches()
        {
            yield return Patch.Postfix(
                AccessTools.Method(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory), new[] { typeof(Thought_Memory), typeof(Pawn) }),
                AccessTools.Method(GetType(), nameof(DoEffects))
            );
        }

        public static void DoEffects(Thought_Memory newThought)
        {
            foreach (var perkWorker in GameComponent_PerkManager.Instance.ActivePerks.Select(perk => perk.Worker).OfType<AffectMemories>()) perkWorker.DoEffect(newThought);
        }

        public abstract void DoEffect(Thought_Memory newThought);
    }

    [StaticConstructorOnStartup]
    public class Morituri : AffectMemories
    {
        public static HashSet<ThoughtDef> ToEffect = new()
        {
            ThoughtDefOf.ColonistLost,
            ThoughtDefOf.KnowColonistDied,
            ThoughtDefOf.PawnWithGoodOpinionLost,
            ThoughtDefOf.PawnWithBadOpinionLost,
            ThoughtDefOf.ColonistBanished,
            ThoughtDefOf.DeniedJoining,
            ThoughtDefOf.ColonistBanishedToDie,
            ThoughtDefOf.PrisonerBanishedToDie,
            ThoughtDefOf.WitnessedDeathAlly,
            ThoughtDefOf.WitnessedDeathNonAlly,
            ThoughtDefOf.WitnessedDeathFamily,
            ThoughtDefOf.KnowColonistDied,
            // For testing purposes
            ThoughtDefOf.DebugBad,
        };

        static Morituri()
        {
            foreach (var def in DefDatabase<PawnRelationDef>.AllDefs)
            {
                ToEffect.Add(def.diedThought);
                if (def.diedThoughtFemale != null) ToEffect.Add(def.diedThoughtFemale);
                ToEffect.Add(def.killedThought);
                if (def.killedThoughtFemale != null) ToEffect.Add(def.killedThoughtFemale);
                ToEffect.Add(def.lostThought);
                if (def.diedThoughtFemale != null) ToEffect.Add(def.lostThoughtFemale);
            }
        }

        public Morituri(PerkDef def) : base(def)
        {
        }

        public override void DoEffect(Thought_Memory newThought)
        {
            if (newThought.pawn.Faction is { IsPlayer: true } && newThought.moodOffset <= 0 && ToEffect.Contains(newThought.def))
                newThought.moodPowerFactor *= 0.5f;
        }
    }
}