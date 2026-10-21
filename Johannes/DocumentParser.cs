using System.Xml.Linq;

namespace Johannes
{
	public record ParagraphRun
	{
		public required string content;
		public bool isItalic { get; init; } = false;
		public bool isBold { get; init; } = false;
		public string? font { get; init; } = null;
	}

	public class DocumentParser(string docxFilePath)
	{
		private static readonly XNamespace W = WordXml.W;

		private IExporter? _exporter;

		internal DocumentParser(IExporter exporter) : this("")
		{
			_exporter = exporter;
		}

		public void ParseAndExport(IExporter exporter)
		{
			_exporter = exporter;
			var wDoc = WordXml.LoadMainDocument(docxFilePath);
			var root = wDoc.Root ?? throw new InvalidOperationException("Document has no document element.");
			var body = root.Element(W + "body") ?? throw new InvalidOperationException("Document has no body.");

			foreach (var child in body.Elements())
			{
				switch (WordXml.Name(child))
				{
					case "p":
						ParseParagraph(child);
						break;
					case "sectPr":
						// Silently ignore section properties here.
						break;
					default:
						throw new NotSupportedException($"Unsupported block-level element: {WordXml.Name(child)}");
				}
			}

			_exporter.FinishExport();
		}

		internal void ParseParagraph(XElement paragraph)
		{
			var props = paragraph.Element(W + "pPr");

			var styleId = (string?)props?.Element(W + "pStyle")?.Attribute(W + "val") ?? "Normal";

			List<ParagraphRun> contentBuilder = [];

			foreach (var child in paragraph.Elements())
			{
				try
				{
					switch (WordXml.Name(child))
					{
						case "r":
							var content = ParseRun(child);
							contentBuilder.Add(content);
							break;
						case "pPr":
							// Silently ignore paragraph properties here.
							break;
						case "bookmarkStart":
							// Silently ignore bookmarks here.
							break;
						case "bookmarkEnd":
							// Silently ignore bookmarks here.
							break;
						case "proofErr":
							// Silently ignore proof errors here.
							break;
						default:
							throw new NotSupportedException($"Unsupported inline element: {WordXml.Name(child)}");
					}
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(
						$"Erreur lors de l'analyse du paragraphe (style: '{styleId}', texte brut: \"{WordXml.PlainText(paragraph)}\") : {ex.Message}",
						ex);
				}
			}

			this._exporter!.Paragraph(styleId, contentBuilder);
		}

		private ParagraphRun ParseRun(XElement run)
		{
			var content = "";
			var isItalic = false;
			var isBold = false;
			string? font = null;

			foreach (var child in run.Elements())
			{
				switch (WordXml.Name(child))
				{
					case "t":
						content += child.Value;
						break;
					case "rPr":
						foreach (var prop in child.Elements())
						{
							switch (WordXml.Name(prop))
							{
								case "i":
									isItalic |= WordXml.IsOn(prop);
									break;
								case "b":
									isBold |= WordXml.IsOn(prop);
									break;
								case "rFonts":
									var fontName = (string?)prop.Attribute(W + "ascii")
										?? (string?)prop.Attribute(W + "hAnsi")
										?? (string?)prop.Attribute(W + "cs")
										?? (string?)prop.Attribute(W + "eastAsia");
									if (!string.IsNullOrWhiteSpace(fontName))
									{
										font = fontName;
									}
									break;
								case "iCs":
								case "bCs":
								case "lang":
									// Silently ignore those properties here.
									break;
								default:
									throw new NotSupportedException($"Unsupported run property: {WordXml.Name(prop)}");
							}
						}
						break;
					case "lastRenderedPageBreak":
						// Silently ignore page breaks here.
						break;
					default:
						throw new NotSupportedException($"Unsupported run-level element: {WordXml.Name(child)}");
				}
			}

			return new ParagraphRun()
			{
				content = content,
				isItalic = isItalic,
				isBold = isBold,
				font = font
			};
		}
	}
}
