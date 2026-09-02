#!/usr/bin/env python3
"""Golden solution counts for the 128 classic levels.

Runs tools/level_metrics.py's exact search over docs/test_vectors.json and
writes docs/level_metrics_classic.json: one row per level with the
order-feasible solution count, the static (coverage-only) count, and the
fewest pieces any solution uses. GridInfect.Core.Solving.SolutionCounter is
the C# product; this file is the oracle it must match (SolverTests), and CI
regenerates it to prove it is current.

    python3 tools/gen_level_metrics_golden.py
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools"))
from level_metrics import solve  # noqa: E402

OUTPUT = os.path.join(ROOT, "docs", "level_metrics_classic.json")


def main():
    levels = json.load(open(os.path.join(ROOT, "docs", "test_vectors.json")))["levels"]
    rows = []
    for key in sorted(levels, key=int):
        lv = levels[key]
        r = solve(lv["board"], lv["pieces"], cap=200000, tlimit=120.0)
        if r["capped"] or r["timeout"]:
            sys.exit(f"level {key}: search capped or timed out; golden would be inexact")
        rows.append({
            "id": int(key),
            "solutions": r["solutions"],
            "static": r["static"],
            "min_pieces": r["min_pieces"],
        })
        print(f"level {key}: {r['solutions']} solutions", flush=True)
    with open(OUTPUT, "w") as f:
        json.dump({"_meta": {"source": "tools/gen_level_metrics_golden.py over docs/test_vectors.json",
                             "semantics": "solutions = order-feasible covering sets found by "
                                          "level_metrics.solve; static = before the order check"},
                   "levels": rows}, f, indent=1)
        f.write("\n")
    print(f"wrote {OUTPUT}: {len(rows)} levels, {sum(1 for r in rows if r['solutions'] == 1)} unique")


if __name__ == "__main__":
    main()
