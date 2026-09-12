# Unity setup

How to go from a fresh clone to a running, committed Unity project. One-time
per machine; the commit wave at the end is one-time for the repo.

The project is **code-only by design**: no scenes or art are required to run
it. `Boot` (`unity/Assets/_Project/Game/GameApp.cs`) spawns the game via
`RuntimeInitializeOnLoadMethod` in whatever scene is open, and an editor
script creates and assigns the URP pipeline asset on first open
(`unity/Assets/_Project/Editor/RenderPipelineSetup.cs`). There is nothing to
stub or import by hand.

## 1. Open the project

1. Unity Hub → **Add** → **Add project from disk** → select the **`unity/`
   folder**, not the repo root. (The repo root holds docs, the dotnet mirror,
   and the original cocos2d-x source; Hub won't recognize it.)
2. Open with the editor pinned in `unity/ProjectSettings/ProjectVersion.txt`
   (currently **6000.5.10f1**) or any newer Unity 6 patch — accept the
   upgrade prompt if Hub offers one.
3. Let the first import finish. It generates `Library/` (ignored) and a
   `.meta` file next to every asset (committed — see §4). The Console should
   show `[setup] URP assigned (Assets/Settings/UniversalRP.asset)` and no
   errors.
4. The editor may rewrite `Packages/manifest.json` (URP's version is tied to
   the editor in Unity 6) and `ProjectSettings/ProjectVersion.txt`. Both
   rewrites are correct; commit them.

Sanity check under **Edit → Project Settings → Editor**: Asset Serialization
= **Force Text**, Version Control = **Visible Meta Files**. These are Unity 6
defaults; just confirm nothing changed them.

## 2. Scene

Play mode needs no scene, but a build needs at least one, and a committed
scene gives everyone the same starting point:

1. **File → New Scene** → *Empty* template.
2. Save as **`Assets/_Project/Scenes/Main.unity`** (create the folder).
   Leave it empty — `Boot` creates the camera and everything else. In
   particular don't add a second camera or EventSystem; the game draws and
   hit-tests procedurally.
3. **File → Build Profiles** (Unity 6's Build Settings) → **Scene List** →
   add `Main.unity` as the only entry.
4. Set the **Game view** to a portrait resolution (e.g. 1080×2340) and press
   **Play** — you should land on the main menu.

## 3. Tests

**Window → General → Test Runner → EditMode → Run All.** Everything should be
green. Headless equivalent:

```sh
Unity -batchmode -runTests -testPlatform EditMode -projectPath unity
```

The suites locate golden data by walking up from the project to
`docs/test_vectors.json`, so the `unity/` folder must stay inside the repo
checkout — don't copy it elsewhere.

## 4. Commit the generated wave

After first open + scene creation, commit exactly this (the `.gitignore`
already fences off the rest):

| Commit | Why |
|---|---|
| `unity/Assets/**/*.meta` | GUIDs — must be identical on every machine |
| `unity/Assets/_Project/Scenes/Main.unity` (+ `.meta`) | the scene from §2 |
| `unity/Assets/Settings/` (URP assets + metas) | pipeline created on first open |
| `unity/Packages/manifest.json`, `packages-lock.json` | editor-resolved package set |
| `unity/ProjectSettings/*` | all of it; small text files |

Never committed (ignored): `Library/`, `Temp/`, `Logs/`, `obj/`,
`UserSettings/` (per-user editor layout), `unity/*.csproj` / `unity/*.sln`
(IDE files Unity regenerates on demand), builds, and `*.utmp` / `*.tmp` —
which covers both `unity/.utmp/`, the IL2CPP and Android native build cache
a player build leaves behind, and `<asset>.utmp`, the editor's mid-save copy
orphaned in `Assets/` by a crash.

Once the metas are in, a `.meta` appearing or disappearing in `git status`
is a real change (asset added/removed) — commit it with the asset, and never
let an asset land without its meta.

## 5. Folder layout

```
unity/
  Assets/
    _Project/            # everything hand-made lives under here, namespaced
      Engine/            #   Bloodhound.Engine — game-agnostic kernel (asmdef)
      Core/              #   GridInfect.Core — pure C# rules, no UnityEngine (asmdef)
      Game/              #   Unity adapter: screens, views, input (asmdef)
      Editor/            #   editor-only tooling (asmdef, Editor platform)
      Tests/EditMode/    #   NUnit suites (asmdef)
      Scenes/            #   Main.unity
      Art/  Audio/       #   future committed runtime assets — small exports only
    Settings/            # URP assets, created by RenderPipelineSetup — committed
    _Local/              # git-ignored per-machine staging (see §7); create if needed
  Packages/              # manifest + lock — committed
  ProjectSettings/       # committed
  Library/ Temp/ Logs/ UserSettings/   # generated — ignored
```

Conventions: new runtime code goes under an existing asmdef (or a new one
under `_Project/`); the `_Project` prefix keeps our tree sorted above any
imported third-party folders; module boundaries (what may reference what)
are enforced by the asmdefs and the `src/` mirror build, and documented in `ARCHITECTURE.md`.

## 6. First run: what to check, in order

Nothing in this repo has been through the Unity editor. The C# is
compile-checked against API stubs and the board shader has never been
compiled by Unity at all. This list is ordered by what is most likely to bite,
so work down it rather than judging the whole thing at once.

| # | Check | If it's wrong |
|---|---|---|
| 1 | **Console is clean on Play.** | `[board] shader 'GridInfect/Board' not found` means the shader failed to compile — Unity logs the real error separately. The board draws nothing; everything else still works. |
| 2 | **The board draws at all.** Six columns, eleven rows, empty cells as plates and holes as bare background. | If cells and holes look the same, `CellPlate` isn't reaching the material. If the whole quad is black, suspect the state texture (`RGBAFloat`, point-filtered) or `_BoardRect`. |
| 3 | **Drop a piece — traces, then ink.** A beam races out, the blot dissolves in behind it, the cell settles hard-edged. | If the dissolve looks like static rather than ink, the blot histogram is the suspect (`BoardNoise` rank-normalises for exactly this reason). |
| 4 | **The hot fill blooms and the cooled fill does not.** | Post-processing has to be on for the camera — `BoardBloom.Ensure` sets `renderPostProcessing`, and the Volume is created at runtime on the Default layer, so check the camera's Volume Mask if nothing glows. Threshold is 1.0 by design: only HDR output blooms. |
| 5 | **Colour space is Linear and HDR is on.** Already set in `ProjectSettings`; `ProjectRendersLinear` gates it. | If the board looks washed out or double-dark, check `BoardView.SetPaletteColor` — it must hand sRGB values over untouched, because Unity already converts `Color` shader properties on `SetColor`. |
| 6 | **Portrait, 1080×2340 game view.** All four screens should compose without anything off-screen or overlapping. | Every screen measures from `PresentationConfig.Layout`; fix the metric, not the screen. |
| 7 | **Audio.** One click per hop, pitched up per ray depth, and a two-strike glass solve after the winning wave's last click; both are struck glass, like the tiles. Synthesised at runtime. | `python3 tools/render_audio.py` renders the same formulas to WAV to listen to off-device; the levels (`ClickVolume`, `ChimeVolume` in `HopClickAudio`) are set from those renders, not from a phone. |
| 7b | **The title.** GRID in dense glass, the bug, INFECT lit, one line across the content width; on the first menu of a launch the bug lands and INFECT lights left to right. | The letters are rasterised on the main thread the first time the menu builds (`View/TitleRaster.cs`, ~0.3 s at 1080 wide on a desktop core; cached after). If the hitch shows on a phone, move the canvases onto `Work.Shared` and keep the sprite creation on the main thread. `tools/style-bench/glyphs` renders the same mark headlessly (`dotnet run -- title 2.77 out.png`) if it looks wrong. |
| 8 | **On device.** `RGBAFloat` sampling, HDR buffer cost, and the fragment loop (8 sparks + 5 state taps) are the three things a phone will judge differently from an editor. | |

Three acceptance criteria in `docs/infection-vfx-spec.md` are runtime
judgements and are still open by definition: **60 fps on the minimum device
with every juice layer on** (§2), **every state identifiable at 5 cm board
width** (§5), and **the bleed reading as ink from 20 ms to 200 ms hop** (§6).

Two things are deliberately left for you rather than guessed:

- **Trace and bleed durations** are the spec's only unfixed numbers (90 ms and
  260 ms today).
- **Direction bias is locked at 0.3, and at 0.3 the bleed edge does not read
  as an edge** — a threshold band in a noise-dominant field is a scattered
  set, not a spatial one. Past roughly 0.6 with the band nearer 0.05 it
  becomes the ink the spec describes. Written up under "As built"; unchanged
  because it is an art call.

## 7. Asset policy — no heavy files, no LFS

The repo stays clone-fast: **no binary source art, no audio masters, no
LFS**. Enforced by `.gitignore` and by review.

- **Committed**: small exported runtime formats only — `.png` for sprites,
  `.ogg` for audio, fonts, `.unity`/`.asset` text serialization. Rule of
  thumb: a committed binary should be KBs, not MBs.
- **Local only**: source/working files (`.psd`, `.xcf`, `.kra`, `.blend`,
  `.wav` masters) are ignored globally. Keep them in
  `unity/Assets/_Local/` if they're convenient to have inside the project
  (Unity imports them; git ignores them), or outside the repo entirely.
  Never reference a `_Local/` asset from a committed scene or prefab — it
  won't exist on other machines.
- **Escape hatch**: if a specific ignored-format file ever genuinely belongs
  in the repo, `git add -f` it deliberately.
- **Longer term**: real asset management (shared source art, versioned
  exports) is an open item — likely a small custom sync/bake step alongside
  `tools/`, not LFS. Until then, source art is per-machine and exports are
  what's shared.

## 8. Mobile builds

Everything a player build needs that is not in `ProjectSettings/` is applied
from code in `Assets/_Project/Editor/MobileBuild.cs`, so a fresh clone builds
the same way from the menu and headless. The settings themselves follow
`docs/DEPENDENCIES.md` §8 (IL2CPP, ARM64 only, target API 36, iOS 15,
portrait, linear).

**Menu:** `Grid Infect ▸ Build ▸ Android (APK for a device)` for a local
install, `… Android (AAB for Play)` for the store, `… iOS (Xcode project)`
on a Mac. `Grid Infect ▸ Apply player settings and icons` does the settings
and icon assignment without building (run it once after the first open and
commit what it changes in `ProjectSettings/ProjectSettings.asset`, which is
where the icon slots are serialized).

**Headless:**

```sh
Unity -batchmode -quit -projectPath unity -executeMethod GridInfect.EditorTools.MobileBuild.AndroidApk
Unity -batchmode -quit -projectPath unity -executeMethod GridInfect.EditorTools.MobileBuild.Android      # AAB
Unity -batchmode -quit -projectPath unity -executeMethod GridInfect.EditorTools.MobileBuild.IOS
```

Output lands in `unity/Builds/` (git-ignored). The Android and iOS Build
Support modules must be installed with the editor in Hub; Android also needs
Hub's bundled OpenJDK, SDK and NDK ticked.

What the script does on every build:

- Creates `Assets/_Project/Scenes/Main.unity` (empty) if it is missing and
  makes it the only scene in the list. A player needs one scene; the game
  needs nothing in it (§2).
- Assigns the icons under `Assets/_Project/Art/Icon/` to every icon slot the
  installed platform module exposes: `icon_1024.png` (the tile with the bug on
  it, STYLE-GUIDE §12 round 4) everywhere, and for Android's adaptive icon
  the background substrate under the 62% foreground, with
  `icon_adaptive_mono_432.png` as the monochrome layer where the kind has a
  third slot (themed icons). `grid-infect-style/gen-media.mjs --install`
  renders the four from the same numbers (STYLE-GUIDE §12.1) and copies
  them here; `gen-logo.mjs --png` still writes the reference rasters.
- Signs Android from the environment, never from the repo:
  `GI_KEYSTORE` (absolute path to the `.jks`), `GI_KEYSTORE_PASS`,
  `GI_KEYALIAS`, `GI_KEYALIAS_PASS`. Unset, the build is debug-signed:
  installable, not uploadable. `*.keystore` / `*.jks` are git-ignored.
  An editor launched from Hub or Finder does not inherit the shell's
  environment on macOS, so build the AAB from a terminal that has the four
  exported, with the `-batchmode -executeMethod` line at the top of
  `MobileBuild.cs` (Unity's own keytool is at
  `<editor>/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool`). The build
  leaves the keystore path and alias in `ProjectSettings.asset`; do not
  commit that hunk. A build also rewrites
  `Plugins/Android/GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml` —
  `GoogleMobileAds/Editor/AndroidBuildPreProcessor.cs` injects the AdMob app
  ID (already committed, in `GoogleMobileAdsSettings.asset`) and the editor
  version string. That hunk is build output too; leave it uncommitted.

**Before the first Play upload** (R-1201, R-1203): the application
identifier is `com.bloodhoundstudios.gridinfect.app` (`MobileBuild.AppId` and
the two entries in `ProjectSettings.asset`; a draft app under that name
exists on Play, and it still needs registering on App Store Connect before
the iOS follow); create the upload key (`keytool -genkeypair -v
-keystore gridinfect-upload.jks -alias upload -keyalg RSA -keysize 2048
-validity 10000`) and keep it somewhere that outlives a laptop — a password
manager with file attachments, not a machine — because losing the 2014 one
is what cost the original package name; bump `bundleVersion` and
`AndroidBundleVersionCode` per upload. The AdMob plugin (DEPENDENCIES §4)
is still not imported, so this build serves no ads.

**CI:** `.github/workflows/mobile-build.yml` builds Android on demand
(Actions ▸ Mobile build ▸ Run workflow) through GameCI. It needs
`UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` as repository secrets
(a Personal licence is fine) and, for a Play-ready AAB, the keystore as
`GI_KEYSTORE_B64` plus its three passwords. It is not part of the push CI
and has not been run yet: the first run will tell you whether GameCI has an
image for the pinned editor patch (pin `unityVersion:` to the nearest one
that exists if not).
