RTLTMPro's text fixer (https://github.com/pnarimani/RTLTMPro, MIT, licence
beside this file), vendored at f480419: the pure string passes that turn
logical Arabic and Hebrew into what a renderer with no bidi and no shaping
can draw — Arabic letters into their joined presentation forms, lam-alef
into its ligature, mixed runs (Latin, digits, tags) kept in reading order,
brackets mirrored. Only the `Runtime` fixer files are here; the two
components are not, because every text in this game is built by
Ui.MakeText and the fix is applied by RtlText, a TextMeshPro subclass of
our own. Files are unmodified apart from this note; regenerate from the
upstream tag rather than editing them.
