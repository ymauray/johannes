using Johannes;
using Xunit;

namespace Johannes.Tests;

public class TypstExporterTests
{
	[Theory]
	[InlineData("Test ?", "Test~?")]
	[InlineData("Attention !", "Attention~!")]
	[InlineData("Exemple :", "Exemple~:")]
	[InlineData("Fin ;", "Fin~;")]
	public void UnRun_ShouldAddNonBreakingSpacesBeforePunctuation(string input, string expected)
	{
		// Arrange
		var runs = new List<ParagraphRun>
		{
			new ParagraphRun { content = input, isItalic = false }
		};

		// Act
		var result = TypstExporter.UnRun(runs);

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
		var result = TypstExporter.UnRun(runs);

		// Assert
		Assert.Equal("_Hello_", result);
	}

	[Fact]
	public void Replace_ShouldHandleEmDash()
	{
		// Arrange
		char emDash = '\u2014';
		byte[] bytes = [0xE2, 0x80, 0x94];

		// Act
		var result = TypstExporter.Replace(emDash, bytes);

		// Assert
		Assert.Equal("---", result);
	}

	[Fact]
	public void Constructor_ShouldCreateSupportFunctionsFileWithEllipsisWhenMissing()
	{
		// Arrange
		var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(directory);
		var baseFile = Path.Combine(directory, "document");
		var supportFunctionsFile = Path.Combine(directory, "support-functions.typ");

		try
		{
			// Act
			var exporter = new TypstExporter(baseFile);
			exporter.FinishExport();

			// Assert
			Assert.True(File.Exists(supportFunctionsFile));
			Assert.Equal(
				"""
				/*
				 * Fonction insérée automatiquement.
				 * Les modifications seront conservées.
				 */
				#let ellipsis() = {
				  align(center, text("***"))
				}

				""".ReplaceLineEndings(Environment.NewLine),
				File.ReadAllText(supportFunctionsFile));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void Constructor_ShouldNotReplaceExistingEllipsisFunction()
	{
		// Arrange
		var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(directory);
		var baseFile = Path.Combine(directory, "document");
		var supportFunctionsFile = Path.Combine(directory, "support-functions.typ");
		const string customDefinition = "#let ellipsis() = text(\"custom\")\n";
		File.WriteAllText(supportFunctionsFile, customDefinition);

		try
		{
			// Act
			var exporter = new TypstExporter(baseFile);
			exporter.FinishExport();

			// Assert
			Assert.Equal(customDefinition, File.ReadAllText(supportFunctionsFile));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void Paragraph_WithUnsupportedStyle_ShouldExportAndCreateDefaultStyleFunction()
	{
		// Arrange
		var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(directory);
		var baseFile = Path.Combine(directory, "document");
		var typstFile = $"{baseFile}.typ";
		var supportFunctionsFile = Path.Combine(directory, "support-functions.typ");

		try
		{
			var exporter = new TypstExporter(baseFile);

			// Act
			exporter.Paragraph("Messagedroit", [new ParagraphRun { content = "Contenu", isItalic = false }]);
			exporter.FinishExport();

			// Assert
			Assert.Contains(
				"""
				#style_Messagedroit([
				Contenu
				])
				""".ReplaceLineEndings("\n"),
				File.ReadAllText(typstFile).ReplaceLineEndings("\n"));
			Assert.Contains(
				"""
				#let style_Messagedroit(body) = {
				  [#body]
				}
				""".ReplaceLineEndings(Environment.NewLine),
				File.ReadAllText(supportFunctionsFile));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Theory]
	[InlineData("// #let ellipsis() = none")]
	[InlineData("\"#let ellipsis() = none\"")]
	public void Constructor_ShouldAddEllipsisWhenOnlyMentionedInCommentOrString(string content)
	{
		// Arrange
		var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(directory);
		var baseFile = Path.Combine(directory, "document");
		var supportFunctionsFile = Path.Combine(directory, "support-functions.typ");
		File.WriteAllText(supportFunctionsFile, content);

		try
		{
			// Act
			var exporter = new TypstExporter(baseFile);
			exporter.FinishExport();

			// Assert
			Assert.Contains("#let ellipsis() = {", File.ReadAllText(supportFunctionsFile));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}
