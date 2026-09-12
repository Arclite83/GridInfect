# AdMob, IAP and the store identities — the setup pass

Companion to [`DEPENDENCIES.md`](DEPENDENCIES.md) §4–5 (which chose the stack
and recorded the API sequence) and [`REQUIREMENTS.md`](REQUIREMENTS.md) §6–8.
This file is the order of operations: what to do in a browser, what to do in
the editor, and what is already done in the repo.

Everything in the repo half is on `main` already. The browser half is yours —
it needs accounts this project cannot reach.

## Where this stands (2026-09-12)

| Done | Where it shows |
|---|---|
| Play draft app on `com.bloodhoundstudios.gridinfect.app` | Play console |
| AdMob Android and iOS apps, app IDs entered | `GoogleMobileAdsSettings.asset` carries both `~` IDs |
| Ad units created (interstitial, rewarded) | AdMob console — **not yet in `AdConfig`, see §3.3** |
| Test device registered | AdMob console ▸ Settings ▸ Test devices |
| GMA plugin v11.5.0 imported and committed, all of it | `Assets/GoogleMobileAds`, `Assets/ExternalDependencyManager`, `Assets/Plugins/{Android,iOS}` |
| `GRIDINFECT_ADMOB` set for Android, iOS and Standalone | `ProjectSettings.asset` — no editor step left |
| `Services/Sdk/AdMobServices.cs` checked against the 11.5.0 DLL metadata | one fix: `CanRequestAds` is a method |

| Open | Blocked on |
|---|---|
| AdMob GDPR message will not publish | a privacy policy URL — §2a |
| App Store Connect privacy step | the same URL |
| Unit IDs into `AdConfig` | nothing; §3.3 |
| Android dependency resolution (EDM) has not run | nothing; §3.2 |
| First editor compile of `Services/Sdk/` | opening the project with the define now set |
| Sideload APK with test ads | the two above |
| Unity IAP | after ads work; §3.5 |

---

## 0. The identifier, decided

`com.bloodhoundstudios.gridinfect.app`, on both stores. Draft app created on
Play 2026-09-11; register the same bundle ID on App Store Connect before the
iOS follow.

Why not the 2014 package: a Play listing can only be updated by something
signed with its original key, the 2014 app predates Play App Signing, and
the keystore left with a laptop. That alone kills updating the old listing.
A published package name also cannot be reused for a *new* listing, which
kills the other route. `com.bloodhoundstudios.gridinfect` is burned both
ways, and nothing on Google's side reverses it.

iOS could have kept the old bundle ID (Apple manages the certificates), but
the one thing reuse was ever going to buy was the Android install base. With
that gone, one identity for the remaster is simpler than two, and a 2014
listing's ratings and screenshots are nothing worth inheriting.

This closes `NEXT_PASS.md` decision 6 and resolves R-1203.

## 1. The keystore, this time

Generate the upload key before the first AAB, and put it somewhere that
outlives a machine. The 2014 one lived on a laptop; that is the whole story
of §0.

```
keytool -genkeypair -v -keystore gridinfect-upload.jks -alias upload \
  -keyalg RSA -keysize 2048 -validity 10000
```

Store the `.jks` plus all three secrets (store password, alias, alias
password) in a password manager that takes file attachments. Play App
Signing is on by default for a new app, so this is an *upload* key and
Google holds the real signing key; losing it is a reset request rather than
a burned package. Still: do not lose it.

`mobile-build.yml` reads it as `GI_KEYSTORE_B64` plus `GI_KEYSTORE_PASS`,
`GI_KEYALIAS`, `GI_KEYALIAS_PASS`. `MobileBuild.cs` reads the same names
from the environment for a local build. Nothing about the key is ever in
the repo.

## 2. AdMob console

Free to set up; AdMob takes a revenue share rather than a fee.

1. Add the **Android** app. When it asks whether the app is listed on a
   store, answer **no**: it hands you an app ID immediately and marks the app
   "not linked". A draft Play app is not a listing, and internal testing is
   not public, so linking waits until the production release. Unlinked apps
   serve test ads without restriction, which is all the first build needs.
2. Copy the **AdMob app ID** (`ca-app-pub-…~…`, tilde). It goes in
   `Assets ▸ Google Mobile Ads ▸ Settings` after the import in §3, which
   writes `GoogleMobileAdsSettings.asset`. The plugin injects it into the
   Android manifest at build.
   **The app ID does not go in `AdConfig`.** Different thing from a unit ID.
3. Create two ad units: one **Interstitial**, one **Rewarded**. Their IDs
   (`ca-app-pub-…/…`, slash) are what `AdConfig` holds.
4. **Privacy & messaging ▸ GDPR**: create the consent message and publish it.
   UMP reads it from here; the code just calls the form. Nothing in the app
   configures the message text. **It will not publish without a privacy
   policy URL** — §2a, below, is the prerequisite.
5. **Privacy & messaging ▸ Other regions / IDFA** can wait for the iOS follow
   (R-803).
6. On each ad unit, set **frequency capping** to roughly 12/hour and 20/day.
   That is deliberately looser than the in-app cadence gates so it never
   binds in normal play. It exists so a client bug cannot spam a user faster
   than you can push a fix.

