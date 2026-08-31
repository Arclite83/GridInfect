# Grid Infect Unity Rebuild — Dependencies & Project Configuration

Date: 2026-08-30. Companion: [`REQUIREMENTS.md`](REQUIREMENTS.md) — every
entry here cites the requirement(s) it exists for (§10 has the full map).

> **Baseline delta (2026-08-30, build-over-buy pass — see
> [`../ARCHITECTURE.md`](../ARCHITECTURE.md)).** The shipped baseline shrank
> wave 1 below what §3 specifies; packaged products earn their place when a
> requirement actually lands:
>
> - **No `com.unity.nuget.newtonsoft-json`** — a ~300-line `MiniJson` in
>   `Bloodhound.Engine` covers vectors, saves, and the action log.
> - **No `com.unity.inputsystem`** — the baseline drag is legacy
>   `UnityEngine.Input` (single-pointer, satisfying R-115); revisit only if a
>   real input requirement outgrows it.
> - **URP is in the baseline, adopted from code** — built-in is in
>   maintenance, so the manifest pins `com.unity.render-pipelines.universal`
>   (the editor locks core-package versions to itself; the pinned number
>   auto-corrects on first open) and an editor script creates and assigns the
>   pipeline asset — no template, no manual setup. Forward renderer for the
>   procedural baseline; the §2 2D Renderer (which needs a Light2D) arrives
>   with the art overhaul. HDRP was never a candidate: it does not ship on
>   mobile.
> - Wave 1 as shipped: URP + `com.unity.test-framework`, nothing else.
> - The §6 assembly layout gained a fourth assembly: `Bloodhound.Engine`, the
>   reusable game-agnostic kernel under `GridInfect.Core`.
>   [`../ARCHITECTURE.md`](../ARCHITECTURE.md) §1 is now authoritative for the
>   module graph.
>
> §4–§11 (ads/consent/IAP/analytics SDKs, Asset Store candidates, project
> settings, version pins) are untouched and remain the plan of record for
> their waves. The Asset Store list stays a *candidate* list under
> build-over-buy: PrimeTween-class utilities are cheap wins; the others get
> re-justified against "implement the needed fraction ourselves" when the
> overhaul begins.

Version-verification note: this session's proxy blocks `unity.com`,
`docs.unity3d.com`, `developers.google.com`, and `assetstore.unity.com`
directly; versions below were verified today via the Unity package registry
mirrors on GitHub (needle-mirror, authoritative for published package
versions), the googleads GitHub releases page, and current search results
against those official pages (each cited in §11). Re-confirm the editor
patch number in Unity Hub at install time — patches land roughly biweekly.

---

## 1. Unity editor

**Unity 6000.3.22f1 (Unity 6.3 LTS)** — the newest 6.3 patch at writing
(released 2026-08-13; 6.3.0f1 shipped Dec 2025, supported through Dec 2027).
Install the newest 6000.3.x Hub offers; pin the project to whatever patch you
install and upgrade patches deliberately.

Why the alternatives lose in one sentence: 6000.0 LTS's support window is
nearly exhausted (ends late 2026) and 6.4/6.5 are non-LTS tech releases —
6.3 is the only stream giving a solo dev two years of fixes without forced
upgrades. Unity Personal covers this project (revenue < $200k). macOS
editor: Apple silicon build. (R-1201, R-1301.)

## 2. Render pipeline

**URP with the 2D Renderer.** Built-in is in maintenance with no active
development and HDRP doesn't ship on mobile, so URP is the only pipeline
with a future for a mobile sprite game — and its 2D Renderer gets the SRP
batcher plus 2D-specific features for free. Create the project from the
**2D (URP)** template so the pipeline asset and 2D Renderer come
preconfigured.

Version: `com.unity.render-pipelines.universal` is a core package whose
version is locked to the editor (17.x line for Unity 6). Do not hand-pin it;
accept what 6000.3.22f1 installs and verify it shows a 17.x version in
Package Manager. (Serves R-1101–R-1103, R-1001 tile art.)

## 3. Wave 1 — packages needed to reach a playable grid

Nothing else gets imported until the grid is playable and R-114's tests pass.

