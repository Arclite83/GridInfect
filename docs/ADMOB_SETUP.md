# AdMob, IAP and the store identities — the setup pass

Companion to [`DEPENDENCIES.md`](DEPENDENCIES.md) §4–5 (which chose the stack
and recorded the API sequence) and [`REQUIREMENTS.md`](REQUIREMENTS.md) §6–8.
This file is the order of operations: what to do in a browser, what to do in
the editor, and what is already done in the repo.

Everything in the repo half is on `main` already. The browser half is yours —
it needs accounts this project cannot reach.

---

## 0. Before anything: find the 2014 Android keystore

**Do this first. It decides the package name, and it is a five-minute check
that invalidates the rest of the plan if it comes back wrong.**

A Play listing can only be updated by something signed with its original key.
Play App Signing launched in 2017, so a 2014 app was almost certainly signed
with a local keystore and never enrolled. If that `.keystore`/`.jks` and its
passwords are gone, `com.bloodhoundstudios.gridinfect` cannot be updated —
and because the package name was published, it also cannot be reused for a
new listing. It would be burned in both directions.

Check for: the keystore file itself, the store password, the key alias, and
the alias password. All four, not just the file.

- **Found all four** → reuse `com.bloodhoundstudios.gridinfect`. See §1.
- **Missing any** → new package name, and open the Play Console to confirm
  whether the old listing is still enrolled in anything before assuming.

iOS has no equivalent trap: Apple manages signing certificates and you can
regenerate them from the developer account at will. The only question there
is whether the App Store Connect record still exists.

`mobile-build.yml` already takes the keystore as `GI_KEYSTORE_B64` plus its
passwords as secrets, and `ProjectSettings.asset` has
`androidUseCustomKeystore: 0` waiting to be turned on. The plumbing is done.

---

## 1. Store identities

`applicationIdentifier` is already `com.bloodhoundstudios.gridinfect` for both
platforms in `ProjectSettings.asset`.

This reopens `NEXT_PASS.md` decision 6 ("new app on the stores regardless"),
which was taken before anyone knew the old credentials survived.

**Reuse both, if the credentials are there.** The rebuild is the same game by
construction — the rules engine replays all 128 shipped levels against golden
board states pulled from the 2014 code. That is a remaster, not a sequel, and
an identifier is exactly the thing a remaster keeps. Reuse also hands you
whatever install base is left as a launch-day cohort, at no cost.

What you give up by reusing:

- **Lifetime ratings carry and cannot be reset.** Check the old listing's
  average before committing. If it is bad enough to hurt, that is the one
  real argument for a fresh package.
- Any old IAP products, pricing and Data safety answers ride along and need
  auditing rather than filling in fresh.

What you do *not* give up, contrary to the usual worry: the update-shock of
pushing a different game to existing owners. It is not a different game.

Decision to record in `NEXT_PASS.md` once you have checked the keystore and
the rating.

---

## 2. AdMob console

Free to set up; AdMob takes a revenue share rather than a fee.

1. Create an AdMob account and add the **Android** app. If the Play listing
   exists, link it — linking is what lets AdMob verify the app and is worth
   doing before you have traffic.
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
| Keystore plumbing | `.github/workflows/mobile-build.yml` |
