using System.IO.Compression;
using System.Security;
using Xunit;

namespace Johannes.Tests;

public class CheckStageTests : IDisposable
{
	private readonly string _directory = Directory.CreateTempSubdirectory("johannes-check-").FullName;

	public void Dispose() => Directory.Delete(_directory, recursive: true);

	private string CreateDocx(string text, string name = "livre.docx")
	{
		var path = Path.Combine(_directory, name);
		using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
		using var writer = new StreamWriter(archive.CreateEntry("word/document.xml").Open());
		writer.Write($"""
			<?xml version="1.0" encoding="utf-8"?>
			<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
			  <w:body>
			    <w:p><w:pPr><w:pStyle w:val="Titre1"/></w:pPr><w:r><w:t>Titre</w:t></w:r></w:p>
			    <w:p><w:r><w:t>{SecurityElement.Escape(text)}</w:t></w:r></w:p>
			  </w:body>
			</w:document>
			""");
		return path;
	}

	private (StageOutcome Outcome, string Out, string Error) Run(string docxPath, bool verbose = false, RevisionRequest? revisions = null)
	{
		var output = new StringWriter();
		var error = new StringWriter();
		var layout = new BookLayout(_directory, null, [], null, []);
		var plan = new StagePlan(Stages.All, GenerateTypst: true, GeneratePaige: true, []);
		var context = new PipelineContext(docxPath, layout, plan, verbose, output, error, Revisions: revisions);
		return (new CheckStage().Run(context), output.ToString(), error.ToString());
	}

	[Fact]
	public void Run_ValidDocument_Succeeds()
	{
		var (outcome, output, _) = Run(CreateDocx("C’est un bel été."));

		Assert.True(outcome.Ok);
		Assert.Contains("parfaitement valide", output);
	}

	[Fact]
	public void Run_WithErrors_FailsWithCheckFailedAndListsRules()
	{
		var (outcome, output, _) = Run(CreateDocx("C'est un bel été."));

		Assert.Equal(ExitCodes.CheckFailed, outcome.ExitCode);
		Assert.Contains("APOS", output);
		Assert.Contains("1 occurrence", output);
	}

	[Fact]
	public void Run_WithWarningsOnly_Succeeds()
	{
		var (outcome, output, _) = Run(CreateDocx("Du pain, du vin, et du fromage."));

		Assert.True(outcome.Ok);
		Assert.Contains("VIRGET", output);
		Assert.Contains("avertissements", output);
	}

	[Fact]
	public void Run_Verbose_ShowsContext()
	{
		var docx = CreateDocx("C'est un bel été.");

		var quiet = Run(docx).Out;
		var verbose = Run(docx, verbose: true).Out;

		Assert.DoesNotContain("Contexte :", quiet);
		Assert.Contains("Contexte :", verbose);
	}

	[Fact]
	public void Run_WithRevisions_WritesAnnotatedCopyAtDefaultPath()
	{
		var (outcome, output, _) = Run(CreateDocx("C'est un bel été."), revisions: new RevisionRequest(null, "Relecteur"));

		Assert.Equal(ExitCodes.CheckFailed, outcome.ExitCode);
		Assert.True(File.Exists(Path.Combine(_directory, "livre-relu.docx")));
		Assert.Contains("Copie annotée écrite", output);
	}

	[Fact]
	public void Run_WithRevisionsAndCustomRelativePath_WritesNextToTheDocument()
	{
		Run(CreateDocx("C'est un bel été."), revisions: new RevisionRequest("corrige.docx", null));

		Assert.True(File.Exists(Path.Combine(_directory, "corrige.docx")));
	}

	[Fact]
	public void Run_WithRevisionsAndNothingToFix_SkipsTheCopyWhenNothingToFix()
	{
		var docx = CreateDocx("C’est un bel été.");
		var (outcome, output, _) = Run(docx, revisions: new RevisionRequest(null, null));

		// Aucune correction à proposer : pas de copie, pas d'échec.
		Assert.True(outcome.Ok);
		Assert.Contains("Aucune correction automatique", output);
		Assert.False(File.Exists(Path.Combine(_directory, "livre-relu.docx")));
	}

	[Fact]
	public void Run_WithRevisionsAndExistingDestination_ReportsError()
	{
		var docx = CreateDocx("C'est un bel été.");
		File.WriteAllText(Path.Combine(_directory, "livre-relu.docx"), "déjà là");

		var (outcome, _, error) = Run(docx, revisions: new RevisionRequest(null, null));

		// Les erreurs de typographie l'emportent sur l'échec d'écriture.
		Assert.Equal(ExitCodes.CheckFailed, outcome.ExitCode);
		Assert.Contains("existe déjà", error);
	}
}