| Package | Version | Why (one line) | Serves |
|---|---|---|---|
| `com.unity.render-pipelines.universal` | editor-locked 17.x | Rendering (see §2) | R-1101–1103 |
| `com.unity.inputsystem` | **1.20.0** | Touch drag/press for piece placement and the cancellable-window touch (R-107); requires Unity 6000.0+, satisfied | R-104, R-107, R-115 |
| `com.unity.test-framework` | **1.4.6** | Edit-mode NUnit runner for the vector replay — the hard test constraint | R-114, R-1302 |
| `com.unity.nuget.newtonsoft-json` | **3.2.2** | Parses `test_vectors.json` (dictionary-keyed levels; `JsonUtility` cannot) and the save file | R-114, R-502 |
| `com.unity.ugui` | editor-bundled 2.x | Menus, popups, HUD; TextMeshPro is merged into uGUI 2.0 in Unity 6 — do **not** add a separate TMP package | R-202–203, R-1003 |
| `com.unity.2d.sprite` | editor-bundled | Sprite import/editing for tiles and pieces | R-101 art, R-1001 |

`manifest.json` additions for wave 1 (everything else in the table is
editor-bundled or template-provided):

```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.20.0",
    "com.unity.test-framework": "1.4.6",
    "com.unity.nuget.newtonsoft-json": "3.2.2"
  }
}
```

Template hygiene: the 2D URP template adds extras (`com.unity.timeline`,
visual scripting, various `com.unity.2d.*`). Remove what has no requirement
behind it; keep `com.unity.2d.sprite`.

## 4. Wave 2 — everything else (do not import up front)

### Third-party SDKs

**Ads + consent: Google Mobile Ads (AdMob) Unity plugin v11.4.0**
(released 2026-08-19; bundles Android next-gen GMA SDK 1.3.1, iOS GMA SDK
13.7.0, External Dependency Manager 1.2.188). One vendor covers R-601 ads
*and* R-801 consent — the UMP consent APIs ship inside this plugin
(`GoogleMobileAds.Ump.Api`), which is why LevelPlay/AppLovin lose: they'd
add a mediation layer and a separate CMP integration that a solo,
single-network MVP doesn't need (mediation can be added under AdMob later
without changing this plugin). Compatibility, per the official quick-start
(updated 2026-08-27): Unity 2019.4+ (6000.3 ✓), Android min API 23 ✓ /
target 35+ ✓ (we target 36), iOS deployment target 13+ ✓ (we set 15),
Xcode 16+. Install: import `GoogleMobileAds-v11.4.0.unitypackage` from the
GitHub releases page. Serves R-601–605, R-801–803.

**IAP: `com.unity.purchasing` 5.4.2** (registry, 2026-07-24; requires Unity
2022.3+ ✓). First-party package, one API over both stores, no server needed
for a single non-consumable — a custom StoreKit/Play Billing integration
loses on maintenance for zero MVP benefit. Serves R-701–702, R-503. LATER.

**Analytics: `com.unity.services.analytics` 6.3.0** (registry, 2026-03-05;
requires Unity 2022.3+ ✓). Package-manager native with a dashboard and no
extra native SDK — Firebase loses on binary size and a second console for a
solo dev who only needs funnel/cadence events. Serves R-606. LATER.

**Achievements/leaderboards (LATER, R-205/R-306):** Google Play Games
plugin for Unity v2 (Android) and Apple's GameKit via Unity's Apple plugins
(iOS) — named here for planning; versions get verified when that work
starts, not now.

### Asset Store candidates for the presentation overhaul (max 5)

Store listing pages are egress-blocked from this session, so prices are
last-known approximations — **verify price and Unity 6.3 compatibility on
the listing before buying**. Each ties to a requirement; none imports
before wave 2.

| Asset | Cost (approx, unverified) | Tied to |
|---|---|---|
| **PrimeTween** (Kyrylo Kuzyk) | free | R-1102 timing table — allocation-free tweens for drop-snap/tray-return/popup slides; DOTween loses on GC allocations and a paid Pro tier for the same job |
| **Feel** (More Mountains) | ~US$45 | R-1102/R-603 moments — packaged juice (shake, flash, haptics) for infect/repel/reset/win beats without hand-rolling feedback systems |
| **All In 1 Sprite Shader** (Seaside Studios) | ~US$40 | R-1001/R-1101 — per-tile glow/outline/pattern/dissolve effects from one shader, keeping state legibility work out of custom shader authoring |
| **GUI PRO Kit – Casual Game** (Layer Lab) | ~US$60 | R-202/203, R-901 — coherent menu/popup/HUD art replacing the 2014 chrome a solo dev shouldn't redraw |
| **Universal Sound FX** (Imphenzia) | ~US$40 | R-902 — a licensed SFX library replacing the single `click.wav` and the unlicensed music-adjacent gap |

