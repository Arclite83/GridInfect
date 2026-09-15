using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // The daily streak as a meter. The counter badge (mono copperHi on
    // black, the SOLVE counter's material) carries the streak's number and,
    // after it, the world meter's track and fill cut into seven, one
    // segment a day of the current week: day 9 lights two, the number says
    // nine. Copper pads on the badge's lower edge mark the rungs of the
    // ladder (Rewards): under 7 always, under 3 until that one-off is
    // taken. The calendar draws it under the well and the daily's COMPLETE
    // popup draws the same one, so a grant is read off the same bar it was
    // counted on.
    public static class StreakBar
    {
        const float SegW = 14f, SegH = 12f, SegGap = 3f, LabelGap = 10f;

        static float SegmentsPx => S.Px(Rewards.StreakEvery * SegW + (Rewards.StreakEvery - 1) * SegGap);

        // The box for a label: the badge's own box for that reading, plus
        // the segments.
        public static Vector2 Box(string label)
        {
            var badge = Ui.BadgeBox(label);
            return new Vector2(badge.x + S.Px(LabelGap) + SegmentsPx, badge.y);
        }

        // `firstPad`: whether the rung at 3 is still marked.
        public static GameObject Make(Transform parent, Vector2 centre, int streak, bool firstPad, int sortingOrder)
        {
            string label = Str.Fmt(Str.DailyStreak, streak);
            var box = Box(label);
            var root = new GameObject("streak");
            root.transform.SetParent(parent, false);
            Ui.SetPos(root, centre.x, centre.y);
            Ui.MakeGlass("badge", root.transform, box, GlassStyle.Badge(BoardPalette.Default), sortingOrder);

            // The label hangs from the leading edge inside the badge's
            // padding; the segments fill from beside it along the reading
            // direction, against the trailing edge.
            float padX = S.Px(S.BadgePadX);
            var text = Ui.MakeText("label", root.transform, label, Ui.LabelPx(new Vector2(0f, box.y)), BoardTheme.Copper,
                sortingOrder + 1, mono: true, anchor: L.Leading);
            Ui.SetPos(text.gameObject, L.Dir * (-box.x / 2f + padX), 0f);

            float segW = S.Px(SegW), segH = S.Px(SegH), pitch = S.Px(SegW + SegGap);
            float firstX = box.x / 2f - padX - SegmentsPx + segW / 2f;
            int lit = Rewards.StreakCycle(streak);
            float dot = S.Px(S.ChipPadDot);
            for (int i = 0; i < Rewards.StreakEvery; i++)
            {
                float x = L.Dir * (firstX + i * pitch);
                var seg = Ui.MakeGlass($"seg:{i + 1}", root.transform, new Vector2(segW, segH),
                    i < lit ? BoardTheme.MeterFill() : BoardTheme.MeterTrack(), sortingOrder + 1);
                Ui.SetPos(seg, x, 0f);
                int day = i + 1;
                bool rung = day == Rewards.StreakEvery || (day == Rewards.StreakFirst && firstPad);
                if (!rung) continue;
                var pad = Ui.MakeGlass($"pad:{day}", root.transform, new Vector2(dot, dot), GlassStyle.Pad(BoardPalette.Default), sortingOrder + 2);
                Ui.SetPos(pad, x, -box.y / 2f);
            }
            return root;
        }
    }
}
