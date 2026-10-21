using Xunit;

namespace Johannes.Tests;

public class StagePlannerTests
{
	private static StagePlan Plan(PlanRequest request)
	{
		var plan = StagePlanner.Create(request, out var error);
		Assert.True(plan is not null, error);
		return plan;
	}

	private static string Error(PlanRequest request)
	{
		var plan = StagePlanner.Create(request, out var error);
		Assert.Null(plan);
		return error!;
	}

	[Fact]
	public void Create_ByDefault_RunsTheWholeChain()
	{
		var plan = Plan(new PlanRequest());

		Assert.Equal(Stages.All, plan.Stages);
		Assert.True(plan.GenerateTypst);
		Assert.True(plan.GeneratePaige);
		Assert.Empty(plan.Notes);
	}

	[Fact]
	public void Create_WithOnly_KeepsOnlyThoseStages() =>
		Assert.Equal(Stages.Epub, Plan(new PlanRequest(Only: Stages.Epub)).Stages);

	[Fact]
	public void Create_WithOnlyGenerate_AddsCheckAsPrerequisite()
	{
		var plan = Plan(new PlanRequest(Only: Stages.Generate));

		Assert.Equal(Stages.Check | Stages.Generate, plan.Stages);
		Assert.Contains(plan.Notes, n => n.Contains("check") && n.Contains("generate"));
	}

	[Theory]
	[InlineData(Stages.Check, Stages.Check)]
	[InlineData(Stages.Generate, Stages.Check | Stages.Generate)]
	[InlineData(Stages.Pdf, Stages.Check | Stages.Generate | Stages.Pdf)]
	[InlineData(Stages.Epub, Stages.All)]
	public void Create_WithUntil_StopsAfterThatStage(Stages until, Stages expected) =>
		Assert.Equal(expected, Plan(new PlanRequest(Until: until)).Stages);

	[Fact]
	public void Create_WithSkip_RemovesThoseStages() =>
		Assert.Equal(Stages.Check | Stages.Generate | Stages.Epub, Plan(new PlanRequest(Skip: Stages.Pdf)).Stages);

	[Fact]
	public void Create_SkippingCheckWhileGenerating_IsRejected() =>
		Assert.Contains("check", Error(new PlanRequest(Skip: Stages.Check)));

	[Fact]
	public void Create_SkippingCheckAndGenerate_IsAllowed() =>
		Assert.Equal(Stages.Pdf | Stages.Epub, Plan(new PlanRequest(Skip: Stages.Check | Stages.Generate)).Stages);

	[Fact]
	public void Create_CombinesOnlyUntilAndSkip()
	{
		var plan = Plan(new PlanRequest(Only: Stages.Pdf | Stages.Epub, Until: Stages.Pdf, Skip: Stages.Epub));

		Assert.Equal(Stages.Pdf, plan.Stages);
	}

	[Fact]
	public void Create_WithNothingLeft_IsRejected() =>
		Assert.Contains("Aucune étape", Error(new PlanRequest(Only: Stages.Pdf, Until: Stages.Check)));

	[Fact]
	public void Create_WithUntilOfSeveralStages_IsRejected() =>
		Assert.Contains("--until", Error(new PlanRequest(Until: Stages.Pdf | Stages.Epub)));

	[Fact]
	public void Create_WithTypstOnly_DropsPaigeBranch()
	{
		var plan = Plan(new PlanRequest(TypstOnly: true));

		Assert.Equal(Stages.Check | Stages.Generate | Stages.Pdf, plan.Stages);
		Assert.True(plan.GenerateTypst);
		Assert.False(plan.GeneratePaige);
	}

	[Fact]
	public void Create_WithPaigeOnly_DropsTypstBranch()
	{
		var plan = Plan(new PlanRequest(PaigeOnly: true));

		Assert.Equal(Stages.Check | Stages.Generate | Stages.Epub, plan.Stages);
		Assert.False(plan.GenerateTypst);
		Assert.True(plan.GeneratePaige);
	}

	[Fact]
	public void Create_WithoutTypst_DropsPdfAndTypstGeneration()
	{
		var plan = Plan(new PlanRequest(WithoutTypst: true));

		Assert.Equal(Stages.Check | Stages.Generate | Stages.Epub, plan.Stages);
		Assert.False(plan.GenerateTypst);
		Assert.True(plan.GeneratePaige);
	}

	[Fact]
	public void Create_WithoutPaige_DropsEpubAndPaigeGeneration()
	{
		var plan = Plan(new PlanRequest(WithoutPaige: true));

		Assert.Equal(Stages.Check | Stages.Generate | Stages.Pdf, plan.Stages);
		Assert.True(plan.GenerateTypst);
		Assert.False(plan.GeneratePaige);
	}

	[Fact]
	public void Create_WithoutBothFormats_OnlyChecks() =>
		Assert.Equal(Stages.Check, Plan(new PlanRequest(WithoutTypst: true, WithoutPaige: true)).Stages);

	[Fact]
	public void Create_WithTypstAndPaigeOnly_IsRejected() =>
		Assert.Contains("mutuellement exclusives", Error(new PlanRequest(TypstOnly: true, PaigeOnly: true)));

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void Create_WithTypstOnlyAndWithout_IsRejected(bool withoutTypst, bool withoutPaige) =>
		Assert.Contains("--typst", Error(new PlanRequest(TypstOnly: true, WithoutTypst: withoutTypst, WithoutPaige: withoutPaige)));

	[Fact]
	public void TryParse_AcceptsCommaSeparatedNamesInAnyCase()
	{
		Assert.True(StagePlanner.TryParse("Check, pdf", out var stages, out _));
		Assert.Equal(Stages.Check | Stages.Pdf, stages);
	}

	[Theory]
	[InlineData("typst")]
	[InlineData("all")]
	[InlineData("check,inconnue")]
	[InlineData("")]
	public void TryParse_RejectsUnknownOrEmptyNames(string text)
	{
		Assert.False(StagePlanner.TryParse(text, out _, out var error));
		Assert.False(string.IsNullOrEmpty(error));
	}
}
