using System.IO.Compression;
using Xunit;

namespace Johannes.Tests;

public sealed class EpubStageTests : IDisposable
{
	private readonly string _book = Directory.CreateTempSubdirectory("johannes-epub-").FullName;

	public void Dispose() => Directory.Delete(_book, recursive: true);

	private const string ValidPaige = """
		#metadata(
		    identifier: "978-2-940855-01-8",
		    title: "Petit livre",
		    language: fr
		)

		#manifest.add(
		    id: "chap1",
		    href: "chap1.xhtml",
		    mediaType: "application/xhtml+xml",
		    nav: "Chapitre 1",
		)[
		<body><h1>Bonjour</h1></body>
		]
		""";

	private (StageOutcome Outcome, StringWriter Out) Run(string? paige, string docx = "Livre.docx")
	{
		var epub = Path.Combine(_book, "epub");
		Directory.CreateDirectory(epub);
		var sources = new List<string>();
		if (paige is not null)
		{
			var file = Path.Combine(epub, "livre.paige");
			File.WriteAllText(file, paige);
			sources.Add(file);
		}

		var output = new StringWriter();
		var layout = new BookLayout(_book, null, [], epub, sources);
		var plan = new StagePlan(Stages.All, GenerateTypst: true, GeneratePaige: true, []);
		var context = new PipelineContext(Path.Combine(_book, docx), layout, plan, false, output, TextWriter.Null);
		return (new EpubStage().Run(context), output);
	}

	[Fact]
	public void Run_WritesAnEpubNamedAfterTheDocument()
	{
		var (outcome, output) = Run(ValidPaige);

		var epub = Path.Combine(_book, "epub", "Livre.epub");
		Assert.True(outcome.Ok, outcome.Message);
		Assert.True(File.Exists(epub));
		using var zip = ZipFile.OpenRead(epub);
		Assert.NotNull(zip.GetEntry("OEBPS/content.opf"));
		Assert.Contains("Livre.epub", output.ToString());
	}

	[Fact]
	public void Run_WithInvalidSource_FailsWithPaigeFailedAndLeavesNoEpub()
	{
		var (outcome, _) = Run("#manifest.add(id: \"x\")");

		Assert.Equal(ExitCodes.PaigeFailed, outcome.ExitCode);
		Assert.False(File.Exists(Path.Combine(_book, "epub", "Livre.epub")));
	}

	[Fact]
	public void Run_WithMissingIncludedFile_FailsWithPaigeFailed()
	{
		var (outcome, _) = Run(ValidPaige + "\n#include \"absent.paige\"\n");

		Assert.Equal(ExitCodes.PaigeFailed, outcome.ExitCode);
		Assert.Contains("absent.paige", outcome.Message);
	}

	[Fact]
	public void Run_WithoutExactlyOnePaigeFile_Fails()
	{
		var (outcome, _) = Run(null);

		Assert.Equal(ExitCodes.PaigeFailed, outcome.ExitCode);
	}

	[Fact]
	public void OmissionReason_WithoutEpubDirectory_ExplainsWhy()
	{
		var context = new PipelineContext("x.docx", new BookLayout(_book, null, [], null, []), new StagePlan(Stages.All, true, true, []), false, TextWriter.Null, TextWriter.Null);

		Assert.Contains("epub", new EpubStage().OmissionReason(context));
	}
}
