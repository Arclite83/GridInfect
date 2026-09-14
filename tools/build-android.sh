#!/usr/bin/env bash
# Android build from a terminal (docs/UNITY_SETUP.md §8).
#
#   tools/build-android.sh [apk|aab] [--dev] [--install] [--no-commit]
#
#   apk          debug-signed, for a USB device (default)
#   aab          signed with the upload key, next versionCode, for Play
#   --dev        development build: full C# stack traces in logcat (apk only)
#   --install    after an apk build, adb install -r it on the connected phone
#   --no-commit  after an aab build, leave the versionCode hunk uncommitted
#
# Unity: $UNITY if set, else the Hub install for the version pinned in
# unity/ProjectSettings/ProjectVersion.txt (macOS and Linux paths). adb:
# $ADB if set, else the one inside that editor's Android SDK, else PATH.
# Signing: GI_KEYSTORE, GI_KEYSTORE_PASS, GI_KEYALIAS, GI_KEYALIAS_PASS from
# the environment, or from $GI_BUILD_ENV (default ~/.gridinfect/build.env),
# a file of `export GI_...=...` lines kept outside the repo. An aab build
# stops before Unity starts when GI_KEYSTORE is unset or missing: Play will
# not take a debug-signed bundle and the build takes minutes.
# The version code: MobileBuild takes the next one per aab build
# (GI_VERSION_CODE overrides) and writes it to ProjectSettings.asset. After
# a successful aab build the script commits that file as "build <code>",
# the way the hand-made commits were, unless --no-commit is given or the
# file already had uncommitted changes before the build (then the hunk is
# left in the tree and the script says so). Nothing is pushed.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
project="$root/unity"
format=apk
install=0
commit=1
for arg in "$@"; do
  case "$arg" in
    apk|aab) format=$arg ;;
    --install) install=1 ;;
    --no-commit) commit=0 ;;
    --dev) export GI_DEV=1 ;;
    -h|--help) sed -n '2,25p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "unknown argument: $arg" >&2; exit 2 ;;
  esac
done

env_file="${GI_BUILD_ENV:-$HOME/.gridinfect/build.env}"
if [ -f "$env_file" ]; then
  # shellcheck disable=SC1090
  . "$env_file"
fi

version=$(sed -n 's/^m_EditorVersion: //p' "$project/ProjectSettings/ProjectVersion.txt")
if [ -z "${UNITY:-}" ]; then
  for candidate in \
    "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" \
    "$HOME/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" \
    "$HOME/Unity/Hub/Editor/$version/Editor/Unity"; do
    if [ -x "$candidate" ]; then UNITY=$candidate; break; fi
  done
fi
if [ -z "${UNITY:-}" ] || [ ! -x "$UNITY" ]; then
  echo "Unity $version not found; set UNITY to the editor binary" >&2
  exit 1
fi

if [ "$format" = aab ] && [ "${GI_DEV:-}" = 1 ]; then
  echo "--dev is for the apk; a development AAB is not something to upload" >&2
  exit 2
fi

if [ "$format" = aab ]; then
  method=GridInfect.EditorTools.MobileBuild.Android
  if [ -z "${GI_KEYSTORE:-}" ]; then
    echo "GI_KEYSTORE is not set. Export the four GI_* variables or put them in $env_file" >&2
    exit 1
  fi
  if [ ! -f "$GI_KEYSTORE" ]; then
    echo "GI_KEYSTORE=$GI_KEYSTORE does not exist" >&2
    exit 1
  fi
  export GI_KEYSTORE GI_KEYSTORE_PASS GI_KEYALIAS GI_KEYALIAS_PASS
else
  method=GridInfect.EditorTools.MobileBuild.AndroidApk
fi

if [ -f "$project/Temp/UnityLockfile" ]; then
  echo "the project is open in the editor (Temp/UnityLockfile); close it, or build from the Grid Infect menu" >&2
  exit 1
fi

# Whether the settings file is clean going in decides whether the versionCode
# hunk can be committed on its own afterwards.
asset=unity/ProjectSettings/ProjectSettings.asset
asset_dirty=0
if [ "$format" = aab ] && [ $commit -eq 1 ]; then
  if ! git -C "$root" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "not a git checkout; the versionCode hunk will not be committed" >&2
    commit=0
  elif ! git -C "$root" diff --quiet -- "$asset"; then
    asset_dirty=1
  fi
fi

mkdir -p "$project/Builds"
log="$project/Builds/build-$format.log"
echo "Unity $version, $format, log $log"
set +e
"$UNITY" -batchmode -quit -projectPath "$project" -executeMethod "$method" -logFile "$log"
status=$?
set -e
grep -E '^\[build\]|error CS[0-9]+|BuildFailedException' "$log" || true
if [ $status -ne 0 ]; then
  echo "build failed (exit $status); see $log" >&2
  exit $status
fi
out="$project/Builds/gridinfect.$format"
if [ ! -f "$out" ]; then
  echo "Unity exited 0 but $out is missing; see $log" >&2
  exit 1
fi
echo "$out"
if [ "$format" = aab ] && [ $commit -eq 1 ]; then
  if git -C "$root" diff --quiet -- "$asset"; then
    echo "$asset unchanged (GI_VERSION_CODE equal to the stored code?); nothing to commit"
  elif [ $asset_dirty -eq 1 ]; then
    echo "$asset had uncommitted changes before the build; not committing the versionCode hunk with them. Commit it yourself." >&2
  else
    code=$(sed -n 's/^ *AndroidBundleVersionCode: *//p' "$root/$asset")
    if ! git -C "$root" commit -q -m "build $code" -- "$asset"; then
      echo "commit failed; the versionCode hunk is still in the tree" >&2
      exit 1
    fi
    echo "committed \"build $code\" ($(git -C "$root" rev-parse --short HEAD)); push it before the next upload build elsewhere"
  fi
fi
if [ $install -eq 1 ] && [ "$format" = apk ]; then
  # The editor's own SDK: <version>/PlaybackEngines/AndroidPlayer on macOS,
  # Editor/Data/PlaybackEngines/AndroidPlayer on Linux. Walk up from the binary.
  adb=${ADB:-}
  dir=$(dirname "$UNITY")
  while [ -z "$adb" ] && [ "$dir" != / ]; do
    for c in "$dir/PlaybackEngines" "$dir/Data/PlaybackEngines"; do
      if [ -x "$c/AndroidPlayer/SDK/platform-tools/adb" ]; then adb="$c/AndroidPlayer/SDK/platform-tools/adb"; break; fi
    done
    dir=$(dirname "$dir")
  done
  [ -n "$adb" ] || adb=adb
  # A copy delivered by Play is signed with Google's key; a debug APK cannot
  # update it in place. Remove it and retry (the app has no data worth keeping).
  if ! "$adb" install -r "$out" 2>&1 | tee /dev/stderr | grep -q INSTALL_FAILED_UPDATE_INCOMPATIBLE; then
    :
  else
    echo "signature mismatch with the installed copy; uninstalling it and retrying"
    "$adb" uninstall com.bloodhoundstudios.gridinfect.app
    "$adb" install -r "$out"
  fi
  echo "installed; for the crash: \"$adb\" logcat -c, launch, then \"$adb\" logcat -d > crash.txt"
fi
