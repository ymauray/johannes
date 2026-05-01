using System.Text.RegularExpressions;

namespace Johannes
{
	public class TypstExporter : AbstractExporter
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

		protected void Write(string data) => _handle.Write(System.Text.Encoding.UTF8.GetBytes(data));

		public override void Paragraph(string styleId, List<ParagraphRun> runs)
		{
			var content = UnRun(runs);

			switch (styleId)
			{
				case "Titre1":
					Write($"= {content}\n\n");
					break;
				case "Normal":
					Write($"{content}\n\n");
					break;
				case "Ellipse":
				case "Elipse":
					Write("#ellipsis()\n\n");
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
			data = SpaceBeforeQuestionMarkRegex().Replace(data, "~?");
			data = SpaceAfterQuestionMarkRegex().Replace(data, "?~");
			data = SpaceBeforeExclamationMarkRegex().Replace(data, "~!");
			data = SpaceAfterExclamationMarkRegex().Replace(data, "!~");
			data = ColonRegex().Replace(data, "~:");
			data = SemiColonRegex().Replace(data, "~;");

			return data;
		}

		public static string Replace(char c, byte[] bytes) => bytes switch
		{
			// em dash
			[0xE2, 0x80, 0x94] => "---",
			// non-breaking space
			[0xC2, 0xA0] => "~",
			_ => $"{c}",
		};

		public override void FinishExport()
		{
			_handle.Flush();
			_handle.Close();
		}
	}
}
