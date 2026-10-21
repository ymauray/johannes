namespace Johannes
{
	/// <summary>Étape <c>generate</c> : écrit les <c>.typ</c> et <c>.paige</c> à côté du <c>.docx</c>.</summary>
	internal sealed class GenerateStage : IStage
	{
		private const string Unit = "generate";

		public Stages Id => Stages.Generate;

		public string? OmissionReason(PipelineContext context) => null;

		public string? UpToDateReason(PipelineContext context) =>
			context.Cache?.IsFresh(Unit, FingerprintOf(context)) == true && Outputs(context).All(File.Exists)
				? "document inchangé"
				: null;

		public StageOutcome Run(PipelineContext context)
		{
			if (UpToDateReason(context) is { } fresh)
			{
				context.Out.WriteLine($" - à jour ({fresh})");
				return StageOutcome.Success;
			}

			var fingerprint = FingerprintOf(context);
			var baseFile = BaseFile(context);

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

				context.Cache?.Record(Unit, fingerprint);
				return StageOutcome.Success;
			}
			catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or IOException or InvalidDataException)
			{
				return StageOutcome.Failure(ExitCodes.GenerationFailed, ex.Message);
			}
		}

		private static string BaseFile(PipelineContext context) =>
			Path.Combine(context.Layout.Directory, Path.GetFileNameWithoutExtension(context.DocxPath));

		/// <summary>Le document et les formats demandés : changer de branche (<c>--typst</c>, <c>--paige</c>) change l'empreinte.</summary>
		private static string FingerprintOf(PipelineContext context) =>
			Fingerprint.Compute(
				context.Layout.Directory,
				[context.DocxPath],
				$"typst={context.Plan.GenerateTypst};paige={context.Plan.GeneratePaige}");

		private static IEnumerable<string> Outputs(PipelineContext context)
		{
			if (context.Plan.GenerateTypst)
			{
				yield return BaseFile(context) + ".typ";
				yield return Path.Combine(context.Layout.Directory, "support-functions.typ");
			}

			if (context.Plan.GeneratePaige)
			{
				yield return BaseFile(context) + ".paige";
			}
		}
	}
}
