using System.Xml.Linq;
using Xunit;

namespace Johannes.Tests;

public class WordXmlTests
{
	private static readonly XNamespace W = WordXml.W;

	[Fact]
	public void IsOn_WithoutValue_IsTrue() =>
		Assert.True(WordXml.IsOn(new XElement(W + "b")));

	[Theory]
	[InlineData("1", true)]
	[InlineData("true", true)]
	[InlineData("on", true)]
	[InlineData("0", false)]
	[InlineData("false", false)]
	[InlineData("off", false)]
	public void IsOn_ReadsValueAttribute(string value, bool expected) =>
		Assert.Equal(expected, WordXml.IsOn(new XElement(W + "b", new XAttribute(W + "val", value))));

	[Fact]
	public void Name_ReturnsLocalNameForWordElements() =>
		Assert.Equal("r", WordXml.Name(new XElement(W + "r")));

	[Fact]
	public void Name_ReturnsFullNameForForeignElements() =>
		Assert.Equal("{urn:autre}r", WordXml.Name(new XElement(XNamespace.Get("urn:autre") + "r")));

	[Fact]
	public void PlainText_ConcatenatesTextNodes()
	{
		var paragraph = new XElement(W + "p",
			new XElement(W + "r", new XElement(W + "t", "Bon")),
			new XElement(W + "r", new XElement(W + "t", "jour")));

		Assert.Equal("Bonjour", WordXml.PlainText(paragraph));
	}
}
