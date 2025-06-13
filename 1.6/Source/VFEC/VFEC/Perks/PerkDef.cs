using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace VFEC.Perks
{
    public class PerkDef : Def
    {
        private Texture2D icon;
        public bool needsSaving;
        public List<StatModifier> statFactors = new();
        public List<StatModifier> statOffsets = new();
        public string texPath;
        public TickerType tickerType;
        private PerkWorker worker;
        public Type workerClass = typeof(PerkWorker);
        public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(texPath);
        public PerkWorker Worker => worker ??= (PerkWorker) Activator.CreateInstance(workerClass, this);
    }
}