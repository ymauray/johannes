namespace Johannes
{
	/// <summary>
	/// Étape <c>pdf</c> : compile chaque <c>Papier/*.typ</c> avec <c>typst</c>, en parallèle.
	/// Une reliure dont les entrées n'ont pas changé depuis sa dernière réussite n'est pas recompilée.
	/// La sortie de <c>typst</c> est reprise telle quelle en cas d'échec.
	/// </summary>
	internal sealed class PdfStage(IProcessRunner? runner = null) : IStage
	{
		private readonly IProcessRunner _runner = runner ?? new SystemProcessRunner();

		public Stages Id => Stages.Pdf;

		public string? OmissionReason(PipelineContext context) => context.Layout.PdfOmissionReason;

		public string? UpToDateReason(PipelineContext context) =>
			context.Layout.PaperSources.All(source => IsFresh(context, source)) ? "entrées inchangées" : null;

		public StageOutcome Run(PipelineContext context)
		{
			if (context.TypstPath is null)
			{
				return StageOutcome.Failure(ExitCodes.MissingDependency, "typst introuvable dans le PATH.");
			}

			// Une tâche par reliure à refaire ; null pour celles qui sont à jour.
			var results = context.Layout.PaperSources
				.Select(source => (Source: source, Fingerprint: FingerprintOf(context, source)))
				.Select(item => (item.Source, item.Fingerprint, Task: IsFresh(context, item.Source) ? null : CompileAsync(context, item.Source)))
				.ToList();
			Task.WaitAll(results.Where(r => r.Task is not null).Select(r => r.Task!).ToArray());

			var failed = new List<string>();
			foreach (var (source, fingerprint, task) in results)
			{
				var name = Path.GetFileName(source);
				var pdf = Path.ChangeExtension(name, ".pdf");

				if (task is null)
				{
					context.Out.WriteLine($" - {pdf} : à jour");
					continue;
				}

				var result = task.Result;
				if (result.ExitCode == 0)
				{
					context.Out.WriteLine($" - {pdf}");
					context.Cache?.Record(Unit(context, source), fingerprint);
					if (context.Verbose && result.Output.Length > 0)
					{
						context.Out.WriteLine(result.Output);
					}

					continue;
				}

				failed.Add(name);
				context.Error.WriteLine($"typst compile {name} (code {result.ExitCode}) :");
				if (result.Output.Length > 0)
				{
					context.Error.WriteLine(result.Output);
				}
			}

			return failed.Count == 0
				? StageOutcome.Success
				: StageOutcome.Failure(ExitCodes.TypstFailed, $"échec de typst compile : {string.Join(", ", failed)}.");
		}

		private static string Unit(PipelineContext context, string source) =>
			"pdf:" + Path.GetRelativePath(context.Layout.Directory, source).Replace(Path.DirectorySeparatorChar, '/');

		/// <summary>Le texte généré, les fonctions de support, la reliure et toutes les ressources de mise en page.</summary>
		private static string FingerprintOf(PipelineContext context, string source)
		{
			var directory = context.Layout.Directory;
			var generated = Path.Combine(directory, Path.GetFileNameWithoutExtension(context.DocxPath) + ".typ");
			var assets = Path.Combine(context.Layout.PaperDirectory ?? directory, "Assets");
			return Fingerprint.Compute(directory, [generated, Path.Combine(directory, "support-functions.typ"), source, assets]);
		}

		private static bool IsFresh(PipelineContext context, string source) =>
			context.Cache?.IsFresh(Unit(context, source), FingerprintOf(context, source)) == true
			&& File.Exists(Path.ChangeExtension(source, ".pdf"));

		private Task<ProcessResult> CompileAsync(PipelineContext context, string source)
		{
			var relative = Path.GetRelativePath(context.Layout.Directory, source);
			if (context.Verbose)
			{
				context.Out.WriteLine($" > typst compile \"{relative}\" --root .");
			}

			return _runner.RunAsync(context.TypstPath!, ["compile", relative, "--root", "."], context.Layout.Directory);
		}
	}
}
