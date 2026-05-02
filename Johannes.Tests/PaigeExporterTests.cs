using Johannes;
using Xunit;

namespace Johannes.Tests;

public class PaigeExporterTests
{
	[Theory]
	[InlineData("Test ?", "Test&#160;?")]
	[InlineData("Attention !", "Attention&#160;!")]
	[InlineData("Exemple :", "Exemple&#160;:")]
	[InlineData("Fin ;", "Fin&#160;;")]
	public void UnRun_ShouldAddHtmlEntitiesForNonBreakingSpaces(string input, string expected)
	{
		// Arrange
		var runs = new List<ParagraphRun>
		{
			new ParagraphRun { content = input, isItalic = false }
		};

		// Act
		var result = PaigeExporter.UnRun(runs);

		// Assert
		Assert.Equal(expected, result);
	}

	[Fact]
	public void UnRun_ShouldHandleItalics()
	{
		// Arrange
		var runs = new List<ParagraphRun>
		{
			new ParagraphRun { content = "Hello", isItalic = true }
		};

		// Act
		var result = PaigeExporter.UnRun(runs);

		// Assert
		Assert.Equal("<em>Hello</em>", result);
	}

	[Fact]
	public void UnRun_ShouldHandleEmDash()
	{
		// Arrange
		var runs = new List<ParagraphRun>
		{
			new ParagraphRun { content = "\u2014", isItalic = false }
		};

		// Act
		var result = PaigeExporter.UnRun(runs);

		// Assert
		Assert.Equal("&#x2014;", result);
	}

	[Fact]
	public void Export_ShouldStructureSingleChapterCorrectly()
	{
		// Arrange
		using var ms = new MemoryStream();
		var exporter = new PaigeExporter(ms);

		// Act
		exporter.Paragraph("Titre1", [new ParagraphRun { content = "Chapitre 1", isItalic = false }]);
		exporter.Paragraph("Normal", [new ParagraphRun { content = "Contenu 1", isItalic = false }]);
		exporter.FinishExport();

		// Assert
		var output = System.Text.Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", "\n");
		Assert.Contains("#manifest.add(", output);
		Assert.Contains("id: \"chapitre_1\"", output);
		Assert.Contains("<body class=\"chapter\">", output);
		Assert.Contains("<h1>Chapitre 1</h1>", output);
		Assert.Contains("<p>Contenu 1</p>", output);
		// Validate closure with at least some newlines (matching the original output)
		Assert.Contains("</body>\n]\n\n", output);
	}

	[Fact]
	public void Export_ShouldStructureMultipleChaptersCorrectly()
	{
		// Arrange
		using var ms = new MemoryStream();
		var exporter = new PaigeExporter(ms);

		// Act
		exporter.Paragraph("Titre1", [new ParagraphRun { content = "Chapitre 1", isItalic = false }]);
		exporter.Paragraph("Normal", [new ParagraphRun { content = "Contenu 1", isItalic = false }]);
		exporter.Paragraph("Titre1", [new ParagraphRun { content = "Chapitre 2", isItalic = false }]);
		exporter.Paragraph("Normal", [new ParagraphRun { content = "Contenu 2", isItalic = false }]);
		exporter.FinishExport();

		// Assert
		var output = System.Text.Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", "\n");
		
		// Check first chapter
		Assert.Contains("id: \"chapitre_1\"", output);
		Assert.Contains("<h1>Chapitre 1</h1>", output);
		Assert.Contains("<p>Contenu 1</p>", output);

		// Check transition
		Assert.Contains("</body>\n]\n\n", output);
		Assert.Contains("#manifest.add(", output);

		// Check second chapter
		Assert.Contains("id: \"chapitre_2\"", output);
		Assert.Contains("<h1>Chapitre 2</h1>", output);
		Assert.Contains("<p>Contenu 2</p>", output);
	}

	[Fact]
	public void Export_ShouldStructureThreeChaptersCorrectly()
	{
		// Arrange
		using var ms = new MemoryStream();
		var exporter = new PaigeExporter(ms);

		// Act
		exporter.Paragraph("Titre1", [new ParagraphRun { content = "C1", isItalic = false }]);
		exporter.Paragraph("Titre1", [new ParagraphRun { content = "C2", isItalic = false }]);
		exporter.Paragraph("Titre1", [new ParagraphRun { content = "C3", isItalic = false }]);
		exporter.FinishExport();

		// Assert
		var output = System.Text.Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", "\n");
		
		Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(output, "#manifest\\.add\\(").Count);
		Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(output, "</body>\n]").Count);
		
		Assert.Contains("id: \"c1\"", output);
		Assert.Contains("id: \"c2\"", output);
		Assert.Contains("id: \"c3\"", output);
	}
}