## 5. Ads path, end to end (acceptance item)

1. **SDK**: GMA Unity plugin v11.4.0 (§4), imported in wave 2. EDM4U inside
   it resolves the Android/iOS native deps.
2. **App registration**: create the Android app in the AdMob console; the
   **AdMob app ID** goes into `Assets ▸ Google Mobile Ads ▸ Settings`
   (`GoogleMobileAdsSettings.asset` — the plugin injects it into the
   Android manifest / Info.plist at build).
3. **Consent before ads (R-801)**: on boot,
   `ConsentInformation.Update()` → `ConsentForm.LoadAndShowConsentFormIfRequired()`
   → only when `ConsentInformation.CanRequestAds` → `MobileAds.Initialize()`
   → load the interstitial. The GDPR message itself is configured in AdMob
   console ▸ Privacy & messaging. Settings screen calls
   `ConsentForm.ShowPrivacyOptionsForm()` when
   `PrivacyOptionsRequirementStatus == Required` (R-802).
4. **Test ads (R-604)**: Google demo units — Android interstitial
   `ca-app-pub-3940256099942544/1033173712` (banner `…/6300978111` if ever
   needed; iOS interstitial `…/4411468910`) — plus real devices registered
   via `RequestConfiguration.TestDeviceIds`. Safe to click; never ship them.
5. **Production unit IDs (R-602/604)**: a single `AdConfig`
   ScriptableObject in `GridInfect.Services` holds per-platform unit IDs,
   the cadence values (first-ad solve count, spacing), and a
   `useTestAds` flag; the repo default is demo IDs + flag on. Real IDs are
   entered only in that asset for release builds. The app ID lives only in
   `GoogleMobileAdsSettings.asset` (step 2).
6. **Store side (R-605)**: Play Console Data safety declares ads +
   advertising ID (the plugin auto-adds `AD_ID`); `app-ads.txt` on your
   developer-site domain once one exists (recommended, not MVP-blocking).

## 6. Assembly definition layout (hard constraint)

Unity project lives at `unity/` in the repo (sibling of `docs/`).

```
unity/Assets/_Project/
  Core/                GridInfect.Core.asmdef
    Rules/             Game, Level, Piece, Repel, enums (RULES.md port)
    Generator/         LevelBuilder, Pcg32 (GENERATOR.md port)
    Save/              SaveModel (pure data, no IO)
  Game/                GridInfect.Game.asmdef
    Board/, Screens/, Presentation/   (rendering, input, the 0.3 s timer)
  Services/            GridInfect.Services.asmdef
    Ads/, Consent/, Iap/, AnalyticsFacade/   (all SDK touchpoints)
  Tests/EditMode/      GridInfect.Core.Tests.asmdef
    VectorReplayTests, (later) GeneratorGoldenTests
```

```json
// GridInfect.Core.asmdef  — R-1301: no UnityEngine anywhere
{ "name": "GridInfect.Core", "rootNamespace": "GridInfect.Core",
  "references": [], "noEngineReferences": true, "autoReferenced": false }

// GridInfect.Game.asmdef
{ "name": "GridInfect.Game", "references": [ "GridInfect.Core", "Unity.InputSystem" ] }

// GridInfect.Services.asmdef  — R-1303: SDKs referenced here only
{ "name": "GridInfect.Services",
  "references": [ "GridInfect.Core" /* + GoogleMobileAds asmdefs, UnityEngine.Purchasing, Unity.Services.Analytics after wave-2 import (names confirmed at import) */ ] }

// GridInfect.Core.Tests.asmdef  — editor-only test assembly
{ "name": "GridInfect.Core.Tests", "references": [ "GridInfect.Core" ],
  "includePlatforms": [ "Editor" ], "defineConstraints": [ "UNITY_INCLUDE_TESTS" ],
  "precompiledReferences": [ "nunit.framework.dll" ], "overrideReferences": true,
  "optionalUnityReferences": [] }
```

