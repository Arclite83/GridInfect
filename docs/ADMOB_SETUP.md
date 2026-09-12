# AdMob, IAP and the store identities — the setup pass

Companion to [`DEPENDENCIES.md`](DEPENDENCIES.md) §4–5 (which chose the stack
and recorded the API sequence) and [`REQUIREMENTS.md`](REQUIREMENTS.md) §6–8.
This file is the order of operations: what to do in a browser, what to do in
the editor, and what is already done in the repo.

Everything in the repo half is on `main` already. The browser half is yours —
it needs accounts this project cannot reach.

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
   configures the message text.
5. **Privacy & messaging ▸ Other regions / IDFA** can wait for the iOS follow
   (R-803).
6. On each ad unit, set **frequency capping** to roughly 12/hour and 20/day.
   That is deliberately looser than the in-app cadence gates so it never
   binds in normal play. It exists so a client bug cannot spam a user faster
   than you can push a fix.

### Do not click your own live ads

Register your device under **Settings ▸ Test devices** in the AdMob console,
or put its ID in `AdConfig.TestDeviceIds`. The SDK logs the device ID on
first run. An unregistered device tapping a live ad is the fastest way to
lose the account, and it is not recoverable by apologising.

Keep `AdConfig.UseTestAds` on for every build that is not a store release.

---

## 3. Unity import

1. Download `GoogleMobileAds-v11.4.0.unitypackage` from the googleads GitHub
   releases page and import it. The External Dependency Manager inside it
   resolves the Android native dependencies on its own.
2. **Player Settings ▸ Other Settings ▸ Scripting Define Symbols (Android):**
   add `GRIDINFECT_ADMOB`.

   This is what activates `Services/Sdk/AdMobServices.cs` and switches
   `Bootstrap` off the Null services. A `.unitypackage` cannot be detected by
   a version define, so it is a manual symbol — that is the one step with no
   automation behind it.
3. For IAP, add `com.unity.purchasing` 5.4.2 to the manifest. No symbol
   needed: the Services asmdef carries a `versionDefine` that raises
   `GRIDINFECT_IAP` when the package is present.
4. **Expect `Services/Sdk/` not to compile first try.** Those two files have
   never been built against the real SDKs. The consent sequence is
   transcribed from `DEPENDENCIES.md` §5 step 3 and should be sound; the
   ad-loading calls and the Unity IAP 5.x listener shape are the parts to fix
   against IntelliSense. Their file headers say exactly which is which.
5. Fill in `Assets ▸ Google Mobile Ads ▸ Settings` with the app ID from §2.

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

The GitHub workflow (`mobile-build.yml`) does the same builds headless,
but it needs the Unity licence secrets set and the plugin's files committed
(`Assets/GoogleMobileAds`, `Assets/ExternalDependencyManager`,
`Assets/Plugins/Android`, their `.meta` files). Commit them; that is
normal for a `.unitypackage`. It just is not the fastest route to the
first APK.

---

## 4. Google Play

1. Data safety: declare ads and the advertising ID. The plugin adds the
   `AD_ID` permission itself (R-605).
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
