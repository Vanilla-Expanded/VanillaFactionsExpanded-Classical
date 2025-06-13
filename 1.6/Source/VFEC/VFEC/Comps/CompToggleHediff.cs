using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEC.Comps
{
    public class CompToggleHediff : ThingComp
    {
        private bool enabled;
        private Pawn pawn;
        public CompProperties_ToggleHediff Props => props as CompProperties_ToggleHediff;

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var gizmo in base.CompGetWornGizmosExtra()) yield return gizmo;
            if (pawn is null) yield break;

            var command = new Command_Toggle
            {
                defaultLabel = Props.label,
                defaultDesc = Props.description,
                icon = parent.def.uiIcon,
                defaultIconColor = parent.DrawColor,
                iconAngle = parent.def.uiIconAngle,
                iconDrawScale = parent.def.uiIconScale,
                iconOffset = parent.def.uiIconOffset,
                isActive = () => enabled,
                toggleAction = delegate
                {
                    if (enabled)
                        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff));
                    else
                        pawn.health.AddHediff(Props.hediff);
                    enabled = !enabled;
                }
            };
            if (pawn.Faction != Faction.OfPlayer) command.Disable("CannotOrderNonControlled".Translate());

            yield return command;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref enabled, "enabled");
            Scribe_References.Look(ref pawn, "pawn");
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (enabled)
                pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff));
            enabled = false;
            this.pawn = null;
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            this.pawn = pawn;
            pawn.health.AddHediff(Props.hediff);
            enabled = true;
        }
    }

    public class CompProperties_ToggleHediff : CompProperties
    {
        public string description;
        public HediffDef hediff;
        public string label;
        public CompProperties_ToggleHediff() => compClass = typeof(CompToggleHediff);
    }
}