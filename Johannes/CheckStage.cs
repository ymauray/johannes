using Johannes.Scrubx;

namespace Johannes
{
	/// <summary>
	/// Étape <c>check</c> : vérification typographique du <c>.docx</c> avec toutes les règles (D14).
	/// Bloquante seulement s'il y a des erreurs ; les avertissements s'affichent sans arrêter la chaîne (D2).
	/// </summary>
	internal sealed class CheckStage : IStage
	{
		public Stages Id => Stages.Check;

		public string? OmissionReason(PipelineContext context) => null;

		public StageOutcome Run(PipelineContext context)
		{
			var report = DocxValidator.Validate(context.DocxPath);
			var errors = report.Errors.Where(e => !e.IsWarning).ToList();
			var warnings = report.Errors.Where(e => e.IsWarning).ToList();

			if (errors.Count > 0)
			{
				context.Out.WriteLine("Des erreurs de validation ont été détectées :");
				WriteGroupedIssues(context.Out, errors, context.Verbose);
			}

			if (warnings.Count > 0)
			{
				context.Out.WriteLine(errors.Count > 0 ? "Avertissements :" : "Le document est valide, mais des avertissements ont été relevés :");
				WriteGroupedIssues(context.Out, warnings, context.Verbose);
			}

			if (errors.Count == 0 && warnings.Count == 0)
			{
				context.Out.WriteLine(" - Le document est parfaitement valide.");
			}

			var revisionsWritten = context.Revisions is null || WriteRevisions(context, report);

			if (errors.Count > 0)
			{
				return StageOutcome.Failure(ExitCodes.CheckFailed, $"{errors.Count} erreur(s) de typographie : la chaîne s'arrête avant la génération.");
			}

			return revisionsWritten
				? StageOutcome.Success
				: StageOutcome.Failure(ExitCodes.RevisionsFailed, "la copie annotée n'a pas pu être écrite.");
		}

		/// <summary>Affiche les anomalies regroupées par règle, avec leur contexte en mode détaillé.</summary>
		internal static void WriteGroupedIssues(TextWriter output, IEnumerable<ValidationError> issues, bool verbose)
		{
			foreach (var group in issues.GroupBy(e => e.RuleName))
			{
				var code = RuleCatalog.GetCode(group.Key);
				var count = group.Count();
				var prefix = code is null ? string.Empty : $"{code} : ";
				output.WriteLine($"* {prefix}{RuleCatalog.GetTitle(group.Key)} ({count} occurrence{(count > 1 ? "s" : string.Empty)})");

				if (!verbose)
				{
					continue;
				}

				foreach (var issue in group.Where(e => !string.IsNullOrEmpty(e.Context)))
				{
					output.WriteLine($"    Contexte : {issue.Context}");
				}
			}
		}

		/// <summary>Écrit la copie annotée. Renvoie <c>false</c> si elle n'a pas pu l'être.</summary>
		private static bool WriteRevisions(PipelineContext context, ValidationReport report)
		{
			var request = context.Revisions!;
			var destination = request.Path is { Length: > 0 } path
				? Path.GetFullPath(path, context.Layout.Directory)
				: DefaultRevisionsPath(context.DocxPath);

			if (!report.Errors.Any(e => e.Fix is not null))
			{
				context.Out.WriteLine("Aucune correction automatique à proposer : aucune copie annotée n'a été écrite.");
				return true;
			}

			if (File.Exists(destination))
			{
				context.Error.WriteLine($"Erreur : le fichier '{destination}' existe déjà. Supprimez-le ou indiquez un autre nom avec --revisions=<fichier>.");
				return false;
			}

			var options = new RevisionOptions();
			if (!string.IsNullOrWhiteSpace(request.Author))
			{
				options.Author = request.Author;
			}

			try
			{
				var result = DocxRevisionWriter.Write(context.DocxPath, destination, report, options);
				context.Out.WriteLine($"Copie annotée écrite dans '{destination}' : {result.RevisionCount} révision(s) et {result.CommentCount} commentaire(s).");
				if (result.SkippedCount > 0)
				{
					context.Out.WriteLine($"{result.SkippedCount} correction(s) n'ont pas pu être écrites et restent à traiter à la main.");
				}

				return true;
			}
			catch (Exception ex) when (ex is DocxRevisionException or IOException or UnauthorizedAccessException)
			{
				context.Error.WriteLine($"Erreur : {ex.Message}");
				return false;
			}
		}

		internal static string DefaultRevisionsPath(string docxPath) =>
			Path.Combine(
				Path.GetDirectoryName(docxPath) ?? ".",
				Path.GetFileNameWithoutExtension(docxPath) + "-relu.docx");
	}
}
