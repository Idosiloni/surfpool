using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A piece of park dressing that comes and goes with the park level (see <see cref="ParkGlowUp"/>): flags, string
    /// lights and crowds appear as the park grows; weeds, trash bags and the old crooked sign disappear.
    /// Nothing is saved: whether it shows is worked out from the park level and owned upgrades.
    /// </summary>
    public class GlowItem : MonoBehaviour
    {
        [Tooltip("Shows from this park level on.")]
        public int fromLevel = 1;
        [Tooltip("Hidden from this park level on (0 = never hidden). Use it for run-down junk that gets cleaned up.")]
        public int untilLevel;
        [Tooltip("Only shows once this upgrade is owned. Empty = no upgrade needed.")]
        public string requires = "";

        Vector3 baseScale;
        bool scaleKnown;

        public Vector3 BaseScale
        {
            get
            {
                if (!scaleKnown)
                {
                    baseScale = transform.localScale;
                    scaleKnown = true;
                }
                return baseScale;
            }
        }

        public bool ShouldShow(int level, UpgradeShop shop)
        {
            if (level < fromLevel) return false;
            if (untilLevel > 0 && level >= untilLevel) return false;
            return string.IsNullOrEmpty(requires) || (shop != null && shop.IsOwned(requires));
        }
    }
}
