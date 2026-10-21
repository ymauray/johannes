using System.IO.Compression;

using Johannes.Paige;

namespace Johannes.Tests;

public class EpubWriterTests : IDisposable
{
	private readonly List<string> _tempDirs = new();

	public void Dispose()
	{
		foreach (var d in _tempDirs)
			if (Directory.Exists(d)) Directory.Delete(d, true);
	}

	private string TempDir()
	{
		var path = Path.Combine(Path.GetTempPath(), $"paige-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		_tempDirs.Add(path);
		return path;
	}

	private static string ReadEntry(string zipPath, string entryName)
	{
		using var zip = ZipFile.OpenRead(zipPath);
		var entry = zip.GetEntry(entryName)
			?? throw new FileNotFoundException($"Entrée '{entryName}' absente de l'archive.");
		using var reader = new StreamReader(entry.Open());
		return reader.ReadToEnd();
	}

	private static bool EntryExists(string zipPath, string entryName)
	{
		using var zip = ZipFile.OpenRead(zipPath);
		return zip.GetEntry(entryName) != null;
	}

	private static EpubDocument MinimalDoc() => new(
		new EpubMetadata("1", "Test", "fr"),
		[]
	);

	// --- Fichiers auto-générés ---

	[Fact]
	public void Write_Mimetype_IsFirstEntry()
	{
		var dir = TempDir();
		Epub.Write(MinimalDoc(), dir, Path.Combine(dir, "out.epub"));
		using var zip = ZipFile.OpenRead(Path.Combine(dir, "out.epub"));
		Assert.Equal("mimetype", zip.Entries[0].FullName);
	}

	[Fact]
	public void Write_Mimetype_IsUncompressed()
	{
		var dir = TempDir();
		Epub.Write(MinimalDoc(), dir, Path.Combine(dir, "out.epub"));
		using var zip = ZipFile.OpenRead(Path.Combine(dir, "out.epub"));
		var entry = zip.Entries[0];
		Assert.Equal(entry.Length, entry.CompressedLength);
	}

	[Fact]
	public void Write_Mimetype_HasCorrectContent()
	{
		var dir = TempDir();
		Epub.Write(MinimalDoc(), dir, Path.Combine(dir, "out.epub"));
		Assert.Equal("application/epub+zip", ReadEntry(Path.Combine(dir, "out.epub"), "mimetype"));
	}

	[Fact]
	public void Write_ContainerXml_IsPresent()
	{
		var dir = TempDir();
		Epub.Write(MinimalDoc(), dir, Path.Combine(dir, "out.epub"));
		Assert.True(EntryExists(Path.Combine(dir, "out.epub"), "META-INF/container.xml"));
	}

	[Fact]
	public void Write_ContainerXml_PointsToContentOpf()
	{
		var dir = TempDir();
		Epub.Write(MinimalDoc(), dir, Path.Combine(dir, "out.epub"));
		var xml = ReadEntry(Path.Combine(dir, "out.epub"), "META-INF/container.xml");
		Assert.Contains("OEBPS/content.opf", xml);
	}

	// --- OPF ---

	[Fact]
	public void Write_ContentOpf_ContainsCorrectMetadata()
	{
		var dir = TempDir();
		var doc = new EpubDocument(new EpubMetadata("9999", "Mon Titre", "en"), []);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var opf = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/content.opf");
		Assert.Contains("9999", opf);
		Assert.Contains("Mon Titre", opf);
		Assert.Contains("en", opf);
	}

	[Fact]
	public void Write_ContentOpf_ManifestContainsAllItems()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[
				new ManifestItem("item-a", "a.xhtml", "application/xhtml+xml", null, null, "<body/>", null, false),
				new ManifestItem("item-b", "b.xhtml", "application/xhtml+xml", null, null, "<body/>", null, false),
			]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var opf = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/content.opf");
		Assert.Contains("item-a", opf);
		Assert.Contains("item-b", opf);
	}

