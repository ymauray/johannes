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
}
