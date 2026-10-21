using System.Xml;
using Johannes.Paige;

namespace Johannes
{
	/// <summary>
	/// Étape <c>epub</c> : compile l'unique <c>.paige</c> de <c>epub/</c> avec le moteur intégré,
	/// vers <c>epub/&lt;nom-du-docx&gt;.epub</c>.
	/// </summary>
	internal sealed class EpubStage : IStage
	{
		public Stages Id => Stages.Epub;

		public string? OmissionReason(PipelineContext context) => context.Layout.EpubOmissionReason;

		public StageOutcome Run(PipelineContext context)
		{
			var layout = context.Layout;

			// Garanti par les contrôles préalables (D5) ; vérifié ici pour ne pas dépendre de l'appelant.
			if (layout.EpubDirectory is null || layout.EpubSources.Count != 1)
			{
				return StageOutcome.Failure(ExitCodes.PaigeFailed, $"le dossier '{BookLayout.EpubDirectoryName}' doit contenir exactement un fichier .paige.");
			}

			var output = Path.Combine(layout.EpubDirectory, Path.GetFileNameWithoutExtension(context.DocxPath) + ".epub");

			try
			{
				var document = Parser.Parse(File.ReadAllText(layout.EpubSources[0]), layout.EpubDirectory);
				Epub.Write(document, layout.EpubDirectory, output);
				context.Out.WriteLine($" - {Path.GetRelativePath(layout.Directory, output)}");
				return StageOutcome.Success;
			}
			catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or XmlException)
			{
				// Ne pas laisser un EPUB tronqué passer pour un livre achevé.
				TryDelete(output);
				return StageOutcome.Failure(ExitCodes.PaigeFailed, ex.Message);
			}
		}

		private static void TryDelete(string file)
		{
			try
			{
				File.Delete(file);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
	}
}