	[Fact]
	public void Write_ContentOpf_SpineContainsOnlyInSpineItems()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[
				new ManifestItem("in-spine",  "a.xhtml", "application/xhtml+xml", null, null, "<body/>", null, true),
				new ManifestItem("not-spine", "b.xhtml", "application/xhtml+xml", null, null, "<body/>", null, false),
			]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var opf = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/content.opf");
		var spineStart = opf.IndexOf("<spine>");
		var spineEnd = opf.IndexOf("</spine>");
		var spine = opf[spineStart..spineEnd];
		Assert.Contains("in-spine", spine);
		Assert.DoesNotContain("not-spine", spine);
	}

	// --- Items ---

	[Fact]
	public void Write_InlineContent_IsWrappedInXhtmlDocument()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("chap1", "chap1.xhtml", "application/xhtml+xml", null, null, "<body><p>Hello</p></body>", null, false)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var content = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/chap1.xhtml");
		Assert.Contains("<?xml", content);
		Assert.Contains("<!DOCTYPE html>", content);
	}

	[Fact]
	public void Write_InlineContent_BodyIsPreserved()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("chap1", "chap1.xhtml", "application/xhtml+xml", null, null, "<body><p>Bonjour</p></body>", null, false)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var content = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/chap1.xhtml");
		Assert.Contains("<p>Bonjour</p>", content);
	}

	[Fact]
	public void Write_SourceItem_IsCopiedToOebps()
	{
		var dir = TempDir();
		File.WriteAllBytes(Path.Combine(dir, "image.jpg"), [0xFF, 0xD8, 0xFF]);
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("img", "image.jpg", "image/jpeg", null, "image.jpg", null, null, false)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		Assert.True(EntryExists(Path.Combine(dir, "out.epub"), "OEBPS/image.jpg"));
	}

	[Fact]
	public void Write_SourceItem_ThrowsFileNotFound_WhenMissing()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("img", "missing.jpg", "image/jpeg", null, "missing.jpg", null, null, false)]
		);
		var ex = Assert.Throws<FileNotFoundException>(() => Epub.Write(doc, dir, Path.Combine(dir, "out.epub")));
		Assert.Contains("missing.jpg", ex.Message);
	}

	[Fact]
	public void Write_BothNullSourceAndInlineContent_Throws()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("x", "x.xhtml", "application/xhtml+xml", null, null, null, null, false)]
		);
		Assert.Throws<InvalidOperationException>(() => Epub.Write(doc, dir, Path.Combine(dir, "out.epub")));
	}

	// --- Cover + Nav auto-générés ---

	[Fact]
	public void Write_CoverXhtml_IsGenerated_WhenCoverImagePresent()
	{
		var dir = TempDir();
		File.WriteAllBytes(Path.Combine(dir, "cover.jpg"), [0xFF, 0xD8, 0xFF]);
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("cover-img", "cover.jpg", "image/jpeg", "cover-image", "cover.jpg", null, null, false)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		Assert.True(EntryExists(Path.Combine(dir, "out.epub"), "OEBPS/cover.xhtml"));
	}

	[Fact]
	public void Write_NavXhtml_UsesNavProperty_WhenAvailable()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("chap1", "chapitre1.xhtml", "application/xhtml+xml", null, null, "<body/>", "Titre Prologue", true)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var nav = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/nav.xhtml");
		Assert.Contains(">Titre Prologue</a>", nav);
	}

	[Fact]
	public void Write_NavXhtml_UsesId_WhenNavIsNull()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("id-seulement", "chapitre1.xhtml", "application/xhtml+xml", null, null, "<body/>", null, true)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var nav = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/nav.xhtml");
		Assert.Contains(">id-seulement</a>", nav);
	}

	[Fact]
	public void Write_NavXhtml_ContainsSpineItemHrefs()
	{
		var dir = TempDir();
		var doc = new EpubDocument(
			new EpubMetadata("1", "T", "fr"),
			[new ManifestItem("chap1", "chapitre1.xhtml", "application/xhtml+xml", null, null, "<body/>", null, true)]
		);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));
		var nav = ReadEntry(Path.Combine(dir, "out.epub"), "OEBPS/nav.xhtml");
		Assert.Contains("chapitre1.xhtml", nav);
	}

	// --- Intégration fixture ---

	[Fact]
	public void SampleFixture_ProducesEpub_WithExpectedEntries()
	{
		var dir = TempDir();
		Directory.CreateDirectory(Path.Combine(dir, "epub"));
		File.WriteAllBytes(Path.Combine(dir, "epub", "cover.jpg"), [0xFF, 0xD8, 0xFF]);

		var source = File.ReadAllText(FixturePath("sample.paige"));
		var doc = Parser.Parse(source, Path.GetDirectoryName(FixturePath("sample.paige"))!);
		Epub.Write(doc, dir, Path.Combine(dir, "out.epub"));

		var outPath = Path.Combine(dir, "out.epub");
		Assert.True(EntryExists(outPath, "mimetype"));
		Assert.True(EntryExists(outPath, "META-INF/container.xml"));
		Assert.True(EntryExists(outPath, "OEBPS/content.opf"));
		Assert.True(EntryExists(outPath, "OEBPS/cover.xhtml"));
		Assert.True(EntryExists(outPath, "OEBPS/nav.xhtml"));
		Assert.True(EntryExists(outPath, "OEBPS/cover.jpg"));
		Assert.True(EntryExists(outPath, "OEBPS/prologue.xhtml"));
		Assert.True(EntryExists(outPath, "OEBPS/chapitre1.xhtml"));
		Assert.True(EntryExists(outPath, "OEBPS/part1.xhtml"));
	}

	private static string FixturePath(string name) =>
		Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
