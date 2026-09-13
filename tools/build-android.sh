#!/usr/bin/env bash
# Android build from a terminal (docs/UNITY_SETUP.md §8).
#
#   tools/build-android.sh [apk|aab] [--install]
#
#   apk        debug-signed, for a USB device (default)
#   aab        signed with the upload key, next versionCode, for Play
#   --install  after an apk build, adb install -r it on the connected phone
#
# Unity: $UNITY if set, else the Hub install for the version pinned in
# unity/ProjectSettings/ProjectVersion.txt (macOS and Linux paths).
# Signing: GI_KEYSTORE, GI_KEYSTORE_PASS, GI_KEYALIAS, GI_KEYALIAS_PASS from
# the environment, or from $GI_BUILD_ENV (default ~/.gridinfect/build.env),
# a file of `export GI_...=...` lines kept outside the repo. An aab build
# stops before Unity starts when GI_KEYSTORE is unset or missing: Play will
# not take a debug-signed bundle and the build takes minutes.
# The version code: MobileBuild takes the next one per aab build
# (GI_VERSION_CODE overrides). Commit the ProjectSettings.asset hunk.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
project="$root/unity"
format=apk
install=0
for arg in "$@"; do
  case "$arg" in
    apk|aab) format=$arg ;;
    --install) install=1 ;;
    -h|--help) sed -n '2,19p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
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
if [ $install -eq 1 ] && [ "$format" = apk ]; then
  adb install -r "$out"
fi
