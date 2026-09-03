#!/bin/sh
# Regenerates every launch world's level list (docs/worlds/*.jsonl) from
# its recorded GenSpec and seed range with tools/gen_levels. The bake
# (tools/bake_worlds.py) turns those files into WorldData.g.cs; the
# WorldTests regenerate a sample from the recorded seeds and compare, so a
# generator change shows up as a diff here.
#
#   tools/gen_worlds.sh [--threads N]
#
# Each world: id, name, level count, seed start, gen_levels flags. Grades
# ramp within a world (the bake orders by grade, then trace length) and
# across worlds. Launch content is cardinal arms plus walls (stage 3);
# later stages add one element per world.
set -e
cd "$(dirname "$0")/.."
THREADS="${2:-3}"
GEN="dotnet src/GenLevels/bin/Release/net8.0/GridInfect.GenLevels.dll"
dotnet build src/GenLevels/GridInfect.GenLevels.csproj -c Release --nologo -v q

world() {
  id="$1"; name="$2"; count="$3"; seed="$4"; shift 4
  out="docs/worlds/$id.jsonl"
  spec="$($GEN --spec-json "$@")"
  printf '{"world":{"id":"%s","name":"%s","elements":["walls"],"seed":%s,"spec":%s}}\n' "$id" "$name" "$seed" "$spec" > "$out"
  $GEN --count "$count" --seed "$seed" --threads "$THREADS" "$@" >> "$out"
}

world w01 "First Steps"  20 100000 --pieces 2-2 --grade G1 --max-run 4
world w02 "Two Lines"    22 110000 --pieces 2-3 --grade G1
world w03 "Corners"      22 120000 --pieces 3-3 --grade G1
world w04 "Crossings"    22 130000 --pieces 3-4 --grade G2
world w05 "Counting"     22 140000 --pieces 4-4 --grade G2
world w06 "Walls"        22 150000 --pieces 4-4 --grade G3 --max-walls 12
world w07 "Corridors"    22 160000 --pieces 4-5 --grade G3 --max-run 3
world w08 "Four Arms"    22 170000 --pieces 5-5 --grade G3
world w09 "Long Reach"   22 180000 --pieces 5-5 --grade G4 --max-run 5
world w10 "Tight"        22 190000 --pieces 5-5 --grade G4 --max-run 3
world w11 "Suppose"      20 200000 --pieces 5-5 --grade G5
world w12 "Mastery"      20 210000 --pieces 5-6 --grade G5
