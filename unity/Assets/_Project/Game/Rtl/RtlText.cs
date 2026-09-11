using RTLTMPro;
using TMPro;

namespace GridInfect.Game
{
    // The TextMeshPro every Ui.MakeText builds. For a shaped language
    // (Str.IsShaped: Arabic, Hebrew) the text set on it is run through the
    // vendored RTLTMPro fixer — joined Arabic forms, mirrored brackets,
    // Latin and digit runs kept in reading order — reversed into visual
    // order, and drawn with TMP's right-to-left flag. For every other
    // language it is a TextMeshPro. The original string is kept so that a
    // language change can re-fix, and so that measurement can ask for the
    // visual text (`text`) rather than the logical one.
    public sealed class RtlText : TextMeshPro
    {
        static readonly FastStringBuilder Fixed = new FastStringBuilder(RTLSupport.DefaultBufferSize);

        string _logical;

        public string Logical => _logical;

        public override string text
        {
            get => base.text;
            set
            {
                _logical = value ?? "";
                if (Str.IsShaped && TextUtils.IsRTLInput(_logical))
                {
                    isRightToLeftText = true;
                    base.text = Fix(_logical);
                }
                else
                {
                    isRightToLeftText = false;
                    base.text = _logical;
                }
            }
        }

        // farsi: false (Arabic forms and Arabic-Indic numerals off);
        // preserveNumbers: true, since every number the game shows is a
        // Western-digit readout (ARCHITECTURE.md §8); fixTags: false, since
        // richText is off and a '<' is a '<'.
        static string Fix(string logical)
        {
            Fixed.Clear();
            RTLSupport.FixRTL(logical, Fixed, farsi: false, fixTextTags: false, preserveNumbers: true);
            Fixed.Reverse();
            return Fixed.ToString();
        }
    }
}
