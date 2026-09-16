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
  checklist instead of a rhythm track).
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

Skill Focus isn't published on the official Blish HUD module repo — build it from
source and drop it into your `modules` folder:

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) (net48 target, so the
   .NET Framework 4.8 targeting pack needs to be available — the regular .NET SDK
   installer on Windows brings this along).
2. Clone this repo and build it:
   ```bash
   git clone https://github.com/JonesiBlitz/gw2-skill-overlay.git
   cd gw2-skill-overlay/SkillFocus
   dotnet build
   ```
   This produces `bin/Debug/net48/SkillFocus.bhm`.
3. Close Blish HUD if it's running (it locks the module file while loaded).
4. Copy the `.bhm` file into your Blish HUD modules folder:
   ```
   Documents\Guild Wars 2\addons\blishhud\modules\
   ```
5. Start Blish HUD. Skill Focus should appear as a new corner icon (a gold crosshair)
   near the other module icons.

### Prerequisite: Dance Dance Rotation

Skill Focus reads its rotation files from Dance Dance Rotation's song folders, so install
[Dance Dance Rotation](https://blishhud.com/modules/?module=com.shooper.ddr) first (via
Blish HUD's built-in module repo) if you don't already have it — even if you never open
DDR itself, its default song pack gives Skill Focus something to load. You can also drop
your own custom song `.json` files into DDR's `customSongs` folder and they'll show up
in Skill Focus too. See DDR's own docs/its
[Song Composer tool](https://campbt.github.io/DanceDanceRotationComposer/create.html) for
how to generate a song from a dps.report log and build template.

## Usage

### 1. Pick a rotation

Click the corner icon → **Choose Rotation...**. This opens a searchable list of every
song found in DDR's `customSongs` and `defaultSongs` folders — type to filter, click one
to load it.

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
