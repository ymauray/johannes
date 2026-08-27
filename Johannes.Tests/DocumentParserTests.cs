using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using Johannes;

namespace Johannes.Tests;

public class DocumentParserTests
{
	private class FakeExporter : IExporter
	{
		public void Paragraph(string styleId, List<ParagraphRun> runs) {}
		public void FinishExport() {}
	}

	[Fact]
	public void ParseParagraph_WithUnsupportedRunProperty_ShouldThrowInvalidOperationExceptionWithContext()
	{
		// Arrange
		var exporter = new FakeExporter();
		var parser = new DocumentParser(exporter);

		// Crée un paragraphe avec style "MonStyle" et du texte
		var paragraph = new Paragraph(
			new ParagraphProperties(new ParagraphStyleId { Val = "MonStyle" }),
			new Run(
				new RunProperties(new RunStyle { Val = "SomeStyle" }), // RunStyle n'est pas supporté par ParseRun
				new Text("Texte problématique")
			)
		);

		// Act & Assert
		var exception = Assert.Throws<InvalidOperationException>(() => parser.ParseParagraph(paragraph));
		Assert.Contains("Erreur lors de l'analyse du paragraphe", exception.Message);
		Assert.Contains("style: 'MonStyle'", exception.Message);
		Assert.Contains("texte brut: \"Texte problématique\"", exception.Message);
		Assert.Contains("Unsupported run property: RunStyle", exception.Message);
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

		var paragraph = new Paragraph(
			new ParagraphProperties(new ParagraphStyleId { Val = "Normal" }),
			new Run(
				new RunProperties(new Bold(), new BoldComplexScript()),
				new Text("Gras")
			),
			new Run(
				new RunProperties(new Italic(), new ItalicComplexScript()),
				new Text("Italique")
			)
		);

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

		var paragraph = new Paragraph(
			new ParagraphProperties(new ParagraphStyleId { Val = "Normal" }),
			new Run(
				new RunProperties(new RunFonts { Ascii = "Amazon Endure Book", HighAnsi = "Amazon Endure Book" }),
				new Text("Texte avec police")
			)
		);

		// Act
		parser.ParseParagraph(paragraph);

		// Assert
		Assert.Equal("Normal", capturedStyle);
		Assert.NotNull(capturedRuns);
		Assert.Single(capturedRuns);
		Assert.Equal("Texte avec police", capturedRuns[0].content);
		Assert.Equal("Amazon Endure Book", capturedRuns[0].font);
	}

	private class FakeExporterWithCapture(Action<string, List<ParagraphRun>> onParagraph) : IExporter
	{
		public void Paragraph(string styleId, List<ParagraphRun> runs) => onParagraph(styleId, runs);
		public void FinishExport() {}
	}
}
