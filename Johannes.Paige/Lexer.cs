namespace Johannes.Paige;

public class Lexer
{
	private readonly string _source;
	private int _pos;
	private int _line = 1;
	private readonly List<Token> _tokens = new();

	private Lexer(string source) => _source = source;

	public static Token[] Tokenize(string source) => new Lexer(source).Scan();

	private Token[] Scan()
	{
		while (_pos < _source.Length)
		{
			SkipWhitespace();
			if (_pos >= _source.Length) break;

			var c = _source[_pos];

			if (c == '#') ReadDirective();
			else if (c == '(') Emit(TokenType.LParen, "(");
			else if (c == ')') Emit(TokenType.RParen, ")");
			else if (c == ':') Emit(TokenType.Colon, ":");
			else if (c == ',') Emit(TokenType.Comma, ",");
			else if (c == '[') { Emit(TokenType.LBracket, "["); ReadBlockContent(); }
			else if (c == '"') ReadString();
			else if (char.IsDigit(c)) ReadInt();
			else if (char.IsLetter(c) || c == '_') ReadIdent();
			else _pos++;
		}

		_tokens.Add(new Token(TokenType.Eof, "", _line));
		return [.. _tokens];
	}

	private void Emit(TokenType type, string value) { _tokens.Add(new Token(type, value, _line)); _pos++; }

	private void SkipWhitespace()
	{
		while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos]))
		{
			if (_source[_pos] == '\n') _line++;
			_pos++;
		}
	}

	private void ReadDirective()
	{
		_pos++; // saute '#'
		var start = _pos;
		while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '.' || _source[_pos] == '_'))
			_pos++;
		_tokens.Add(new Token(TokenType.Directive, _source[start.._pos], _line));
	}

	private void ReadBlockContent()
	{
		var start = _pos;
		var startLine = _line;
		while (_pos < _source.Length && _source[_pos] != ']')
		{
			if (_source[_pos] == '\n') _line++;
			_pos++;
		}
		_tokens.Add(new Token(TokenType.BlockContent, _source[start.._pos], startLine));
		if (_pos < _source.Length)
		{
			_tokens.Add(new Token(TokenType.RBracket, "]", _line));
			_pos++;
		}
	}

	private void ReadString()
	{
		_pos++; // saute '"' ouvrant
		var start = _pos;
		while (_pos < _source.Length && _source[_pos] != '"')
		{
			if (_source[_pos] == '\n') _line++;
			_pos++;
		}
		_tokens.Add(new Token(TokenType.String, _source[start.._pos], _line));
		if (_pos < _source.Length) _pos++; // saute '"' fermant
	}

	private void ReadInt()
	{
		var start = _pos;
		while (_pos < _source.Length && char.IsDigit(_source[_pos]))
			_pos++;
		_tokens.Add(new Token(TokenType.Int, _source[start.._pos], _line));
	}

	private void ReadIdent()
	{
		var start = _pos;
		while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '-' || _source[_pos] == '_'))
			_pos++;
		var value = _source[start.._pos];
		var type = value is "true" or "false" ? TokenType.Bool : TokenType.Ident;
		_tokens.Add(new Token(type, value, _line));
	}
}
