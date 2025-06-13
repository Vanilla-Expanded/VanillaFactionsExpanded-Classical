using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFEC.Buildings;

[StaticConstructorOnStartup]
public class Scorpion : MinifiedThing, IEquipGizmos
{
    private static readonly Texture2D SetupTex = ContentFinder<Texture2D>.Get("UI/Gizmos/SetUpScorpion");

    public override Graphic Graphic => DefaultGraphic;

    public override string LabelNoCount => def.label;

    public override string DescriptionDetailed => def.DescriptionDetailed;

    public override string DescriptionFlavor => def.description;

    public override ModContentPack ContentSource => def.modContentPack;

    public IEnumerable<Gizmo> GetEquipGizmos(Pawn pawn)
    {
        var des = new Designator_InstallScorpion
        {
            icon = SetupTex,
            Scorpion = this,
            GiveJobTo = pawn
        };

        if (pawn.Faction is not { IsPlayer: true }) des.Disable("CannotOrderNonControlled".Translate());

        yield return des;
    }

    public override void PostMake()
    {
        base.PostMake();
        InnerThing ??= ThingMaker.MakeThing(VFEC_DefOf.VFEC_Turret_Scorpion);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        InnerThing ??= ThingMaker.MakeThing(VFEC_DefOf.VFEC_Turret_Scorpion);
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        InnerThing ??= ThingMaker.MakeThing(VFEC_DefOf.VFEC_Turret_Scorpion);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        Graphic.Draw(drawLoc, Rotation, this);
    }

    public override void Print(SectionLayer layer)
    {
        Graphic.Print(layer, this, 0f);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            if (gizmo is Designator_Install)
                yield return new Designator_InstallScorpion
                {
                    icon = SetupTex,
                    Scorpion = this
                };
            else
                yield return gizmo;
    }
}

public class Designator_InstallScorpion : Designator_Place
{
    public Pawn GiveJobTo;
    public Scorpion Scorpion;
    public override string Label => "VFEC.Scorpion.SetUp".Translate();
    public override BuildableDef PlacingDef => Scorpion.GetInnerIfMinified().def;
    public override ThingStyleDef ThingStyleDefForPreview => Scorpion.GetInnerIfMinified().StyleDef;
    public override ThingDef StuffDef => Scorpion.GetInnerIfMinified().Stuff;

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map)) return false;

        return c.GetThingList(Map).Find(x => x.Position == c && x.Rotation == placingRot && x.def == PlacingDef) != null
            ? new("IdenticalThingExists".Translate())
            : GenConstruct.CanPlaceBlueprintAt(PlacingDef, c, placingRot, Map, false, Scorpion, Scorpion.GetInnerIfMinified());
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        GenSpawn.WipeExistingThings(c, placingRot, PlacingDef.installBlueprintDef, Map, DestroyMode.Deconstruct);
        var blueprint = GenConstruct.PlaceBlueprintForInstall(Scorpion, c, Map, placingRot, Faction.OfPlayer);
        GiveJobTo?.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFEC_DefOf.VFEC_SetupScorpion, null, blueprint),
            GiveJobTo.Drafted ? JobTag.DraftedOrder : JobTag.UnloadingOwnInventory);

        FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, placingRot, PlacingDef.Size), Map);
        Find.DesignatorManager.Deselect();
    }

    protected override void DrawGhost(Color ghostCol)
    {
        var baseGraphic = Scorpion.Graphic.ExtractInnerGraphicFor(Scorpion);
        GhostDrawer.DrawGhostThing(UI.MouseCell(), placingRot, (ThingDef)PlacingDef, baseGraphic, ghostCol, AltitudeLayer.Blueprint, Scorpion, true,
            StuffDef);
    }
}

[StaticConstructorOnStartup]
public class ScorpionTurret : Building_TurretGun
{
    private static readonly Texture2D PickupTex = ContentFinder<Texture2D>.Get("UI/Gizmos/PickUpScorpion");

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var des = Find.ReverseDesignatorDatabase.Get<Designator_Uninstall>();
        foreach (var gizmo in base.GetGizmos())
            if (gizmo is not Designator_Install)
                yield return gizmo;

        var report = des.CanDesignateThing(this);
        yield return new Command_Action
        {
            defaultLabel = "VFEC.Scorpion.PickUp".Translate(),
            icon = PickupTex,
            Disabled = !report.Accepted,
            disabledReason = report.Reason,
            Order = -10f,
            action = () =>
            {
                des.DesignateThing(this);
                des.Finalize(true);
            }
        };
    }
}

public interface IEquipGizmos
{
    public IEnumerable<Gizmo> GetEquipGizmos(Pawn pawn);
}
