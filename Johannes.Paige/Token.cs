namespace Johannes.Paige;

public enum TokenType
{
	Directive,    // #metadata, #manifest.add
	LParen,       // (
	RParen,       // )
	Colon,        // :
	Comma,        // ,
	Ident,        // fr, camelCase, identifier…
	String,       // "quoted" — valeur sans guillemets
	Int,          // 12345
	Bool,         // true, false
	LBracket,     // [
	RBracket,     // ]
	BlockContent, // texte brut entre [ et ]
	Eof
}

public record Token(TokenType Type, string Value, int Line);
