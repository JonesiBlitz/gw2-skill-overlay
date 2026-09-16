using System.Collections.Generic;

namespace SkillFocus
{
    /// <summary>
    /// A single calibrated set of skill bar positions, keyed by profession+specialization
    /// so different classes/specs (which have different numbers of profession-mechanic
    /// buttons, shifting the whole bar) can each have their own saved layout.
    /// </summary>
    public class Layout
    {
        public string Key;
        public string DisplayName;
        public Dictionary<Slot, PointData> Positions = new Dictionary<Slot, PointData>();
    }

    /// <summary>
    /// Plain serializable stand-in for Microsoft.Xna.Framework.Point.
    /// Point's X/Y are public fields, which Json.NET does not serialize by default -
    /// using a small class with properties avoids that trap.
    /// </summary>
    public class PointData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
