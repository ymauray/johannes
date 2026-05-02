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
}
