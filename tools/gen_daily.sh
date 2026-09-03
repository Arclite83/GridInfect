#!/bin/sh
# Regenerates the Daily's seven pools (docs/daily/d1..d7.jsonl, Monday to
# Sunday) from the weekday specs in DailySpec (gen_levels --daily). The
# bake (tools/bake_worlds.py) turns them into DailyData.g.cs; DailyTests
# regenerate a sample from the recorded seeds and compare. A date opens
# its weekday's pool at the week number since DailyPool.Epoch, so 52
# levels a pool is a year without a repeat.
#
#   tools/gen_daily.sh [--threads N] [--only d3]
set -e
cd "$(dirname "$0")/.."
THREADS=3
ONLY=""
COUNT=52
while [ $# -gt 0 ]; do
  case "$1" in
    --threads) THREADS="$2"; shift 2 ;;
    --only) ONLY="$2"; shift 2 ;;
    --count) COUNT="$2"; shift 2 ;;
    *) echo "unknown argument $1" >&2; exit 2 ;;
  esac
done
GEN="dotnet src/GenLevels/bin/Release/net8.0/GridInfect.GenLevels.dll"
dotnet build src/GenLevels/GridInfect.GenLevels.csproj -c Release --nologo -v q

# The element list mirrors DailySpec.ElementsFor; DailyTests check the two agree.
pool() {
  id="$1"; day="$2"; elements="$3"; seed="$4"
  if [ -n "$ONLY" ] && [ "$ONLY" != "$id" ]; then return; fi
  out="docs/daily/$id.jsonl"
  spec="$($GEN --spec-json --daily "$day")"
  printf '{"world":{"id":"%s","name":"%s","elements":%s,"seed":%s,"spec":%s}}\n' "$id" "$day" "$elements" "$seed" "$spec" > "$out"
  $GEN --daily "$day" --count "$COUNT" --seed "$seed" --threads "$THREADS" >> "$out"
}

pool d1 Monday    '["walls"]'                                        1100000
pool d2 Tuesday   '["walls","shortarms"]'                            1200000
pool d3 Wednesday '["walls","area"]'                                 1300000
pool d4 Thursday  '["walls","forbidden"]'                            1400000
pool d5 Friday    '["walls","diagonals"]'                            1500000
pool d6 Saturday  '["walls","relays"]'                               1600000
pool d7 Sunday    '["walls","shortarms","forbidden","diagonals"]'    1700000
