using System.Xml.Linq;
using Xunit;

namespace Johannes.Tests;

public class DocumentParserTests
{
	private static readonly XNamespace W = WordXml.W;

	private class FakeExporter : IExporter
	{
		public void Paragraph(string styleId, List<ParagraphRun> runs) { }
		public void FinishExport() { }
	}

	private static XElement Paragraph(string style, params XElement[] runs) =>
		new(W + "p",
			new XElement(W + "pPr", new XElement(W + "pStyle", new XAttribute(W + "val", style))),
			runs);

	private static XElement Run(string text, params XElement[] properties) =>
		new(W + "r",
			properties.Length > 0 ? new XElement(W + "rPr", properties) : null,
			new XElement(W + "t", text));

	[Fact]
	public void ParseParagraph_WithUnsupportedRunProperty_ShouldThrowInvalidOperationExceptionWithContext()
	{
		// Arrange
		var exporter = new FakeExporter();
		var parser = new DocumentParser(exporter);

		// RunStyle n'est pas supporté par ParseRun
		var paragraph = Paragraph("MonStyle",
			Run("Texte problématique", new XElement(W + "rStyle", new XAttribute(W + "val", "SomeStyle"))));

		// Act & Assert
		var exception = Assert.Throws<InvalidOperationException>(() => parser.ParseParagraph(paragraph));
		Assert.Contains("Erreur lors de l'analyse du paragraphe", exception.Message);
		Assert.Contains("style: 'MonStyle'", exception.Message);
		Assert.Contains("texte brut: \"Texte problématique\"", exception.Message);
		Assert.Contains("Unsupported run property: rStyle", exception.Message);
	}

	[Fact]
	public void ParseParagraph_WithBoldAndItalicRuns_ShouldParsePropertiesCorrectly()
	{
		// Arrange
		string? capturedStyle = null;
		List<ParagraphRun>? capturedRuns = null;
		var exporter = new FakeExporterWithCapture((style, runs) =>
		{
			capturedStyle = style;
			capturedRuns = runs;
		});
		var parser = new DocumentParser(exporter);

		var paragraph = Paragraph("Normal",
			Run("Gras", new XElement(W + "b"), new XElement(W + "bCs")),
			Run("Italique", new XElement(W + "i"), new XElement(W + "iCs")));

		// Act
		parser.ParseParagraph(paragraph);

		// Assert
		Assert.Equal("Normal", capturedStyle);
		Assert.NotNull(capturedRuns);
		Assert.Equal(2, capturedRuns.Count);
		Assert.Equal("Gras", capturedRuns[0].content);
		Assert.True(capturedRuns[0].isBold);
		Assert.False(capturedRuns[0].isItalic);
		Assert.Equal("Italique", capturedRuns[1].content);
		Assert.False(capturedRuns[1].isBold);
		Assert.True(capturedRuns[1].isItalic);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("false")]
	[InlineData("off")]
	public void ParseParagraph_WithDisabledBold_ShouldNotBeBold(string value)
	{
		List<ParagraphRun>? capturedRuns = null;
		var parser = new DocumentParser(new FakeExporterWithCapture((_, runs) => capturedRuns = runs));

		parser.ParseParagraph(Paragraph("Normal",
			Run("Texte", new XElement(W + "b", new XAttribute(W + "val", value)))));

		Assert.NotNull(capturedRuns);
		Assert.False(capturedRuns[0].isBold);
	}

	[Fact]
	public void ParseParagraph_WithRunFonts_ShouldParseFontPropertyCorrectly()
	{
		// Arrange
		string? capturedStyle = null;
		List<ParagraphRun>? capturedRuns = null;
		var exporter = new FakeExporterWithCapture((style, runs) =>
		{
			capturedStyle = style;
			capturedRuns = runs;
		});
		var parser = new DocumentParser(exporter);

		var paragraph = Paragraph("Normal",
			Run("Texte avec police", new XElement(W + "rFonts",
				new XAttribute(W + "ascii", "Amazon Endure Book"),
				new XAttribute(W + "hAnsi", "Amazon Endure Book"))));

		// Act
		parser.ParseParagraph(paragraph);

		// Assert
		Assert.Equal("Normal", capturedStyle);
		Assert.NotNull(capturedRuns);
		Assert.Single(capturedRuns);
		Assert.Equal("Texte avec police", capturedRuns[0].content);
		Assert.Equal("Amazon Endure Book", capturedRuns[0].font);
	}

	[Fact]
	public void ParseParagraph_WithoutStyle_ShouldDefaultToNormal()
	{
		string? capturedStyle = null;
		var parser = new DocumentParser(new FakeExporterWithCapture((style, _) => capturedStyle = style));

		parser.ParseParagraph(new XElement(W + "p", Run("Texte")));

		Assert.Equal("Normal", capturedStyle);
	}

	[Fact]
	public void ParseParagraph_WithForeignElementNamedLikeWordOne_ShouldBeRejected()
	{
		var parser = new DocumentParser(new FakeExporter());
		XNamespace other = "urn:autre";
		var paragraph = new XElement(W + "p", new XElement(other + "r"));

		var exception = Assert.Throws<InvalidOperationException>(() => parser.ParseParagraph(paragraph));
		Assert.Contains("Unsupported inline element", exception.Message);
	}

	[Fact]
	public void ParseParagraph_WithIgnoredElements_ShouldSkipThem()
	{
		List<ParagraphRun>? capturedRuns = null;
		var parser = new DocumentParser(new FakeExporterWithCapture((_, runs) => capturedRuns = runs));

		parser.ParseParagraph(new XElement(W + "p",
			new XElement(W + "bookmarkStart"),
			new XElement(W + "proofErr"),
			Run("Texte"),
			new XElement(W + "bookmarkEnd")));

		Assert.NotNull(capturedRuns);
		Assert.Single(capturedRuns);
	}

	private class FakeExporterWithCapture(Action<string, List<ParagraphRun>> onParagraph) : IExporter
	{
		public void Paragraph(string styleId, List<ParagraphRun> runs) => onParagraph(styleId, runs);
		public void FinishExport() { }
	}
}
