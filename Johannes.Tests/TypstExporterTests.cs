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
}
