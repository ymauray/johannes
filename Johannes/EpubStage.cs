using System.Xml;
using Johannes.Paige;

namespace Johannes
{
	/// <summary>
	/// Étape <c>epub</c> : compile l'unique <c>.paige</c> de <c>epub/</c> avec le moteur intégré,
	/// vers <c>epub/&lt;nom-du-docx&gt;.epub</c>. Rien n'est refait si les entrées n'ont pas changé.
	/// </summary>
	internal sealed class EpubStage : IStage
	{
		private const string Unit = "epub";

		public Stages Id => Stages.Epub;

		public string? OmissionReason(PipelineContext context) => context.Layout.EpubOmissionReason;

		public string? UpToDateReason(PipelineContext context) =>
			context.Layout.EpubDirectory is not null
			&& context.Cache?.IsFresh(Unit, FingerprintOf(context)) == true
			&& File.Exists(OutputOf(context))
				? "entrées inchangées"
				: null;

		public StageOutcome Run(PipelineContext context)
		{
			if (UpToDateReason(context) is { } fresh)
			{
				context.Out.WriteLine($" - à jour ({fresh})");
				return StageOutcome.Success;
			}

			var layout = context.Layout;

			// Garanti par les contrôles préalables (D5) ; vérifié ici pour ne pas dépendre de l'appelant.
			if (layout.EpubDirectory is null || layout.EpubSources.Count != 1)
			{
				return StageOutcome.Failure(ExitCodes.PaigeFailed, $"le dossier '{layout.EpubName}' doit contenir exactement un fichier .paige.");
			}

			var output = OutputOf(context);
			var fingerprint = FingerprintOf(context);

			try
			{
				var document = Parser.Parse(File.ReadAllText(layout.EpubSources[0]), layout.EpubDirectory);
				Epub.Write(document, layout.EpubDirectory, output);
				context.Out.WriteLine($" - {Path.GetRelativePath(layout.Directory, output)}");
				context.Cache?.Record(Unit, fingerprint);
				return StageOutcome.Success;
			}
			catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or XmlException)
			{
				// Ne pas laisser un EPUB tronqué passer pour un livre achevé.
				TryDelete(output);
				return StageOutcome.Failure(ExitCodes.PaigeFailed, ex.Message);
			}
		}

		private static string OutputOf(PipelineContext context) =>
			Path.Combine(context.Layout.EpubDirectory ?? context.Layout.Directory, Path.GetFileNameWithoutExtension(context.DocxPath) + ".epub");

		/// <summary>Le <c>.paige</c> généré, la feuille de style et tout le projet <c>epub/</c> (hors l'EPUB produit).</summary>
		private static string FingerprintOf(PipelineContext context)
		{
			var directory = context.Layout.Directory;
			var output = OutputOf(context);
			return Fingerprint.Compute(
				directory,
				[
					Path.Combine(directory, Path.GetFileNameWithoutExtension(context.DocxPath) + ".paige"),
					Path.Combine(directory, "support-styles.css"),
					context.Layout.EpubDirectory ?? directory
				],
				exclude: file => string.Equals(file, output, StringComparison.OrdinalIgnoreCase));
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
