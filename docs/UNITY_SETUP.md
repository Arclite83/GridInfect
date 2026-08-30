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
4. Set the **Game view** to a landscape resolution (e.g. 16:9) and press
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
(IDE files Unity regenerates on demand), builds.

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
    _Local/              # git-ignored per-machine staging (see §6); create if needed
  Packages/              # manifest + lock — committed
  ProjectSettings/       # committed
  Library/ Temp/ Logs/ UserSettings/   # generated — ignored
```

Conventions: new runtime code goes under an existing asmdef (or a new one
under `_Project/`); the `_Project` prefix keeps our tree sorted above any
imported third-party folders; module boundaries (what may reference what)
are enforced by `ArchitectureGateTests` and documented in `ARCHITECTURE.md`.

## 6. Asset policy — no heavy files, no LFS

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
