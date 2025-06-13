using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Verse;

namespace VFEC.Senators;

public static class DebugActionsSenators
{
    [DebugAction("General", "Gain Favor Of...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void GainFavor()
    {
        var list = new List<DebugMenuOption>();
        list.AddRange(Find.FactionManager.AllFactions.Where(faction => faction.def.HasModExtension<FactionExtension_SenatorInfo>())
           .Select(faction =>
                new DebugMenuOption(faction.Name, DebugMenuOptionMode.Action,
                    () =>
                    {
                        var list = new List<DebugMenuOption>
                        {
                            new("*All", DebugMenuOptionMode.Action, () =>
                            {
                                foreach (var info in WorldComponent_Senators.Instance.SenatorInfo[faction]
                                            .Where(info => !info.Favored && info.Pawn is not null))
                                    WorldComponent_Senators.Instance.GainFavorOf(info.Pawn, faction);
                            })
                        };
                        list.AddRange(WorldComponent_Senators.Instance.SenatorInfo[faction]
                           .Where(info => !info.Favored && info.Pawn is not null)
                           .Select(info =>
                                new DebugMenuOption(info.Pawn.Name.ToStringFull, DebugMenuOptionMode.Action,
                                    () => { WorldComponent_Senators.Instance.GainFavorOf(info.Pawn, faction); })));
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                    })));

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
    }

    [DebugAction("General", "Regenerate Senators", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void RegenerateSenators()
    {
        WorldComponent_Senators.Instance.InitFromZero();
    }
}
