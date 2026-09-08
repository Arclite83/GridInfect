#!/bin/sh
# Regenerates every launch world's level list (docs/worlds/*.jsonl) from
# its recorded GenSpec and seed range with tools/gen_levels. The bake
# (tools/bake_worlds.py) turns those files into WorldData.g.cs; the
# A generator change shows up as a diff here.
#
#   tools/gen_worlds.sh [--threads N] [--only w16]
#
# No pool passes --max-locks: GenSpec budgets it to 0, so no generated
# level ever ships a piece placed and locked before play.
#
# Each world: id, name, level count, seed start, gen_levels flags. Grades
# ramp within a world (the bake orders by grade, then trace length) and
# across worlds.
set -e
cd "$(dirname "$0")/.."
THREADS=4
ONLY=""
while [ $# -gt 0 ]; do
  case "$1" in
    --threads) THREADS="$2"; shift 2 ;;
    --only) ONLY="$2"; shift 2 ;;
    *) echo "unknown argument $1" >&2; exit 2 ;;
  esac
done
GEN="dotnet src/GenLevels/bin/Release/net8.0/GridInfect.GenLevels.dll"
dotnet build src/GenLevels/GridInfect.GenLevels.csproj -c Release --nologo -v q

world() {
  id="$1"; name="$2"; count="$3"; seed="$4"; shift 4
  if [ -n "$ONLY" ] && [ "$ONLY" != "$id" ]; then return; fi
  out="docs/worlds/$id.jsonl"
  spec="$($GEN --spec-json "$@")"
  printf '{"world":{"id":"%s","name":"%s","elements":%s,"seed":%s,"spec":%s}}\n' "$id" "$name" "$ELEMENTS" "$seed" "$spec" > "$out"
  $GEN --count "$count" --seed "$seed" --threads "$THREADS" "$@" >> "$out"
}

# The progression is linear on pieces and grade with one element entering
# at a time, each in its own world at three or four pieces and then in the
# pool of every world after it: forbidden cells first (no reading cost),
# the blot (local), diagonals (new line families), relays (chains). Ids
# stay in order (w13 is retired) so saved progress keeps its keys.
ELEMENTS='["walls"]'
world w01 "First Steps"  20 100000 --pieces 2-2 --grade G1 --max-run 4
world w02 "Two Lines"    22 110000 --pieces 2-3 --grade G1
world w03 "Corners"      22 120000 --pieces 3-3 --grade G1
world w04 "Crossings"    22 130000 --pieces 3-4 --grade G2

ELEMENTS='["walls","forbidden"]'
world w05 "Keep Clean"   22 140000 --pieces 3-4 --grade G2 --elements walls,forbidden --max-forbidden 4
world w06 "Counting"     22 150000 --pieces 4-4 --grade G2 --elements walls,forbidden --max-forbidden 3
world w07 "Corridors"    22 160000 --pieces 4-4 --grade G3 --elements walls,forbidden --max-forbidden 3 --max-run 3

ELEMENTS='["walls","forbidden","area"]'
world w08 "Blots"        22 170000 --pieces 3-4 --grades G2-G3 --elements walls,forbidden,area --area-chance 8 --max-forbidden 2
world w09 "Long Reach"   22 180000 --pieces 4-5 --grade G3 --elements walls,forbidden,area --area-chance 5 --max-forbidden 3
world w10 "Four Arms"    22 190000 --pieces 5-5 --grade G3 --elements walls,forbidden,area --area-chance 5 --max-forbidden 3

ELEMENTS='["walls","forbidden","area","diagonals"]'
world w11 "Diagonals"    22 200000 --pieces 4-4 --grades G2-G3 --elements walls,forbidden,area,diagonals --diagonal-chance 14 --area-chance 3 --max-forbidden 2
world w12 "Tight"        22 210000 --pieces 5-5 --grade G4 --elements walls,forbidden,area,diagonals --diagonal-chance 8 --area-chance 4 --max-forbidden 3 --max-run 3

ELEMENTS='["walls","forbidden","area","diagonals","relays"]'
world w14 "Relays"       22 230000 --pieces 4-4 --grade G3 --elements walls,forbidden,area,diagonals,relays --relay-chance 14 --diagonal-chance 4 --area-chance 3 --max-forbidden 2
world w15 "Crosstalk"    22 240000 --pieces 5-5 --grade G4 --elements walls,forbidden,area,diagonals,relays --relay-chance 8 --diagonal-chance 8 --area-chance 4 --max-forbidden 3
world w16 "Suppose"      20 250000 --pieces 5-5 --grade G5 --elements walls,forbidden,area,diagonals,relays --relay-chance 6 --diagonal-chance 6 --area-chance 4 --max-forbidden 4
world w17 "Mastery"      20 260000 --pieces 5-6 --grade G5 --elements walls,forbidden,area,diagonals,relays --relay-chance 6 --diagonal-chance 6 --area-chance 4 --max-forbidden 4