(The tests asmdef also references `Newtonsoft.Json` for the vector loader;
IO and JSON live in the test/Unity layers — Core's API takes plain arrays.)

## 7. Test setup (hard constraint)

Edit-mode only, editor on macOS, no device, no player build (R-1302):

- `GridInfect.Core.Tests` walks up from `Application.dataPath` to the git
  root and loads `docs/test_vectors.json` directly — one source of truth, no
  copied fixture to drift.
- For each of the 128 levels: construct the board, apply the solution
  placements through the Core API, resolve after every placement (the
  cancellation bug is not ported — R-107), assert every `board_after` and
  the final win — the C# mirror of `docs/tools/verify_test_vectors.py`.
  Run `docs/tools/regen_clean_solutions_40_86.py` once first so the
  vectors for ids 40/86 are exploit-free (RULES §4.1 correction).
- Runs in the Test Runner window and headless:
  `Unity -batchmode -runTests -testPlatform EditMode -projectPath unity`.

## 8. Project settings (decided)

| Setting | Decision | Why the alternative loses |
|---|---|---|
| Scripting backend | **IL2CPP**, both platforms | Unity's Android ARM64 support requires IL2CPP (Mono is ARMv7-only) and iOS is IL2CPP-only — Mono can't ship this game anywhere it's going |
| Target architectures | **ARM64 only** | ARMv7 doubles native payload and QA for 32-bit-only devices that are effectively extinct in 2026 ad inventory |
| Android min API | **23** (Android 6.0) | It is both Unity 6.x's floor and the GMA plugin's floor — raising it only donates reach, and reach is revenue here |
| Android target API | **36** (Android 16) | Play mandates 36 for new apps from 2026-08-31; anything lower is rejected at submission |
| Android packaging | AAB for Play (APK for local installs) | Play requires AAB |
| Android graphics APIs | Vulkan + OpenGL ES 3.0 fallback (default order) | ES2 is below the URP floor; auto fallback covers pre-Vulkan devices |
| iOS deployment target | **15.0** | Unity 6.3's own iOS floor is 15 (GMA only needs 13) — you cannot ship lower, and 15 covers effectively all live iPhones |
| Orientation | **Portrait** only (upside-down allowed) | Product direction: mobile play is one-handed and upright. The original shipped landscape-locked, and an 11-wide board in portrait is a strip rather than a screenful — the layout now fits whichever axis binds so nothing overflows, but making portrait *good* is an open design question (R-1103) |
| Color space | **Linear** | Correct blending under URP, and the min spec (GLES3/Metal) supports it everywhere we ship; gamma's only advantage was hardware we don't target |
| Texture compression | **Android ETC2, iOS ASTC** | The API-23/GLES3 floor guarantees ETC2 decode but not ASTC (Adreno 3xx-era), so ASTC-for-Android would silently decompress on the oldest devices; on iOS every supported device decodes ASTC |
| Frame rate | `Application.targetFrameRate = 60` | R-1104 — the original's 90 buys nothing in a turn-based puzzle and costs battery |
| App ID | new, user-supplied (R-1203) | old `com.bloodhoundstudios.gridinfect` ownership unverified — UNKNOWN |

## 9. Wave summary

- **Wave 1** (playable grid + passing vector tests): Unity 6000.3.22f1, 2D
  URP template, Input System 1.20.0, Test Framework 1.4.6,
  Newtonsoft JSON 3.2.2. Nothing else.
- **Wave 2** (import when its requirement starts): GMA plugin v11.4.0
  (ads + consent, MVP), Unity IAP 5.4.2 (LATER), Unity Analytics 6.3.0
  (LATER), the ≤5 Asset Store items (overhaul), Play Games/GameKit plugins
  (LATER, versions verified then).

Note wave ≠ priority: the GMA plugin is wave 2 (not needed for a playable
grid) but MVP (needed for the internal-testing build that serves an ad).

## 10. Dependency → requirement map (acceptance item)

| Dependency | Requirements |
|---|---|
| Unity 6000.3.22f1 | all; explicitly R-1201, R-1301–1303 |
| URP (editor-locked 17.x) | R-1001, R-1101–1103 |
| com.unity.inputsystem 1.20.0 | R-104, R-107, R-115 |
| com.unity.test-framework 1.4.6 | R-114, R-403, R-1302 |
| com.unity.nuget.newtonsoft-json 3.2.2 | R-114, R-502 |
| com.unity.ugui (bundled, TMP included) | R-202, R-203, R-303, R-901, R-1003 |
| com.unity.2d.sprite (bundled) | R-101, R-1001 |
| GMA Unity plugin v11.4.0 (incl. UMP, EDM4U 1.2.188) | R-601–605, R-801–803 |
| com.unity.purchasing 5.4.2 | R-503, R-701, R-702 |
| com.unity.services.analytics 6.3.0 | R-606 |
| PrimeTween (free) | R-1102 |
| Feel (~$45) | R-1102, R-603 presentation |
| All In 1 Sprite Shader (~$40) | R-1001, R-1101 |
| GUI PRO Kit – Casual Game (~$60) | R-202/203, R-901 |
| Universal Sound FX (~$40) | R-902 |

## 11. Version sources (verified 2026-08-30)

- Unity 6.3 LTS availability & support window: unity.com blog "Unity 6.3 LTS is Now Available"; latest patches 6000.3.20f1 (2026-07-16), 6000.3.21f1 (2026-07-29), **6000.3.22f1 (2026-08-13)**: unity.com/releases/editor/whats-new/6000.3.22f1 (via search index; unity.com direct fetch blocked).
- Unity 6 Android minimum = API 23: docs.unity3d.com Manual "Android requirements and compatibility" (6000.2/6000.3) + Unity Discussions "[2023.2+] Increasing the minimum supported API to Android 6 (API 23)".
- Unity 6.3 iOS minimum = iOS 15, Xcode 16+: docs.unity3d.com/6000.3 Manual "iOS requirements and compatibility".
- Play target-API mandate (36 from 2026-08-31 for new apps/updates): support.google.com/googleplay/android-developer/answer/11926878.
- GMA Unity plugin v11.4.0 (2026-08-19), bundled Android next-gen SDK 1.3.1 / iOS SDK 13.7.0 / EDM4U 1.2.188: github.com/googleads/googleads-mobile-unity/releases (fetched directly).
- GMA plugin prerequisites (Unity 2019.4+, Android min 23 / target 35+, iOS 13+ / Xcode 16+): developers.google.com/admob/unity/quick-start (page dated 2026-08-27, via search).
- UMP consent APIs inside the Unity plugin: developers.google.com/admob/unity/privacy (`GoogleMobileAds.Ump.Api`).
- Demo ad unit IDs: developers.google.com/admob/unity/test-ads.
- com.unity.purchasing **5.4.2** (2026-07-24, unity: 2022.3): github.com/needle-mirror/com.unity.purchasing (tags + package.json, fetched directly).
- com.unity.inputsystem **1.20.0** (2026-07-21, unity: 6000.0): needle-mirror, fetched directly.
- com.unity.services.analytics **6.3.0** (2026-03-05, unity: 2022.3): needle-mirror, fetched directly.
- com.unity.test-framework **1.4.6** (unity: 2019.4): needle-mirror, fetched directly.
- com.unity.nuget.newtonsoft-json **3.2.2** (unity: 2018.4): needle-mirror, fetched directly.
- TextMeshPro merged into com.unity.ugui 2.0 (no separate package in Unity 6): docs.unity3d.com/Packages/com.unity.ugui@2.0 + Unity Discussions "[2023.2] Latest Development on TextMesh Pro".

## UNKNOWN / could not verify from this session

- Whether a 6000.3 patch newer than .22f1 dropped in the last two weeks
  (unity.com blocked; check Hub — the pick is "newest 6000.3.x" regardless).
- Exact URP package version string bundled with 6000.3 (editor-locked 17.x;
  read it from Package Manager after project creation).
- Asset Store prices and per-asset Unity 6.3 compatibility flags (store
  blocked; verify on each listing before purchase).
- GMA plugin's asmdef names for the §6 Services references (confirm at
  import; documentation doesn't state them).
- CocoaPods/Xcode specifics for the eventual iOS build beyond "Xcode 16+,
  iOS 13+" from the quick-start (LATER work, verify then).
