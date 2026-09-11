using System.Globalization;
using UnityEngine;

namespace GridInfect.Game
{
    // The adapter's string table. The prose itself is generated
    // (Strings.g.cs, from docs/strings/*.json); this half is the parts a
    // generator has no business writing: which tag is in force, how the
    // device's language maps onto the shipped set, and the one formatter
    // every substitution goes through.
    //
    // Core holds keys and never prose (ARCHITECTURE.md §8), so every word the
    // player reads is resolved here, in the presentation layer, exactly as
    // the skin's colours are.
    public static partial class Str
    {
        // Every number the game shows is a scoreboard readout in a mono
        // column, never prose: a solve count, a streak, a level number, a
        // duration. Ambient culture would render those in the device's own
        // digits and separators and the column would stop lining up, so
        // every substitution is invariant (ARCHITECTURE.md §8). Text order is
        // the translation's business; digits are not.
        public static string Fmt(string format, object a0) =>
            string.Format(CultureInfo.InvariantCulture, format, a0);

        public static string Fmt(string format, object a0, object a1) =>
            string.Format(CultureInfo.InvariantCulture, format, a0, a1);

        public static string Fmt(string format, object a0, object a1, object a2) =>
            string.Format(CultureInfo.InvariantCulture, format, a0, a1, a2);

        // A bare number with no sentence around it: a calendar day, a level
        // number on a tile, a year. Same rule as Fmt, and the reason to have
        // it is that `n.ToString()` is the easy thing to type and is wrong.
        public static string Num(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        // The difficulty band a weekday or a level carries. Grade's own names
        // (G1..G5) stay the stored contract in Core; these are the words over
        // them. A band whose ends match is just the one tier.
        public static string TierBandLabel(Core.Solving.Grade min, Core.Solving.Grade max) =>
            min == max ? Fmt(TierShort, (int)min)
                : Fmt(TierBand, Fmt(TierShort, (int)min), (int)max);

        // Whether the language in force reads right to left. Chrome mirrors
        // on this (ARCHITECTURE.md §8); the board never does. The RTL pseudolocale
        // is how it is exercised before any such language ships.
        public static bool IsRtl
        {
            get
            {
                string tag = CurrentTag;
                if (tag == "qps-plocm") return true;
                int cut = tag.IndexOf('-');
                string primary = cut < 0 ? tag : tag.Substring(0, cut);
                switch (primary)
                {
                    case "ar": case "he": case "fa": case "ur": return true;
                    default: return false;
                }
            }
        }

        // Whether the language in force needs its text shaped and reordered
        // before TMP draws it (RtlText). A subset of IsRtl: the mirrored
        // pseudolocale reads right to left for layout and is not shaped.
        public static bool IsShaped
        {
            get
            {
                string tag = CurrentTag;
                int cut = tag.IndexOf('-');
                switch (cut < 0 ? tag : tag.Substring(0, cut))
                {
                    case "ar": case "he": case "fa": case "ur": return true;
                    default: return false;
                }
            }
        }

        // Adopt `tag` if it ships, and report whether it did. An unknown tag
        // leaves the current one in force: a save written by a build that
        // shipped more languages than this one must not blank the UI.
        public static bool Use(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i] == tag)
                {
                    Bind(i);
                    return true;
                }
            }
            return false;
        }

        // The stored preference, else the device, else English. An empty
        // stored tag means "follow the device", which is the default and
        // stays true when the player changes the device's language later.
        public static void Resolve(string storedTag)
        {
            if (Use(storedTag)) return;
            if (Use(FromDevice(Application.systemLanguage))) return;
            Use(DefaultTag);
        }

        // Unity's SystemLanguage is a language, not a locale, so it cannot
        // tell pt-BR from pt-PT or zh-Hans from zh-Hant. Where the shipped
        // set has exactly one variant of a language, that variant is the
        // answer; where it ever has two, the player picks and the stored tag
        // wins from then on. Chinese resolves to Simplified because
        // SystemLanguage.ChineseTraditional exists and would have been
        // reported instead had the device meant Traditional.
        static string FromDevice(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.English: return "en";
                case SystemLanguage.German: return "de";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Portuguese: return "pt-BR";
                case SystemLanguage.Italian: return "it";
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Chinese: return "zh-Hans";
                case SystemLanguage.ChineseSimplified: return "zh-Hans";
                case SystemLanguage.ChineseTraditional: return "zh-Hant";
                case SystemLanguage.Arabic: return "ar";
                case SystemLanguage.Hebrew: return "he";
                default: return null;
            }
        }
    }
}
