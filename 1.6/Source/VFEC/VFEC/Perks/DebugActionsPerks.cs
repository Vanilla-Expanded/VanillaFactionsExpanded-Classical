using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Verse;

namespace VFEC.Perks;

public static class DebugActionsPerks
{
    [DebugAction("General", "Add Perk...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void AddPerk()
    {
        var list = new List<DebugMenuOption>
        {
            new("*All", DebugMenuOptionMode.Action, () =>
            {
                foreach (var def in DefDatabase<PerkDef>.AllDefs) GameComponent_PerkManager.Instance.AddPerk(def);
            })
        };
        list.AddRange(DefDatabase<PerkDef>.AllDefs.Select(perk =>
            new DebugMenuOption(perk.LabelCap, DebugMenuOptionMode.Action, () => GameComponent_PerkManager.Instance.AddPerk(perk))));

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
    }

    [DebugAction("General", "Log Perks", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void LogPerks()
    {
        Log.Message(GameComponent_PerkManager.Instance.ActivePerks.Select(perk => perk.LabelCap.Resolve()).ToLineList("  - "));
    }
}
