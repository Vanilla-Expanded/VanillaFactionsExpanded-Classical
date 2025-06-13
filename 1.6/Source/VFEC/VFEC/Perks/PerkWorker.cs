using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEC.Perks
{
    [StaticConstructorOnStartup]
    public class PerkWorker : IExposable
    {
        private static readonly HashSet<Patch> APPLIED = new();
        private static readonly byte[] cachedStatsToModify;
        private readonly Dictionary<StatDef, float> statFactors = new();
        private readonly Dictionary<StatDef, float> statOffsets = new();

        // ReSharper disable once InconsistentNaming
        public PerkDef def;

        static PerkWorker() => cachedStatsToModify = new byte[DefDatabase<StatDef>.DefCount];

        public PerkWorker(PerkDef def)
        {
            this.def = def;
            foreach (var factor in def.statFactors) statFactors.Add(factor.stat, factor.value);

            foreach (var offset in def.statOffsets) statOffsets.Add(offset.stat, offset.value);
        }

        public IEnumerable<Patch> Patches => PerkPatches.GetPatches().Concat(GetPatches());

        public virtual void ExposeData()
        {
            throw new NotImplementedException("PerkWorker.ExposeData");
        }

        public virtual void Initialize()
        {
        }

        public virtual void Notify_Added()
        {
            foreach (var patch in Patches)
                if (!APPLIED.Contains(patch))
                {
                    Log.Message($"Perk {def.LabelCap} is patching {patch}");
                    try
                    {
                        patch.Apply(ClassicMod.Harm);
                    }
                    catch (Exception error)
                    {
                        Log.Error($"Error while patching for {def.LabelCap}:\n{error}");
                    }

                    APPLIED.Add(patch);
                }


            if (def.tickerType != TickerType.Never) GameComponent_PerkManager.Instance.TickLists[def.tickerType].Add(def);
            foreach (var factor in def.statFactors) cachedStatsToModify[factor.stat.index]++;

            foreach (var offset in def.statOffsets) cachedStatsToModify[offset.stat.index]++;
        }

        public virtual bool ShouldModifyStatsOf(StatRequest req, StatDef stat) => statFactors.ContainsKey(stat) || statOffsets.ContainsKey(stat);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ModifyStat(StatRequest req, StatDef stat, ref float value)
        {
            if (!ShouldModifyStatsOf(req, stat)) return;
            if (statFactors.TryGetValue(stat, out var factor)) value *= factor;
            if (statOffsets.TryGetValue(stat, out var offset)) value += offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ModifyStatExplain(StatRequest req, StatDef stat, StatWorker worker, ref string explanation)
        {
            if (!ShouldModifyStatsOf(req, stat)) return;
            if (statFactors.TryGetValue(stat, out var factor) && Math.Abs(factor - 1f) > 0.0001f)
                explanation += "\n" + (def.LabelCap + ": " + worker.ValueToString(factor, false, ToStringNumberSense.Factor));
            if (statOffsets.TryGetValue(stat, out var offset) && offset != 0f)
                explanation += "\n" + (def.LabelCap + ": " + worker.ValueToString(offset, false, ToStringNumberSense.Offset));
        }

        public virtual void Notify_Removed()
        {
            foreach (var patch in Patches)
                if (APPLIED.Contains(patch))
                {
                    patch.Unapply(ClassicMod.Harm);
                    APPLIED.Remove(patch);
                }

            if (def.tickerType != TickerType.Never) GameComponent_PerkManager.Instance.TickLists[def.tickerType].Remove(def);
            foreach (var factor in def.statFactors) cachedStatsToModify[factor.stat.index]--;

            foreach (var offset in def.statOffsets) cachedStatsToModify[offset.stat.index]--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldModifyStatEver(StatDef stat) => cachedStatsToModify[stat.index] > 0;

        public virtual IEnumerable<Patch> GetPatches()
        {
            yield break;
        }

        public virtual void Tick()
        {
            throw new NotImplementedException("PerkWorker.Tick");
        }

        public virtual void TickRare()
        {
            throw new NotImplementedException("PerkWorker.TickRare");
        }

        public virtual void TickLong()
        {
            throw new NotImplementedException("PerkWorker.TickLong");
        }

        public struct Patch
        {
            private readonly MethodInfo target;
            private readonly MethodInfo prefix;
            private readonly MethodInfo postfix;
            private readonly MethodInfo transpiler;

            public override string ToString() =>
                $"{target.FullDescription()} with:\nPrefix: {prefix.FullDescription()}\nPostfix: {postfix.FullDescription()}\nTranspiler: {transpiler.FullDescription()}";

            public static Patch Prefix(MethodInfo target, MethodInfo prefix) => new(target, prefix);
            public static Patch Postfix(MethodInfo target, MethodInfo postfix) => new(target, postfix: postfix);
            public static Patch Transpiler(MethodInfo target, MethodInfo transpiler) => new(target, transpiler: transpiler);

            public Patch(MethodInfo target, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null)
            {
                this.target = target;
                this.prefix = prefix;
                this.postfix = postfix;
                this.transpiler = transpiler;
            }

            public void Apply(Harmony harm)
            {
                harm.Patch(target,
                    prefix is null ? null : new HarmonyMethod(prefix),
                    postfix is null ? null : new HarmonyMethod(postfix),
                    transpiler is null ? null : new HarmonyMethod(transpiler));
            }

            public void Unapply(Harmony harm)
            {
                if (prefix is not null) harm.Unpatch(target, prefix);

                if (postfix is not null) harm.Unpatch(target, postfix);

                if (transpiler is not null) harm.Unpatch(target, transpiler);
            }
        }
    }
}