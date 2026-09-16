using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Input;

namespace SkillFocus
{
    /// <summary>
    /// Resolves which physical key(s) are bound to each skill bar slot.
    /// GW2 allows two independent binds per action (primary + secondary), and either one
    /// triggers the skill in-game, so both are tracked here rather than picking just one.
    /// Starts from GW2's stock (primary) defaults, then overlays anything found in a
    /// GW2 keybind export (Documents\Guild Wars 2\InputBinds\*.xml). GW2's exported button
    /// codes are plain Win32 virtual-key codes, which map 1:1 onto
    /// Microsoft.Xna.Framework.Input.Keys, so no translation table is needed.
    /// </summary>
    public static class KeybindMap
    {
        private static readonly Dictionary<Slot, Keys> Defaults = new Dictionary<Slot, Keys>
        {
            { Slot.Weapon1, Keys.D1 },
            { Slot.Weapon2, Keys.D2 },
            { Slot.Weapon3, Keys.D3 },
            { Slot.Weapon4, Keys.D4 },
            { Slot.Weapon5, Keys.D5 },
            { Slot.Heal, Keys.D6 },
            { Slot.Utility1, Keys.D7 },
            { Slot.Utility2, Keys.D8 },
            { Slot.Utility3, Keys.D9 },
            { Slot.Elite, Keys.D0 },
            { Slot.WeaponSwap, Keys.OemTilde },
            { Slot.Profession1, Keys.F1 },
            { Slot.Profession2, Keys.F2 },
            { Slot.Profession3, Keys.F3 },
            { Slot.Profession4, Keys.F4 },
            { Slot.Profession5, Keys.F5 },
        };

        private static readonly Dictionary<string, Slot> ActionNameToSlot = new Dictionary<string, Slot>(StringComparer.OrdinalIgnoreCase)
        {
            { "Weapon Skill 1", Slot.Weapon1 },
            { "Weapon Skill 2", Slot.Weapon2 },
            { "Weapon Skill 3", Slot.Weapon3 },
            { "Weapon Skill 4", Slot.Weapon4 },
            { "Weapon Skill 5", Slot.Weapon5 },
            { "Healing Skill", Slot.Heal },
            { "Utility Skill 1", Slot.Utility1 },
            { "Utility Skill 2", Slot.Utility2 },
            { "Utility Skill 3", Slot.Utility3 },
            { "Elite Skill", Slot.Elite },
            { "Profession Skill 1", Slot.Profession1 },
            { "Profession Skill 2", Slot.Profession2 },
            { "Profession Skill 3", Slot.Profession3 },
            { "Profession Skill 4", Slot.Profession4 },
            { "Profession Skill 5", Slot.Profession5 },
            { "Weapon Swap", Slot.WeaponSwap },
            { "Swap Weapons", Slot.WeaponSwap },
        };

        /// <summary>
        /// Builds the slot -> valid keys map (a slot matches if the pressed key is anywhere
        /// in its set). Returns the map plus a list of slots that ended up with no keyboard
        /// key at all (e.g. both primary and secondary explicitly moved to a mouse button).
        /// </summary>
        public static (Dictionary<Slot, HashSet<Keys>> Map, List<Slot> UnsupportedDeviceSlots) Build(string inputBindsDirectory)
        {
            var map = new Dictionary<Slot, HashSet<Keys>>();
            foreach (var kv in Defaults) map[kv.Key] = new HashSet<Keys> { kv.Value };

            string exportPath = FindNewestExport(inputBindsDirectory);
            if (exportPath != null)
            {
                try
                {
                    var doc = XDocument.Load(exportPath);
                    foreach (var action in doc.Descendants("action"))
                    {
                        string name = (string)action.Attribute("name");
                        if (name == null || !ActionNameToSlot.TryGetValue(name, out Slot slot)) continue;

                        ApplyBindSlot(map[slot], (string)action.Attribute("device"), (string)action.Attribute("button"), isPrimary: true);
                        ApplyBindSlot(map[slot], (string)action.Attribute("device2"), (string)action.Attribute("button2"), isPrimary: false);
                    }
                }
                catch
                {
                    // If the export is malformed, just fall back to defaults silently.
                }
            }

            var unsupported = map.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();

            return (map, unsupported);
        }

        /// <summary>
        /// Applies one bind slot (primary or secondary) from the export onto a slot's key set.
        /// An explicit keyboard bind is added. An explicit non-keyboard bind on the PRIMARY
        /// slot removes the stock default (the user deliberately moved it away from keyboard);
        /// a non-keyboard SECONDARY bind is just ignored, since it's an extra alternate trigger
        /// alongside whatever the primary still resolves to. An absent attribute means that
        /// bind slot is untouched (still whatever it started as) and is left alone.
        /// </summary>
        private static void ApplyBindSlot(HashSet<Keys> keys, string device, string buttonCode, bool isPrimary)
        {
            if (device == null) return; // untouched - leave existing defaults/binds as-is

            if (device == "Keyboard" && buttonCode != null && TryParseKey(buttonCode, out Keys key))
            {
                keys.Add(key);
                return;
            }

            if (isPrimary && device != "None" && device != "Keyboard")
            {
                // Primary was deliberately moved off keyboard - the stock default no longer
                // applies. (Constructed once from Defaults, so remove that seeded value.)
                keys.Clear();
            }
        }

        private static string FindNewestExport(string inputBindsDirectory)
        {
            if (!Directory.Exists(inputBindsDirectory)) return null;

            return Directory.EnumerateFiles(inputBindsDirectory, "*.xml")
                             .OrderByDescending(File.GetLastWriteTimeUtc)
                             .FirstOrDefault();
        }

        private static bool TryParseKey(string code, out Keys key)
        {
            key = Keys.None;
            if (!int.TryParse(code, out int vkCode)) return false;

            key = (Keys)vkCode;
            return Enum.IsDefined(typeof(Keys), key);
        }
    }
}
