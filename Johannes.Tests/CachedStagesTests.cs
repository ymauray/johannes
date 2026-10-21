using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security;
using Xunit;

namespace Johannes.Tests;

/// <summary>Chaque étape ne refait que ce dont les entrées ont changé depuis son dernier succès.</summary>
public sealed class CachedStagesTests : IDisposable
{
	private readonly string _book = Directory.CreateTempSubdirectory("johannes-cached-").FullName;

	public void Dispose() => Directory.Delete(_book, recursive: true);

	private sealed class CountingRunner : IProcessRunner
	{
		public ConcurrentBag<string> Compiled { get; } = [];

		public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string workingDirectory)
		{
			Compiled.Add(arguments[1]);
			// Comme typst : écrit le PDF à côté de la source.
			File.WriteAllText(Path.ChangeExtension(Path.Combine(workingDirectory, arguments[1]), ".pdf"), "pdf");
			return Task.FromResult(new ProcessResult(0, string.Empty));
		}
	}

	private string Write(string relative, string content)
	{
		var path = Path.Combine(_book, relative);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	private string CreateDocx(string text)
	{
		var path = Path.Combine(_book, "Livre.docx");
		File.Delete(path);
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

	private PipelineContext Context(BuildCache cache, RevisionRequest? revisions = null)
	{
		var docx = Path.Combine(_book, "Livre.docx");
		var plan = new StagePlan(Stages.All, GenerateTypst: true, GeneratePaige: true, []);
		return new PipelineContext(docx, BookLayout.Discover(docx), plan, false, new StringWriter(), new StringWriter(), "typst", revisions, cache);
	}

	private BuildCache NewCache(string version = "v1", bool rebuild = false) => new(_book, version, rebuild);

	[Fact]
	public void Check_SecondRunOnUnchangedDocument_IsUpToDate()
	{
		CreateDocx("C’est bien.");
		var stage = new CheckStage();

		Assert.Null(stage.UpToDateReason(Context(NewCache())));
		Assert.True(stage.Run(Context(NewCache())).Ok);

		Assert.NotNull(stage.UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Check_AfterFailure_IsNotUpToDate()
	{
		CreateDocx("C'est mal.");
		var stage = new CheckStage();

		Assert.Equal(ExitCodes.CheckFailed, stage.Run(Context(NewCache())).ExitCode);

		Assert.Null(stage.UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Check_WhenDocumentChanges_IsReplayed()
	{
		CreateDocx("C’est bien.");
		var stage = new CheckStage();
		stage.Run(Context(NewCache()));

		CreateDocx("C'est mal.");

		Assert.Null(stage.UpToDateReason(Context(NewCache())));
		Assert.Equal(ExitCodes.CheckFailed, stage.Run(Context(NewCache())).ExitCode);
	}

	[Fact]
	public void Check_WithRevisionsRequested_AlwaysRuns()
	{
		CreateDocx("C’est bien.");
		var stage = new CheckStage();
		stage.Run(Context(NewCache()));

		Assert.Null(stage.UpToDateReason(Context(NewCache(), new RevisionRequest(null, null))));
	}

	[Fact]
	public void Check_WithRebuild_IsNeverUpToDate()
	{
		CreateDocx("C’est bien.");
		var stage = new CheckStage();
		stage.Run(Context(NewCache()));

		Assert.Null(stage.UpToDateReason(Context(NewCache(rebuild: true))));
	}

	[Fact]
	public void Check_WithAnotherJohannesVersion_IsReplayed()
	{
		CreateDocx("C’est bien.");
		var stage = new CheckStage();
		stage.Run(Context(NewCache("v1")));

		Assert.Null(stage.UpToDateReason(Context(NewCache("v2"))));
	}

	[Fact]
	public void Generate_SecondRunIsUpToDate_UntilAnOutputDisappears()
	{
		CreateDocx("C’est bien.");
		var stage = new GenerateStage();
		Assert.True(stage.Run(Context(NewCache())).Ok);
		Assert.NotNull(stage.UpToDateReason(Context(NewCache())));

		File.Delete(Path.Combine(_book, "Livre.paige"));

		Assert.Null(stage.UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Generate_AfterATypstOnlyRun_IsNotUpToDateForBothFormats()
	{
		CreateDocx("C’est bien.");
		var docx = Path.Combine(_book, "Livre.docx");
		var typstOnly = new PipelineContext(docx, BookLayout.Discover(docx), new StagePlan(Stages.All, true, false, []), false, new StringWriter(), new StringWriter(), Cache: NewCache());
		new GenerateStage().Run(typstOnly);

		Assert.Null(new GenerateStage().UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Pdf_RecompilesOnlyTheBindingWhoseInputsChanged()
	{
		CreateDocx("x");
		Write("Livre.typ", "texte");
		Write("support-functions.typ", "fonctions");
		Write("Papier/A.typ", "a");
		Write("Papier/B.typ", "b");
		var runner = new CountingRunner();
		var stage = new PdfStage(runner);

		Assert.True(stage.Run(Context(NewCache())).Ok);
		Assert.Equal(2, runner.Compiled.Count);

		runner.Compiled.Clear();
		Assert.True(stage.Run(Context(NewCache())).Ok);
		Assert.Empty(runner.Compiled);
		Assert.NotNull(stage.UpToDateReason(Context(NewCache())));

		Write("Papier/B.typ", "b modifié");
		Assert.True(stage.Run(Context(NewCache())).Ok);
		Assert.Equal([Path.Combine("Papier", "B.typ")], runner.Compiled);
	}

	[Fact]
	public void Pdf_WhenAnAssetChanges_RecompilesEveryBinding()
	{
		CreateDocx("x");
		Write("Livre.typ", "texte");
		Write("support-functions.typ", "fonctions");
		Write("Papier/A.typ", "a");
		Write("Papier/B.typ", "b");
		Write("Papier/Assets/common.typ", "v1");
		var runner = new CountingRunner();
		var stage = new PdfStage(runner);
		stage.Run(Context(NewCache()));
		runner.Compiled.Clear();

		Write("Papier/Assets/common.typ", "v2");
		stage.Run(Context(NewCache()));

		Assert.Equal(2, runner.Compiled.Count);
	}

	[Fact]
	public void Pdf_WhenThePdfWasDeleted_RecompilesIt()
	{
		CreateDocx("x");
		Write("Livre.typ", "texte");
		Write("Papier/A.typ", "a");
		var runner = new CountingRunner();
		var stage = new PdfStage(runner);
		stage.Run(Context(NewCache()));
		runner.Compiled.Clear();

		File.Delete(Path.Combine(_book, "Papier", "A.pdf"));
		stage.Run(Context(NewCache()));

		Assert.Single(runner.Compiled);
	}

	private void WriteEpubProject()
	{
		Write("Livre.paige", "#manifest.add(\n id: \"c\",\n href: \"c.xhtml\",\n mediaType: \"application/xhtml+xml\",\n nav: \"C\",\n)[\n<body><p>Bonjour</p></body>\n]\n");
		Write("epub/modele.paige", "#metadata(\n identifier: \"1\",\n title: \"T\",\n language: fr\n)\n#include \"../Livre.paige\"\n");
	}

	[Fact]
	public void Epub_SecondRunIsUpToDate_AndItsOwnOutputDoesNotInvalidateIt()
	{
		CreateDocx("x");
		WriteEpubProject();
		var stage = new EpubStage();

		var first = stage.Run(Context(NewCache()));
		Assert.True(first.Ok, first.Message);
		Assert.True(File.Exists(Path.Combine(_book, "epub", "Livre.epub")));

		Assert.NotNull(stage.UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Epub_WhenTheStylesheetChanges_IsRebuilt()
	{
		CreateDocx("x");
		WriteEpubProject();
		Write("support-styles.css", "p {}");
		var stage = new EpubStage();
		stage.Run(Context(NewCache()));

		Write("support-styles.css", "p { color: red }");

		Assert.Null(stage.UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Epub_WhenTheEpubWasDeleted_IsRebuilt()
	{
		CreateDocx("x");
		WriteEpubProject();
		var stage = new EpubStage();
		stage.Run(Context(NewCache()));

		File.Delete(Path.Combine(_book, "epub", "Livre.epub"));

		Assert.Null(stage.UpToDateReason(Context(NewCache())));
	}

	[Fact]
	public void Describe_ReportsUpToDateStages()
	{
		CreateDocx("C’est bien.");
		new CheckStage().Run(Context(NewCache()));
		var output = new StringWriter();
		var context = Context(NewCache()) with { Out = output };

		new Pipeline([new CheckStage(), new GenerateStage()]).Describe(context);

		Assert.Matches(@"check\s+à jour", output.ToString());
		Assert.Matches(@"generate\s+à faire", output.ToString());
	}
}
