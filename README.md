# Skill Focus

A [Blish HUD](https://blishhud.com/) module that helps you learn a Guild Wars 2 rotation
at your own pace. Instead of a separate scrolling window (like
[Dance Dance Rotation](https://github.com/campbt/DanceDanceRotation)), it draws a
highlight box directly on top of the skill you need to press next, right on your real
skill bar — so you never have to look away from it. It only advances when you press the
correct key. There's no timer and no penalty for a wrong key; it just waits.

## Why this exists

Rhythm-game-style rotation trainers are great for practicing *speed*, but they're not
great for learning a rotation for the first time — you end up watching the trainer
window instead of your skill bar, and the metronome pace punishes hesitation before
you've learned the sequence. Skill Focus is meant for the "get the sequence right first"
phase: no clock, no separate window, just "is this the next correct skill, yes or no."

## Features

- **Overlay on your real skill bar** — a pulsing highlight box sits on the expected
  skill's icon. It's fully click-through, so it never interferes with actually playing.
- **Self-paced** — advances only on a correct keypress. Wrong keys are ignored, not
  penalized.
- **Reuses Dance Dance Rotation's song files** — Skill Focus reads rotations straight out
  of DDR's `customSongs`/`defaultSongs` folders, so if you already have DDR installed you
  already have a library of rotations to use here (DDR's song format has the ordered
  skill sequence baked in; Skill Focus just ignores the timing and treats it as a
  checklist instead of a rhythm track). DDR isn't required, though — Skill Focus also has
  its own rotations folder, with a corner-menu shortcut to open it and an "Add Rotation
  from Clipboard" option for pasting a song straight in.
- **Auto-detects your keybinds** — reads your keybind export
  (`Documents\Guild Wars 2\InputBinds\*.xml`) and layers it over Guild Wars 2's stock
  defaults, so it knows what to actually wait for instead of assuming 1-5/6/7-9/0.
  Both a primary and secondary bind on the same action are accepted, matching how GW2
  itself lets either one trigger the skill.
- **Per-profession/spec layouts** — the skill bar's width (and therefore where each
  slot sits on screen) changes with the number of profession-mechanic buttons a build
  has, so one calibration doesn't fit every spec. Skill Focus reads your current
  profession + specialization from Mumble Link and automatically switches to the
  matching saved layout, or lets you pick one manually from the corner icon menu.
- **Searchable rotation picker** — a scrollable, filterable panel for choosing a
  rotation, instead of a giant context menu that runs off the screen when you have 100+
  songs installed.

## Installation

Skill Focus isn't published on the official Blish HUD module repo, but you don't need to
build it yourself — grab the pre-built module from
[the latest release](https://github.com/JonesiBlitz/gw2-skill-overlay/releases/latest):

1. Download `SkillFocus.bhm` from the release's Assets.
2. Close Blish HUD if it's running (it locks the module file while loaded).
3. Copy the `.bhm` file into your Blish HUD modules folder:
   ```
   Documents\Guild Wars 2\addons\blishhud\modules\
   ```
4. Start Blish HUD. New modules install disabled by default, so open the Blish HUD
   settings menu → **Manage Modules**, find **Skill Focus** in the list, and enable it.
5. Skill Focus should now appear as a new corner icon (a gold crosshair) near the other
   module icons.

### Building from source instead

If you'd rather build it yourself (or want to modify it):

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) (net48 target, so the
   .NET Framework 4.8 targeting pack needs to be available — the regular .NET SDK
   installer on Windows brings this along).
2. Clone this repo and build it:
   ```bash
   git clone https://github.com/JonesiBlitz/gw2-skill-overlay.git
   cd gw2-skill-overlay/SkillFocus
   dotnet build -c Release
   ```
   This produces `bin/Release/net48/SkillFocus.bhm`.
3. Follow steps 2-5 above using that file instead of the downloaded one.

### Getting rotation files

Skill Focus reads rotations from three folders, in this order of preference: its own
folder, then Dance Dance Rotation's `customSongs`, then DDR's `defaultSongs`. You have a
couple of options for getting rotations into it:

- **Install [Dance Dance Rotation](https://blishhud.com/modules/?module=com.shooper.ddr)**
  (via Blish HUD's built-in module repo) — even if you never open DDR itself, its
  bundled default song pack gives Skill Focus a full library to load immediately, and any
  custom songs you drop into DDR's `customSongs` folder show up in Skill Focus too.
- **Or skip DDR entirely** and use Skill Focus's own folder: corner icon →
  **Open Rotations Folder** opens
  `Documents\Guild Wars 2\addons\blishhud\skillfocus-data\customSongs\` directly, or
  corner icon → **Add Rotation from Clipboard** parses whatever song JSON you have copied
  and saves it there for you (same idea as DDR's own "Add from Clipboard").

Either way, rotation files are just JSON in the DDR song format (an ordered list of
`{time, duration, noteType, abilityId}` notes — Skill Focus only cares about the order
and `noteType`).

### Making your own rotation

The easiest way to generate a rotation file from scratch is DDR's own
[Song Composer tool](https://campbt.github.io/DanceDanceRotationComposer/create.html),
which builds one from a real combat log rather than hand-typing skill names:

1. **Get a dps.report log of the rotation you want to learn.** The simplest source is a
   benchmark log — either record your own on the golem in the
   [Special Forces Training Area](https://blishhud.com/docs/user/faqs/) with
   [arcdps](https://www.deltaconnected.com/arcdps/) running (arcdps can auto-upload to
   dps.report if configured, or upload the resulting `.zevtc` file manually at
   [dps.report](https://dps.report/)), or grab an existing benchmark log linked from a
   build guide (e.g. [Snow Crows](https://snowcrows.com/) build pages link a "DPS Report"
   for their benchmark). Either way you need the resulting `https://dps.report/...` link.
2. **Get the matching build template chat code.** In-game, open your build/equipment
   panel and use its "copy build template to clipboard" button, or copy the code straight
   off a build guide page that lists one.
3. **Fill in the Composer form**: a name, a short description, the dps.report link, and
   the build template code, then hit Submit. It parses the log and generates the song
   JSON automatically.
4. **Copy the result** with the Composer's "Copy Song to Clipboard" button.
5. **In-game**, corner icon → **Add Rotation from Clipboard**. It'll parse what's on your
   clipboard, save it into Skill Focus's own rotations folder, and load it immediately.

One gotcha we ran into ourselves: arcdps can't record anything that happens *before*
combat officially starts, so a pre-cast opener (buffs/utilities used right before
engaging) can end up missing from the generated song, silently starting mid-rotation
instead. If that happens, you can hand-edit the JSON's `notes` array to prepend the
missing steps — each note just needs a `noteType` (`Weapon1`-`Weapon5`, `Heal`,
`Utility1`-`Utility3`, `Elite`, `Profession1`-`Profession5`, or `WeaponSwap`) and an
`abilityId` (cosmetic only, not used for matching); `time`/`duration` can be anything
since Skill Focus ignores them entirely.

## Usage

### 1. Pick a rotation

Click the corner icon → **Choose Rotation...**. This opens a searchable list of every
song found across Skill Focus's own folder and DDR's `customSongs`/`defaultSongs`
folders — type to filter, click one to load it.

### 2. Calibrate your skill bar (one-time per build)

Guild Wars 2 doesn't expose the skill bar's on-screen position to overlays, so the first
time you use a given profession/specialization, you need to tell Skill Focus where your
skill icons actually are:

1. Corner icon → **Calibrate Skill Bar**.
2. A labeled, draggable box appears for every skill bar slot (weapon 1-5, heal,
   utility 1-3, elite, profession skills, weapon swap). Drag each one onto the matching
   real skill icon.
3. Corner icon → **Finish Calibration (Save Positions)**.

This calibration is saved per (profession, specialization) pair and is picked up
automatically any time you swap to that build again — see below.

### 3. Play

Once a rotation is loaded and your current build has a saved layout, the highlight box
appears on the first skill. Press the corresponding key (whatever GW2 has it bound to;
Skill Focus figures this out on its own) and the box jumps to the next skill in the
sequence. Keep going — it loops back to the start when it reaches the end.

### Layouts (multiple specs/builds)

Corner icon → **Layouts** lists every profession/spec you've calibrated so far. Skill
Focus switches between them automatically when it detects (via Mumble Link) that you've
changed characters or swapped your equipped build template. If you swap to something you
haven't calibrated yet, it'll tell you instead of silently showing a stale layout — just
run **Calibrate Skill Bar** again for the new build. You can also pick a layout manually
from this menu if you want to override the auto-detected one.

### Rescan Keybinds

If you change your keybinds in-game, re-export them (**Options → Controls → Export**,
bottom-right of the Controls panel) so a fresh `InputBinds\*.xml` file exists, then click
corner icon → **Rescan Keybinds**.

## Known limitations

- **Mouse-button binds aren't detected.** If a skill is bound to a mouse button GW2
  itself sees as a distinct mouse input (not a keyboard key sent by mouse software like
  Logitech's), Skill Focus can't currently track it and will auto-skip that step in the
  rotation instead of stalling on it. A one-time warning notification lists which slots
  this applies to. (If your mouse software remaps buttons to send actual keystrokes —
  e.g. Logitech G-series "keystroke" button assignments — those work fine, since GW2 and
  Skill Focus both just see a normal key press.)
- **Calibration is resolution/UI-scale specific.** If you change your GW2 resolution or
  UI scale, your saved layouts' positions will be off — recalibrate for that
  profession/spec again.
- **Not published to the Blish HUD module repo** — install from source per above.

## Building from source / development

Standard Blish HUD module project — see
[Blish HUD's module development docs](https://blishhud.com/docs/modules/overview/getting-started/)
for the general workflow (Visual Studio + the `launchSettings.json` debug-attach flow
works here too). The project targets `net48` and depends on the `BlishHUD` NuGet package.

```
SkillFocus/
  manifest.json           module metadata
  SkillFocusModule.cs      module lifecycle, settings, rotation tracking
  KeybindMap.cs            resolves GW2 keybinds from InputBinds export + stock defaults
  RotationSong.cs          loads DDR-format song JSON as an ordered skill sequence
  Layout.cs                per-profession/spec calibrated skill bar positions
  HighlightControl.cs      the click-through highlight box drawn on the skill bar
  CalibrationMarker.cs     the draggable marker used during calibration
  SongPickerPanel.cs       searchable/scrollable rotation picker
  Slot.cs                  skill bar slot enum, shared with DDR's noteType naming
```

## License

No license file yet — treat as all-rights-reserved until one is added.
