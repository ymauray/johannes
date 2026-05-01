using System.Text.RegularExpressions;

namespace Johannes
{
	public partial class PaigeExporter : AbstractExporter
	{
		private readonly FileStream _handle;

		private bool chapterNeedsToBeClosed = false;

		public PaigeExporter(string baseFile)
		{
			var filename = $"{baseFile}.paige";
			// delete file if it already exists to avoid appending to an old file
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
			_handle = File.OpenWrite(filename);
		}

		protected void Write(string data) => _handle.Write(System.Text.Encoding.UTF8.GetBytes(data));

		public override void Paragraph(string styleId, List<ParagraphRun> runs)
		{
			var content = UnRun(runs);

			switch (styleId)
			{
				case "Titre1":
					if (chapterNeedsToBeClosed)
					{
						Write($"""
						</body>
						]


						""");
					}
					// Sanitize content to be used as an ID by replacing spaces with underscores and removing non-alphanumeric characters
					var id = SpacesRegex().Replace(content, "_");
					id = NonWordRegex().Replace(id, "");
					id = id.ToLowerInvariant();
					Write($$"""
					#manifest.add(
						id: "{{id}}",
						href: "{{id}}.xhtml",
						mediaType: "application/xhtml+xml",
						spine: true,
						nav: "{{content}}",
					)
					[
					<head>
						<title>{{content}}</title>
						<style>
							p {
								text-indent: 1.25em;
								margin: 0.25em;
							}

							p.ellipse {
								text-align: center;
								text-indent: initial;
								margin: initial;
							}

							h1 {
								text-indent: initial;
								margin-top: 0;
							}

							h1.mid-title {
								text-align: center;
								text-indent: initial;
								margin: initial;
							}
						</style>
					</head>
					<body class="chapter">
						<h1>{{content}}</h1>
					""");
					chapterNeedsToBeClosed = true;
					break;
				case "Normal":
					Write($"""	
						<p>{content}</p>
					
					""");
					break;
				case "Ellipse":
				case "Elipse":
					Write($"""
						<p class="ellipse">***</p>
					
					""");
					break;
				case "Titre":
					Write($"""
						<h1 class="mid-title">{content}</h1>
					
					""");
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
					content = $"<em>{content}</em>";
				}
				sb.Append(content);
			}

			var data = sb.ToString();
			data = SpaceBeforeQuestionMarkRegex().Replace(data, "&#160;?");
			data = SpaceAfterQuestionMarkRegex().Replace(data, "?&#160;");
			data = SpaceBeforeExclamationMarkRegex().Replace(data, "&#160;!");
			data = SpaceAfterExclamationMarkRegex().Replace(data, "!&#160;");
			data = ColonRegex().Replace(data, "&#160;:");
			data = SemiColonRegex().Replace(data, "&#160;;");

			return data;
		}

		private static string Replace(char c, byte[] bytes) => bytes switch
		{
			// em dash
			[0xE2, 0x80, 0x94] => "&#x2014;",
			// non-breaking space
			[0xC2, 0xA0] => "&#160;",
			_ => $"{c}",
		};

		public override void FinishExport()
		{
			if (chapterNeedsToBeClosed)
			{
				Write($"""
				</body>
				]


				""");
			}

			_handle.Flush();
			_handle.Close();
		}

		[GeneratedRegex(@"\s+")]
		private static partial Regex SpacesRegex();
		[GeneratedRegex(@"[^\w]")]
		private static partial Regex NonWordRegex();
	}
}
