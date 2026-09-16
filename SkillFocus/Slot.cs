namespace SkillFocus
{
    /// <summary>
    /// Maps 1:1 to the "noteType" strings used by the Dance Dance Rotation song format,
    /// so existing DDR song files (ours and the community defaults) can be reused directly.
    /// </summary>
    public enum Slot
    {
        Weapon1,
        Weapon2,
        Weapon3,
        Weapon4,
        Weapon5,
        Heal,
        Utility1,
        Utility2,
        Utility3,
        Elite,
        Profession1,
        Profession2,
        Profession3,
        Profession4,
        Profession5,
        WeaponSwap,
        Unknown
    }

    public static class SlotExtensions
    {
        public static Slot ParseNoteType(string noteType)
        {
            switch (noteType)
            {
                case "Weapon1": return Slot.Weapon1;
                case "Weapon2": return Slot.Weapon2;
                case "Weapon3": return Slot.Weapon3;
                case "Weapon4": return Slot.Weapon4;
                case "Weapon5": return Slot.Weapon5;
                case "Heal": return Slot.Heal;
                case "Utility1": return Slot.Utility1;
                case "Utility2": return Slot.Utility2;
                case "Utility3": return Slot.Utility3;
                case "Elite": return Slot.Elite;
                case "Profession1": return Slot.Profession1;
                case "Profession2": return Slot.Profession2;
                case "Profession3": return Slot.Profession3;
                case "Profession4": return Slot.Profession4;
                case "Profession5": return Slot.Profession5;
                case "WeaponSwap": return Slot.WeaponSwap;
                default: return Slot.Unknown;
            }
        }

        public static string ShortLabel(this Slot slot)
        {
            switch (slot)
            {
                case Slot.Weapon1: return "1";
                case Slot.Weapon2: return "2";
                case Slot.Weapon3: return "3";
                case Slot.Weapon4: return "4";
                case Slot.Weapon5: return "5";
                case Slot.Heal: return "Heal";
                case Slot.Utility1: return "Util 1";
                case Slot.Utility2: return "Util 2";
                case Slot.Utility3: return "Util 3";
                case Slot.Elite: return "Elite";
                case Slot.Profession1: return "F1";
                case Slot.Profession2: return "F2";
                case Slot.Profession3: return "F3";
                case Slot.Profession4: return "F4";
                case Slot.Profession5: return "F5";
                case Slot.WeaponSwap: return "Swap";
                default: return "?";
            }
        }
    }
}
