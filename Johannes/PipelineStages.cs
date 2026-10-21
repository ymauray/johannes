namespace Johannes
{
	/// <summary>Étape <c>generate</c> : écrit les <c>.typ</c> et <c>.paige</c> à côté du <c>.docx</c>.</summary>
	internal sealed class GenerateStage : IStage
	{
		public Stages Id => Stages.Generate;

		public string? OmissionReason(PipelineContext context) => null;

		public StageOutcome Run(PipelineContext context)
		{
			var baseFile = Path.Combine(context.Layout.Directory, Path.GetFileNameWithoutExtension(context.DocxPath));

			try
			{
				var documentParser = new DocumentParser(context.DocxPath);

				if (context.Plan.GenerateTypst)
				{
					context.Out.WriteLine(" - Export vers Typst...");
					documentParser.ParseAndExport(new TypstExporter(baseFile));
				}

				if (context.Plan.GeneratePaige)
				{
					context.Out.WriteLine(" - Export vers Paige...");
					documentParser.ParseAndExport(new PaigeExporter(baseFile));
				}

				return StageOutcome.Success;
			}
			catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or IOException or InvalidDataException)
			{
				return StageOutcome.Failure(ExitCodes.GenerationFailed, ex.Message);
			}
		}
	}

	/// <summary>
	/// Étape dont le travail reste à écrire (phases 4, 6 et 7 de la feuille de route) :
	/// elle le signale et laisse la chaîne continuer.
	/// </summary>
	internal sealed class PendingStage(Stages id, int phase, Func<BookLayout, string?>? omissionReason = null) : IStage
	{
		public Stages Id => id;

		public string? OmissionReason(PipelineContext context) => omissionReason?.Invoke(context.Layout);

		public StageOutcome Run(PipelineContext context)
		{
			context.Out.WriteLine($" ! étape {StagePlanner.Name(id)} pas encore implémentée (phase {phase}) : rien n'a été fait.");
			return StageOutcome.Success;
		}
	}
}