### 2a. The privacy policy URL (R-804)

One URL unblocks three consoles: the AdMob GDPR message, the Play listing
(store listing field, plus the Data safety form links to it) and App Store
Connect's App Privacy step. The policy is written and in the repo:
`docs/privacy.html`, with `docs/index.html` and `docs/.nojekyll` beside it so
GitHub Pages serves the folder as-is, no Jekyll build to break.

1. Edit `docs/privacy.html`: replace `CONTACT_EMAIL` (both occurrences) with
   an address you are willing to have public, and check the effective date.
2. GitHub ▸ repo **Settings ▸ Pages ▸ Build and deployment**: Source
   *Deploy from a branch*, branch `main`, folder `/docs`. Save.
3. A minute later the policy is at
   `https://arclite83.github.io/GridInfect/privacy.html`. That is the URL.
   The developer-website field on Play can take
   `https://arclite83.github.io/GridInfect/`.

What the policy states, so the console forms agree with it: no accounts, no
servers, nothing collected by the developer; progress stored on-device only
and erasable from Settings; AdMob collects the advertising ID, IP, device
info, ad interactions and consent state; the store handles the purchase and
the app only reads the receipt; no analytics or crash SDKs; not
child-directed. If R-606 (analytics) ever ships, the policy changes first.

The app does not link to it yet: nothing in `Game/` calls
`Application.OpenURL`. A "Privacy policy" row on the settings
screen next to "Privacy options" is the one remaining stage-6 code item
(new string in every locale via the strings generator, one row, one
`OpenURL`). It is required by Play's User Data policy, not only by the
document.

### Do not click your own live ads

Register your device under **Settings ▸ Test devices** in the AdMob console,
or put its ID in `AdConfig.TestDeviceIds`. The SDK logs the device ID on
first run. An unregistered device tapping a live ad is the fastest way to
lose the account, and it is not recoverable by apologising.

Keep `AdConfig.UseTestAds` on for every build that is not a store release.

---

## 3. Unity import

1. **Done.** `GoogleMobileAds-v11.5.0.unitypackage` is imported and every
   file it wrote is committed — `Assets/GoogleMobileAds` (the C# DLLs, the
   editor scripts, the settings asset), `Assets/ExternalDependencyManager`,
   `Assets/Plugins/Android` (the `.aar` and the `androidlib`),
   `Assets/Plugins/iOS` (the xcframework and native templates), every
   `.meta`, and `ProjectSettings/GvhProjectSettings.xml`. "All of it" was
   the right answer: a `.unitypackage` is source, not a dependency, and the
   headless build needs the same bytes the editor has. The `.pdb` files are
   dead weight but harmless.
2. **Resolve the Android dependencies.** The plugin's C# is in the repo; the
   Google Play Services / GMA native libraries are not, and EDM fetches them.
   It has not run yet (there is no `mainTemplate.gradle` and no
   `ProjectSettings/AndroidResolverDependencies.xml`). Do it in template
   mode so the repo gets three small gradle files instead of thirty AARs:
   - **Player Settings ▸ Android ▸ Publishing Settings**: tick *Custom Main
     Gradle Template*, *Custom Gradle Properties Template* and *Custom
     Gradle Settings Template*. Unity writes `Assets/Plugins/Android/
     mainTemplate.gradle`, `gradleTemplate.properties`,
     `settingsTemplate.gradle`.
   - **Assets ▸ External Dependency Manager ▸ Android Resolver ▸ Resolve.**
     EDM patches the dependencies into those templates and writes
     `ProjectSettings/AndroidResolverDependencies.xml`.
   - Commit all four. EDM also auto-resolves before every build, so a CI
     build without them still works; the commit just makes it deterministic.
3. **Unit IDs go in `Assets/_Project/Resources/AdConfig.asset`.** Select it
   in the Project window; the Inspector shows *Android Interstitial* and
   *Android Rewarded* (and the iOS pair). That is where the two `/`-style
   IDs from §2.3 go — the plugin has no field for unit IDs anywhere, only
   the app ID, which is why there was nothing to find under
   *Google Mobile Ads ▸ Settings*.

   Recommendation, against what this file said yesterday: **commit the real
   unit IDs.** A unit ID is not a secret — it ships in every APK in plain
   text. The account-safety measure is the registered test device (§2), and
   `UseTestAds` already substitutes Google's demo units for whatever is in
   the fields, so the committed asset with `UseTestAds` on still serves demo
   ads. Committing them is what lets the CI workflow produce a release AAB
   at all; the alternative is hand-editing an asset on every release build.
   The switch that changes for a store build is then one bool, and it can
   be driven by an environment variable in `MobileBuild` later.
4. **The define is set.** `GRIDINFECT_ADMOB` is in `ProjectSettings.asset`
   for Android, iPhone and Standalone (Standalone so the editor's play mode
   uses the AdMob adapter with the plugin's placeholder ads whichever build
   target is active). If you ever need the field: **Edit ▸ Project Settings
   ▸ Player**, pick the **Android tab** at the top of the Player panel, then
   **Other Settings ▸ Script Compilation ▸ Scripting Define Symbols**. It is
   per-platform, which is why it is not visible until a platform tab is
   selected.
