using Johannes.Paige;

namespace Johannes.Tests;

public class ParserTests
{
	// --- Métadonnées ---

	[Fact]
	public void Metadata_Identifier_IsStoredAsString()
	{
		var doc = Parser.Parse("#metadata(identifier: 12345, title: \"T\", language: fr)");
		Assert.Equal("12345", doc.Metadata.Identifier);
	}

	[Fact]
	public void Metadata_Title_PreservesSpecialChars()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "Les Ailéris : Anya", language: fr)
            """);
		Assert.Equal("Les Ailéris : Anya", doc.Metadata.Title);
	}

	[Fact]
	public void Metadata_Language_Fr_IsValid()
	{
		var doc = Parser.Parse("#metadata(identifier: 1, title: \"T\", language: fr)");
		Assert.Equal("fr", doc.Metadata.Language);
	}

	[Fact]
	public void Metadata_Language_En_IsValid()
	{
		var doc = Parser.Parse("#metadata(identifier: 1, title: \"T\", language: en)");
		Assert.Equal("en", doc.Metadata.Language);
	}

	[Fact]
	public void Metadata_Language_Unknown_Throws()
	{
		Assert.Throws<InvalidOperationException>(() =>
			Parser.Parse("#metadata(identifier: 1, title: \"T\", language: de)"));
	}

	// --- ManifestItem — champs obligatoires ---

	[Fact]
	public void ManifestAdd_RequiredFields_AreParsed()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "chap1", href: "chap1.xhtml", mediaType: "application/xhtml+xml")
            """);
		var item = doc.Manifest[0];
		Assert.Equal("chap1", item.Id);
		Assert.Equal("chap1.xhtml", item.Href);
		Assert.Equal("application/xhtml+xml", item.MediaType);
	}

	// --- ManifestItem — champs optionnels ---

	[Fact]
	public void ManifestAdd_Properties_IsNull_WhenAbsent()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml")
            """);
		Assert.Null(doc.Manifest[0].Properties);
	}

	[Fact]
	public void ManifestAdd_Source_IsNull_WhenAbsent()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml")
            """);
		Assert.Null(doc.Manifest[0].Source);
	}

	[Fact]
	public void ManifestAdd_Nav_IsParsed()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml", nav: "Label")
            """);
		Assert.Equal("Label", doc.Manifest[0].Nav);
	}

	[Fact]
	public void ManifestAdd_InSpine_DefaultsFalse()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml")
            """);
		Assert.False(doc.Manifest[0].InSpine);
	}

	[Fact]
	public void ManifestAdd_InSpine_TrueWhenSpecified()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml", spine: true)
            """);
		Assert.True(doc.Manifest[0].InSpine);
	}

	[Fact]
	public void ManifestAdd_TrailingComma_IsHandled()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml", spine: true,)
            """);
		Assert.True(doc.Manifest[0].InSpine);
	}

	// --- Invariants Source / InlineContent ---

	[Fact]
	public void ManifestAdd_WithSource_InlineContentIsNull()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "img", href: "cover.jpg", mediaType: "image/jpeg", source: "epub/cover.jpg")
            """);
		Assert.Null(doc.Manifest[0].InlineContent);
		Assert.NotNull(doc.Manifest[0].Source);
	}

	[Fact]
	public void ManifestAdd_WithInlineBlock_SourceIsNull()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml")[<p>Hello</p>]
            """);
		Assert.Null(doc.Manifest[0].Source);
		Assert.NotNull(doc.Manifest[0].InlineContent);
	}

	// --- Contenu inline ---

	[Fact]
	public void ManifestAdd_InlineContent_IsPreserved()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml")[
            <body><p>Hello</p></body>
            ]
            """);
		Assert.Contains("<body>", doc.Manifest[0].InlineContent);
	}

	[Fact]
	public void ManifestAdd_NoBlock_InlineContentIsNull()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "x", href: "x.xhtml", mediaType: "application/xhtml+xml")
            """);
		Assert.Null(doc.Manifest[0].InlineContent);
	}

	// --- EpubDocument global ---

	[Fact]
	public void Parse_ManifestCount_IsCorrect()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "a", href: "a.xhtml", mediaType: "application/xhtml+xml")
            #manifest.add(id: "b", href: "b.xhtml", mediaType: "application/xhtml+xml")
            """);
		Assert.Equal(2, doc.Manifest.Count);
	}

	[Fact]
	public void Parse_ManifestOrder_IsPreserved()
	{
		var doc = Parser.Parse("""
            #metadata(identifier: 1, title: "T", language: fr)
            #manifest.add(id: "first", href: "a.xhtml", mediaType: "application/xhtml+xml")
            #manifest.add(id: "second", href: "b.xhtml", mediaType: "application/xhtml+xml")
            """);
		Assert.Equal("first", doc.Manifest[0].Id);
		Assert.Equal("second", doc.Manifest[1].Id);
	}

	// --- Tests fixture ---

	[Fact]
	public void SampleFixture_ProducesValidEpubDocument()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		Assert.NotNull(doc);
	}

	[Fact]
	public void SampleFixture_Metadata_IsCorrect()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		Assert.Equal("12345", doc.Metadata.Identifier);
		Assert.Equal("Les Ailéris : Anya", doc.Metadata.Title);
		Assert.Equal("fr", doc.Metadata.Language);
	}

	[Fact]
	public void SampleFixture_Manifest_HasFourItems()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		Assert.Equal(4, doc.Manifest.Count);
	}

	[Fact]
	public void SampleFixture_FirstItem_HasSource()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		var item = doc.Manifest[0];
		Assert.NotNull(item.Source);
		Assert.Null(item.InlineContent);
	}

	[Fact]
	public void SampleFixture_IncludedItem_IsPresent()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		// Item 0: cover-img, Item 1: le-prologue, Item 2: chap1, Item 3: part1-item (from include)
		Assert.Equal(4, doc.Manifest.Count);
		Assert.Equal("part1-item", doc.Manifest[3].Id);
	}

	[Fact]
	public void Parse_Include_ThrowsFileNotFound_WhenMissing()
	{
		var source = "#metadata(identifier: 1, title: \"T\", language: fr)\n#include \"missing.paige\"";
		Assert.Throws<FileNotFoundException>(() => Parser.Parse(source));
	}

	[Fact]
	public void SampleFixture_SecondItem_HasInlineContent()
	{
		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		var item = doc.Manifest[1];
		Assert.Null(item.Source);
		Assert.Contains("<body>", item.InlineContent);
	}

	private static string FixturePath(string name) =>
		Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
