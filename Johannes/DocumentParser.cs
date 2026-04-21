using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Johannes
{
	public record ParagraphRun {
		public required string content;
		public required bool isItalic;
	}

	public class DocumentParser(string docxFilePath)
	{
		private IExporter? _exporter;

		public void ParseAndExport(IExporter exporter)
		{
			_exporter = exporter;
			using var doc = WordprocessingDocument.Open(docxFilePath, false);
			var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("Document has no main part.");
			var wDoc = mainPart.Document ?? throw new InvalidOperationException("Document has no document element.");
			var body = wDoc.Body ?? throw new InvalidOperationException("Document has no body.");

			foreach (var child in body.ChildElements)
			{
				switch (child)
				{
					case W.Paragraph p:
						ParseParagraph(p);
						break;
					case W.SectionProperties:
						// Silently ignore section properties here.
						break;
					default:
						throw new NotSupportedException($"Unsupported block-level element: {child.GetType().Name}");
				}
			}

			_exporter.FinishExport();
		}

		private void ParseParagraph(W.Paragraph paragraph)
		{
			var props = paragraph.ParagraphProperties;

			var styleId = props?.ParagraphStyleId?.Val?.ToString() ?? "Normal";

			List<ParagraphRun> contentBuilder = [];

			foreach (var child in paragraph.ChildElements)
			{
				switch (child)
				{
					case W.Run r:
						var content = ParseRun(r);
						contentBuilder.Add(content);
						break;
					case W.ParagraphProperties:
						// Silently ignore paragraph properties here.
						break;
					case W.BookmarkStart:
						// Silently ignore bookmarks here.
						break;
					case W.BookmarkEnd:
						// Silently ignore bookmarks here.
						break;
					case ProofError:
						// Silently ignore proof errors here.
						break;
					default:
						throw new NotSupportedException($"Unsupported inline element: {child.GetType().Name}");
				}
			}

			this._exporter!.Paragraph(styleId, contentBuilder);
		}

		private ParagraphRun ParseRun(W.Run run)
		{
			var content = "";
			var isItalic = false;

			foreach (var child in run.ChildElements)
			{
				switch (child)
				{
					case W.Text text:
						var t = text.InnerText ?? "";
						content += t;
						break;
					case W.RunProperties:
						foreach (var prop in child.ChildElements)
						{
							switch (prop)
							{
								case W.Italic:
									isItalic = true;
									break;
								case W.ItalicComplexScript:
								case W.Languages:
									// Silently ignore those properties here.
									break;
								default:
									throw new NotSupportedException($"Unsupported run property: {prop.GetType().Name}");
							}
						}
						// Silently ignore run properties here.
						break;
					case W.LastRenderedPageBreak:
						// Silently ignore page breaks here.
						break;
					default:
						throw new NotSupportedException($"Unsupported run-level element: {child.GetType().Name}");
				}
			}

			return new ParagraphRun() {
				content = content,
				isItalic= isItalic
			};
		}
	}
}
