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
}
