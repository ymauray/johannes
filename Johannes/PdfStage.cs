namespace Johannes
{
	/// <summary>
	/// Étape <c>pdf</c> : compile chaque <c>Papier/*.typ</c> avec <c>typst</c>, en parallèle.
	/// La sortie de <c>typst</c> est reprise telle quelle en cas d'échec.
	/// </summary>
	internal sealed class PdfStage(IProcessRunner? runner = null) : IStage
	{
		private readonly IProcessRunner _runner = runner ?? new SystemProcessRunner();

		public Stages Id => Stages.Pdf;

		public string? OmissionReason(PipelineContext context) => context.Layout.PdfOmissionReason;

		public StageOutcome Run(PipelineContext context)
		{
			if (context.TypstPath is null)
			{
				return StageOutcome.Failure(ExitCodes.MissingDependency, "typst introuvable dans le PATH.");
			}

			var results = context.Layout.PaperSources
				.Select(source => (Source: source, Task: CompileAsync(context, source)))
				.ToList();
			Task.WaitAll(results.Select(r => r.Task).ToArray());

			var failed = new List<string>();
			foreach (var (source, task) in results)
			{
				var name = Path.GetFileName(source);
				var result = task.Result;

				if (result.ExitCode == 0)
				{
					context.Out.WriteLine($" - {Path.ChangeExtension(name, ".pdf")}");
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
