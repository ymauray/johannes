using Xunit;

namespace Johannes.Tests;

public class PipelineTests
{
	private sealed class FakeStage(Stages id, List<string> journal, StageOutcome? outcome = null, string? omission = null) : IStage
	{
		public Stages Id => id;

		public string? OmissionReason(PipelineContext context) => omission;

		public StageOutcome Run(PipelineContext context)
		{
			journal.Add(StagePlanner.Name(id));
			return outcome ?? StageOutcome.Success;
		}
	}

	private static readonly BookLayout Layout = new("dossier", null, [], null, []);

	private static (PipelineContext Context, StringWriter Out, StringWriter Error) NewContext(Stages stages, bool verbose = false)
	{
		var output = new StringWriter();
		var error = new StringWriter();
		var plan = new StagePlan(stages, GenerateTypst: true, GeneratePaige: true, []);
		return (new PipelineContext("livre.docx", Layout, plan, verbose, output, error), output, error);
	}

	private static Pipeline Chain(List<string> journal, Func<Stages, FakeStage?>? custom = null)
	{
		// Volontairement dans le désordre : le pipeline doit imposer l'ordre des étapes.
		IReadOnlyList<IStage> stages = new[] { Stages.Epub, Stages.Check, Stages.Pdf, Stages.Generate }
			.Select(id => (IStage)(custom?.Invoke(id) ?? new FakeStage(id, journal)))
			.ToList();
		return new Pipeline(stages);
	}

	[Fact]
	public void Run_ExecutesPlannedStagesInOrder()
	{
		var journal = new List<string>();
		var (context, _, _) = NewContext(Stages.All);

		var exitCode = Chain(journal).Run(context);

		Assert.Equal(ExitCodes.Success, exitCode);
		Assert.Equal(["check", "generate", "pdf", "epub"], journal);
	}

	[Fact]
	public void Run_SkipsStagesOutsideThePlan()
	{
		var journal = new List<string>();
		var (context, _, _) = NewContext(Stages.Check | Stages.Epub);

		Chain(journal).Run(context);

		Assert.Equal(["check", "epub"], journal);
	}

	[Fact]
	public void Run_StopsAtTheFirstFailureAndReturnsItsCode()
	{
		var journal = new List<string>();
		var (context, _, error) = NewContext(Stages.All);
		var pipeline = Chain(journal, id => id == Stages.Generate
			? new FakeStage(id, journal, StageOutcome.Failure(ExitCodes.GenerationFailed, "boum"))
			: null);

		var exitCode = pipeline.Run(context);

		Assert.Equal(ExitCodes.GenerationFailed, exitCode);
		Assert.Equal(["check", "generate"], journal);
		Assert.Contains("boum", error.ToString());
		Assert.Contains("[generate]", error.ToString());
	}

	[Fact]
	public void Run_DoesNotRunOmittedStagesButContinues()
	{
		var journal = new List<string>();
		var (context, output, _) = NewContext(Stages.All);
		var pipeline = Chain(journal, id => id == Stages.Pdf ? new FakeStage(id, journal, omission: "pas de dossier 'Papier'") : null);

		var exitCode = pipeline.Run(context);

		Assert.Equal(ExitCodes.Success, exitCode);
		Assert.Equal(["check", "generate", "epub"], journal);
		Assert.Contains("[pdf] omise : pas de dossier 'Papier'", output.ToString());
	}

	[Fact]
	public void Run_MentionsUnrequestedStagesOnlyInVerboseMode()
	{
		var journal = new List<string>();
		var quiet = NewContext(Stages.Check);
		var verbose = NewContext(Stages.Check, verbose: true);

		Chain(journal).Run(quiet.Context);
		Chain(journal).Run(verbose.Context);

		Assert.DoesNotContain("non demandée", quiet.Out.ToString());
		Assert.Contains("[pdf] non demandée", verbose.Out.ToString());
	}

	[Fact]
	public void Describe_ListsEveryStageWithoutRunningAnything()
	{
		var journal = new List<string>();
		var plan = new StagePlan(Stages.Check | Stages.Generate | Stages.Pdf, true, true, ["note d'essai"]);
		var output = new StringWriter();
		var context = new PipelineContext("livre.docx", Layout, plan, false, output, new StringWriter());
		var pipeline = Chain(journal, id => id == Stages.Pdf ? new FakeStage(id, journal, omission: "aucun .typ") : null);

		pipeline.Describe(context);

		Assert.Empty(journal);
		var text = output.ToString();
		Assert.Contains("note d'essai", text);
		Assert.Matches(@"check\s+à faire", text);
		Assert.Matches(@"pdf\s+omise \(aucun \.typ\)", text);
		Assert.Matches(@"epub\s+non demandée", text);
	}
}
