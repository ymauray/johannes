using System.IO.Compression;

namespace Johannes.Paige;

public static class Epub
{
	public static void Write(EpubDocument doc, string basePath, string outputPath)
	{
		using var stream = new FileStream(outputPath, FileMode.Create);
		using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

		var coverItem = doc.Manifest.FirstOrDefault(i => i.Properties == "cover-image");
		var spineItems = doc.Manifest.Where(i => i.InSpine).ToList();

		WriteMimetype(zip);
		WriteContainerXml(zip);
		WriteContentOpf(zip, doc, coverItem, spineItems);
		if (coverItem != null) WriteCoverXhtml(zip, coverItem);
		foreach (var item in doc.Manifest) WriteItem(zip, item, basePath);
		WriteNavXhtml(zip, spineItems);
	}

	private static void WriteMimetype(ZipArchive zip)
	{
		var entry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
		using var w = new StreamWriter(entry.Open());
		w.Write("application/epub+zip");
	}

	private static void WriteContainerXml(ZipArchive zip)
	{
		var entry = zip.CreateEntry("META-INF/container.xml");
		using var w = new StreamWriter(entry.Open());
		w.Write("""
            <?xml version="1.0" encoding="UTF-8"?>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
                </rootfiles>
            </container>
            """);
	}

	private static void WriteContentOpf(ZipArchive zip, EpubDocument doc, ManifestItem? coverItem, IList<ManifestItem> spineItems)
	{
		var entry = zip.CreateEntry("OEBPS/content.opf");
		using var w = new StreamWriter(entry.Open());
		var modified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

		w.WriteLine($"""
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" unique-identifier="pub-id" version="3.0">
                <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:identifier id="pub-id">{doc.Metadata.Identifier}</dc:identifier>
                    <dc:title>{doc.Metadata.Title}</dc:title>
                    <dc:language>{doc.Metadata.Language}</dc:language>
                    <meta property="dcterms:modified">{modified}</meta>
                </metadata>
                <manifest>
            """);

		if (coverItem != null)
			w.WriteLine("""        <item id="cover-page" href="cover.xhtml" media-type="application/xhtml+xml"/>""");

		w.WriteLine("""        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>""");

		foreach (var item in doc.Manifest)
		{
			var props = item.Properties != null ? $""" properties="{item.Properties}" """ : "";
			w.WriteLine($"""        <item id="{item.Id}" href="{item.Href}" media-type="{item.MediaType}"{props}/>""");
		}

		w.WriteLine("    </manifest>");
		w.WriteLine("    <spine>");

		if (coverItem != null)
			w.WriteLine("""        <itemref idref="cover-page"/>""");

		foreach (var item in spineItems)
			w.WriteLine($"""        <itemref idref="{item.Id}"/>""");

		w.WriteLine("    </spine>");
		w.Write("</package>");
	}

	private static void WriteCoverXhtml(ZipArchive zip, ManifestItem coverItem)
	{
		var entry = zip.CreateEntry("OEBPS/cover.xhtml");
		using var w = new StreamWriter(entry.Open());
		w.Write($$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE html>
            <html xmlns="http://www.w3.org/1999/xhtml">
            <head>
                <title>Couverture</title>
                <style>
                    body { margin: 0; padding: 0; text-align: center; background-color: #000000; }
                    img { max-width: 100%; height: auto; }
                </style>
            </head>
            <body>
                <img src="{{coverItem.Href}}" alt="Couverture" />
            </body>
            </html>
            """);
	}

	private static void WriteNavXhtml(ZipArchive zip, IList<ManifestItem> spineItems)
	{
		var entry = zip.CreateEntry("OEBPS/nav.xhtml");
		using var w = new StreamWriter(entry.Open());
		w.WriteLine("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE html>
            <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
            <head><title>Table des matières</title></head>
            <body>
                <nav epub:type="toc" id="toc">
                    <h1>Sommaire</h1>
                    <ol>
            """);

		foreach (var item in spineItems)
			w.WriteLine($"""            <li><a href="{item.Href}">{item.Nav ?? item.Id}</a></li>""");

		w.Write("""
                    </ol>
                </nav>
            </body>
            </html>
            """);
	}

	private static void WriteItem(ZipArchive zip, ManifestItem item, string basePath)
	{
		if (item.InlineContent != null)
		{
			var entry = zip.CreateEntry($"OEBPS/{item.Href}");
			using var w = new StreamWriter(entry.Open());
			w.Write($"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE html>
                <html xmlns="http://www.w3.org/1999/xhtml">
                {item.InlineContent}
                </html>
                """);
		}
		else if (item.Source != null)
		{
			CopyFile(zip, $"OEBPS/{item.Href}", Path.Combine(basePath, item.Source));
		}
		else
		{
			throw new InvalidOperationException($"L'item '{item.Id}' n'a ni Source ni InlineContent.");
		}
	}

	private static void CopyFile(ZipArchive zip, string entryName, string sourcePath)
	{
		if (!File.Exists(sourcePath))
		{
			throw new FileNotFoundException($"Le fichier source est introuvable : {sourcePath}", sourcePath);
		}

		var entry = zip.CreateEntry(entryName);
		using var entryStream = entry.Open();
		using var sourceStream = File.OpenRead(sourcePath);
		sourceStream.CopyTo(entryStream);
	}
}