5. For IAP, add `com.unity.purchasing` 5.4.2 to the manifest. No symbol
   needed: the Services asmdef carries a `versionDefine` that raises
   `GRIDINFECT_IAP` when the package is present. Leave this until test ads
   are on a device; a second unverified SDK on the same day doubles the
   things to sort out.
6. **`Services/Sdk/AdMobServices.cs` is close, not compiled.** Every call in
   it was checked against the public metadata of the committed
   `GoogleMobileAds*.dll` (method versus property, parameter lists, event
   shapes) and the one mismatch fixed. Open the project with the define now
   set and read the console: a leftover error there is a `using` or a type
   the metadata reader could not see, and the file header says what was
   and was not checked. `UnityIapPurchaseService.cs` has had no such check
   and only compiles once the package is in.
7. The app IDs are already in `Assets ▸ Google Mobile Ads ▸ Settings`.

### The first build with ads in it

Two milestones, in this order, because the first needs no key and the
second does.

**Sideload APK.** `Grid Infect ▸ Build ▸ Android (APK for a device)`. With no
`GI_KEYSTORE` in the environment it is debug-signed, which installs fine and
uploads nowhere. Leave `AdConfig.UseTestAds` on. Solve nine boards outside
the tutorial and the ninth's popup should carry a test interstitial; the
LOCK button at an empty wallet should offer a test rewarded ad. That is
the whole acceptance for "ads work", and it needs nothing from Play.

**Internal-testing AAB.** Generate the key (§1), export the four `GI_*`
variables, `Grid Infect ▸ Build ▸ Android (AAB for Play)`, upload to the
draft app's **Internal testing** track. Internal testing needs only a
tester list, not the full store listing or review, and it is the first
thing that proves the signing, the manifest and the `AD_ID` permission end
to end on a Play-delivered install.

The GitHub workflow (`mobile-build.yml`) does the same builds headless.
The plugin's files are committed, so what it still needs is the Unity
licence secrets. It is not the fastest route to the first APK; the editor
on the Mac is.

---

## 4. Google Play

1. Data safety, answered to match `docs/privacy.html`: the app collects
   **Device or other IDs** (advertising ID) and **App activity ▸ app
   interactions**, both *collected*, *shared* with Google for
   **Advertising or marketing**, not optional, encrypted in transit (the
   SDK uses HTTPS), no deletion mechanism of ours (the user resets the ID
   on the device). Approximate location is inferred from IP by Google;
   declare it as collected for advertising. Nothing else is collected. Link the privacy policy URL from
   §2a. The plugin adds the `AD_ID` permission itself (R-605).
2. Declare the app **not** child-directed.
3. Ads declaration: yes, the app contains ads.
4. Create the `remove_ads` **non-consumable** in-app product. The ID must be
   exactly `remove_ads` — `UnityIapPurchaseService.RemoveAdsProductId`. Price
   it at the USD 4.99 tier and let Play localise from there (R-701).
5. `app-ads.txt` on a developer-site domain once one exists. Recommended,
   not launch-blocking.

---

## 5. What "done" looks like

Stage 6's acceptance, from `EXECUTION_PLAN.md`:

- An internal-testing AAB serves a **test** interstitial and a **test**
  rewarded ad, both behind the consent gate.
- Remove-ads purchase and restore work on a licence-tester account.
- The assembly-boundary gate test passes — it already does, and it runs in
  CI without any SDK present.

Verify the cadence on device rather than trusting it: solve eight boards
outside the tutorial and confirm the first interstitial does not arrive
before that, then confirm the next needs three more solves *and* four
minutes of board time. `AdCadenceTests` pins the rule; the device run is
checking the wiring, not the logic.

---

## 6. Already in the repo

Nothing below needs doing:

| Thing | Where |
|---|---|
| Cadence decision, clamps, counters | `Services/AdCadenceGate.cs`, 14 tests |
| Cadence values, editable | `Resources/AdCadence.asset` |
| Unit IDs and the test-ads flag | `Resources/AdConfig.asset`, `Services/AdConfig.cs` |
| SDK adapters, fenced | `Services/Sdk/` — unverified, see §3.4 |
| Implementation selection | `Bootstrap`, by define |
| Privacy options entry (R-802) | `SettingsScreen.cs`, shown only when UMP requires it |
| NO ADS chip (R-701) | `MainMenuScreen.cs`, hidden once owned |
| Assembly boundary gate (R-1303) | `Tests/EditMode/AssemblyBoundaryTests.cs` |
| Keystore plumbing (the key itself is yours to make, §1) | `.github/workflows/mobile-build.yml`, `Editor/MobileBuild.cs` |
| GMA plugin v11.5.0, every file, and the `GRIDINFECT_ADMOB` define | `Assets/GoogleMobileAds`, `Assets/Plugins`, `ProjectSettings.asset` |
| App IDs, Android and iOS | `GoogleMobileAdsSettings.asset` |
| Privacy policy text (R-804), ready to host | `docs/privacy.html` — §2a |
