using Johannes.Paige;

namespace Johannes.Tests;

public class LexerTests
{
	// --- Token type tests ---

	[Fact]
	public void Directive_Metadata_ProducesDirectiveToken()
	{
		var tokens = Lexer.Tokenize("#metadata(");
		Assert.Equal(TokenType.Directive, tokens[0].Type);
		Assert.Equal("metadata", tokens[0].Value);
	}

	[Fact]
	public void Directive_ManifestAdd_ProducesDirectiveToken()
	{
		var tokens = Lexer.Tokenize("#manifest.add(");
		Assert.Equal(TokenType.Directive, tokens[0].Type);
		Assert.Equal("manifest.add", tokens[0].Value);
	}

	[Fact]
	public void IntLiteral_ProducesIntToken()
	{
		var tokens = Lexer.Tokenize("12345");
		Assert.Equal(TokenType.Int, tokens[0].Type);
		Assert.Equal("12345", tokens[0].Value);
	}

	[Fact]
	public void StringLiteral_ProducesStringTokenWithoutQuotes()
	{
		var tokens = Lexer.Tokenize(""""
            "Les Ailéris : Anya"
            """");
		Assert.Equal(TokenType.String, tokens[0].Type);
		Assert.Equal("Les Ailéris : Anya", tokens[0].Value);
	}

	[Fact]
	public void BoolLiteral_True_ProducesBoolToken()
	{
		var tokens = Lexer.Tokenize("true");
		Assert.Equal(TokenType.Bool, tokens[0].Type);
		Assert.Equal("true", tokens[0].Value);
	}

	[Fact]
	public void BoolLiteral_False_ProducesBoolToken()
	{
		var tokens = Lexer.Tokenize("false");
		Assert.Equal(TokenType.Bool, tokens[0].Type);
		Assert.Equal("false", tokens[0].Value);
	}

	[Fact]
	public void Identifier_ProducesIdentToken()
	{
		var tokens = Lexer.Tokenize("fr");
		Assert.Equal(TokenType.Ident, tokens[0].Type);
		Assert.Equal("fr", tokens[0].Value);
	}

	// --- Punctuation tests ---

	[Fact]
	public void Punctuation_Parens_ProduceCorrectTokens()
	{
		var tokens = Lexer.Tokenize("()");
		Assert.Equal(TokenType.LParen, tokens[0].Type);
		Assert.Equal(TokenType.RParen, tokens[1].Type);
	}

	[Fact]
	public void Punctuation_ColonAndComma_ProduceCorrectTokens()
	{
		var tokens = Lexer.Tokenize("a: b,");
		Assert.Equal(TokenType.Ident, tokens[0].Type);
		Assert.Equal(TokenType.Colon, tokens[1].Type);
		Assert.Equal(TokenType.Ident, tokens[2].Type);
		Assert.Equal(TokenType.Comma, tokens[3].Type);
	}

	// --- Block content tests ---

	[Fact]
	public void BlockContent_CapturesRawTextBetweenBrackets()
	{
		var tokens = Lexer.Tokenize("[<body><p>Hello</p></body>]");
		Assert.Equal(TokenType.LBracket, tokens[0].Type);
		Assert.Equal(TokenType.BlockContent, tokens[1].Type);
		Assert.Equal("<body><p>Hello</p></body>", tokens[1].Value);
		Assert.Equal(TokenType.RBracket, tokens[2].Type);
	}

	[Fact]
	public void BlockContent_MultilineIsPreserved()
	{
		var tokens = Lexer.Tokenize("[\n<p>A</p>\n<p>B</p>\n]");
		Assert.Equal(TokenType.BlockContent, tokens[1].Type);
		Assert.Contains("\n", tokens[1].Value);
	}

	[Fact]
	public void EmptyBlock_ProducesEmptyBlockContent()
	{
		var tokens = Lexer.Tokenize("[]");
		Assert.Equal(TokenType.LBracket, tokens[0].Type);
		Assert.Equal(TokenType.BlockContent, tokens[1].Type);
		Assert.Equal("", tokens[1].Value);
		Assert.Equal(TokenType.RBracket, tokens[2].Type);
	}

	// --- EOF tests ---

	[Fact]
	public void LastToken_IsAlwaysEof()
	{
		var tokens = Lexer.Tokenize("anything");
		Assert.Equal(TokenType.Eof, tokens[^1].Type);
	}

	[Fact]
	public void EmptySource_ProducesOnlyEof()
	{
		var tokens = Lexer.Tokenize("");
		Assert.Single(tokens);
		Assert.Equal(TokenType.Eof, tokens[0].Type);
	}

	// --- Fixture tests ---

	[Fact]
	public void SampleFixture_TokenizesWithoutError()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var tokens = Lexer.Tokenize(source);
		Assert.NotEmpty(tokens);
		Assert.Equal(TokenType.Eof, tokens[^1].Type);
	}

	[Fact]
	public void SampleFixture_FirstTokenIsMetadataDirective()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var tokens = Lexer.Tokenize(source);
		Assert.Equal(TokenType.Directive, tokens[0].Type);
		Assert.Equal("metadata", tokens[0].Value);
	}

	[Fact]
	public void SampleFixture_ContainsThreeManifestAddDirectives()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var tokens = Lexer.Tokenize(source);
		var count = tokens.Count(t => t.Type == TokenType.Directive && t.Value == "manifest.add");
		Assert.Equal(3, count);
	}

	[Fact]
	public void SampleFixture_ThirdDirectiveHasBlockContent()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var tokens = Lexer.Tokenize(source);
		var blockContent = tokens.FirstOrDefault(t => t.Type == TokenType.BlockContent);
		Assert.NotNull(blockContent);
		Assert.Contains("<body>", blockContent.Value);
	}

	private static string FixturePath(string name) =>
		Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
