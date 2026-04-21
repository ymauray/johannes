using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.ExtendedProperties;
using System.Text.RegularExpressions;

namespace Johannes
{
	public partial class TypstExporter : IExporter
	{
		private readonly FileStream _handle;

		public TypstExporter(string baseFile)
		{
			var filename = $"{baseFile}.typ";
			// delete file if it already exists to avoid appending to an old file
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
			_handle = File.OpenWrite(filename);
			Write("#import \"/support-functions.typ\" : *\n\n");
		}

		private void Write(string data)
		{
			_handle.Write(System.Text.Encoding.UTF8.GetBytes(data));
		}

		public void Paragraph(string styleId, List<ParagraphRun> runs)
		{
			var content = UnRun(runs);

			switch (styleId) {
				case "Titre1":
					Write($"= {content}\n\n");
					break;
				case "Normal":
					Write($"{content}\n\n");
					break;
				case "Ellipse":
				case "Elipse":
					Write("#ellipsis()");
					break;
				case "Titre":
					Write($"#titre()[\n{content}\n]\n\n");
					break;
				default:
					throw new NotSupportedException($"Unsupported style ID: {styleId}");
			}
		}

		internal static string UnRun(List<ParagraphRun> runs)
		{
			var sb = new System.Text.StringBuilder();
			foreach (var run in runs)
			{
				var content = "";

				foreach (var c in run.content)
				{
					var bytes = System.Text.Encoding.UTF8.GetBytes([c]);
					content += Replace(c, bytes);
				}
				
				if (run.isItalic)
				{
					content = $"_{content}_";
				}
				sb.Append(content);
			}
			
			var data = sb.ToString();
			data = QuestionMarkRegex().Replace(data, "~?");
			data = ExclamationMarkRegex().Replace(data, "~!");
			data = ColonRegex().Replace(data, "~:");
			data = SemiColonRegex().Replace(data, "~;");

			// Replace " ?" with "~?" to prevent line breaks between a word and its following question mark.

			return data;
		}

		public static string Replace(char c, byte[] bytes)
		{
			return bytes switch
			{
				// em dash
				[0xE2, 0x80, 0x94] => "---",
				// non-breaking space
				[0xC2, 0xA0] => "~",
				_ => $"{c}",
			};
		}

		public void FinishExport()
		{
			_handle.Flush();
			_handle.Close();
		}

		[GeneratedRegex(@"[ \u00A0~]\?")]
		private static partial Regex QuestionMarkRegex();
		[GeneratedRegex(@"[ \u00A0~]\!")]
		private static partial Regex ExclamationMarkRegex();
		[GeneratedRegex(@"[ \u00A0~]:")]
		private static partial Regex ColonRegex();
		[GeneratedRegex(@"[ \u00A0~];")]
		private static partial Regex SemiColonRegex();
	}
}
