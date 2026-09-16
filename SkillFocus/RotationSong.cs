using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SkillFocus
{
    public class RotationStep
    {
        public Slot Slot;
        public long AbilityId;
    }

    public class RotationSong
    {
        public string Name;
        public string Description;
        public string SourcePath;
        public List<RotationStep> Steps = new List<RotationStep>();

        /// <summary>
        /// Loads a song using the same JSON shape as Dance Dance Rotation's song files
        /// (name/description/notes[{time,duration,noteType,abilityId}]), so any existing
        /// DDR song - default or custom - can be reused as a rotation source here.
        /// Timing (time/duration) is intentionally ignored; only the ordered sequence matters.
        /// </summary>
        public static RotationSong LoadFromFile(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path));

            var song = new RotationSong
            {
                Name = (string)root["name"] ?? Path.GetFileNameWithoutExtension(path),
                Description = (string)root["description"] ?? "",
                SourcePath = path,
            };

            var notes = root["notes"] as JArray;
            if (notes == null) return song;

            foreach (var note in notes)
            {
                string noteType = (string)note["noteType"];
                Slot slot = SlotExtensions.ParseNoteType(noteType);
                if (slot == Slot.Unknown) continue;

                song.Steps.Add(new RotationStep
                {
                    Slot = slot,
                    AbilityId = (long?)note["abilityId"] ?? 0,
                });
            }

            return song;
        }

        public static List<string> DiscoverSongFiles(params string[] directories)
        {
            var files = new List<string>();
            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir)) continue;
                files.AddRange(Directory.EnumerateFiles(dir, "*.json"));
            }
            return files.OrderBy(f => Path.GetFileNameWithoutExtension(f)).ToList();
        }
    }
}
