using System.Linq;
using Verse;

namespace VFEC
{
    public class DamageWorker_ApparelDamage : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            if (victim is Pawn {apparel: var apparelTracker} && apparelTracker.WornApparel.Where(t => t.def.useHitPoints).TryRandomElement(out var apparel))
                return apparel.TakeDamage(dinfo);
            return new DamageResult();
        }
    }
}