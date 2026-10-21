namespace Johannes;

/// <summary>
/// Vocabulaire typographique français partagé par la vérification et les exports.
/// </summary>
public static class Typography
{
	/// <summary>Espace insécable (U+00A0).</summary>
	public const char NoBreakSpace = '\u00A0';

	/// <summary>Espace fine insécable (U+202F).</summary>
	public const char NarrowNoBreakSpace = '\u202F';

	/// <summary>Tiret cadratin (U+2014).</summary>
	public const char EmDash = '\u2014';

	/// <summary>Apostrophe courbe (U+2019).</summary>
	public const char Apostrophe = '\u2019';

	public const char OpeningGuillemet = '«';

	public const char ClosingGuillemet = '»';

	public const char Ellipsis = '…';

	/// <summary><see cref="NoBreakSpace"/> sous forme de chaîne, pour les remplacements de texte.</summary>
	public const string NoBreakSpaceText = "\u00A0";

	/// <summary>Ponctuations doubles, précédées d'une insécable et suivies d'une espace.</summary>
	public const string DoublePunctuation = "!?:;";

	/// <summary>Vrai pour une espace insécable ou une fine insécable.</summary>
	public static bool IsNonBreakingSpace(char c) => c == NoBreakSpace || c == NarrowNoBreakSpace;

	/// <summary>Vrai pour une espace ordinaire, insécable ou fine insécable.</summary>
	public static bool IsSpace(char c) => c == ' ' || IsNonBreakingSpace(c);

	/// <summary>Vrai pour une espace (voir <see cref="IsSpace"/>) ou une tabulation.</summary>
	public static bool IsSpaceOrTab(char c) => IsSpace(c) || c == '\t';

	public static bool IsDoublePunctuation(char c) => DoublePunctuation.Contains(c);
}
