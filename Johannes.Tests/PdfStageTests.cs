using System.Collections.Concurrent;
using Xunit;

namespace Johannes.Tests;

public class PdfStageTests
{
	private sealed class FakeRunner(Func<string, ProcessResult> respond) : IProcessRunner
	{
		public ConcurrentBag<(string Executable, string[] Arguments, string WorkingDirectory)> Calls { get; } = [];

		public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string workingDirectory)
		{
			Calls.Add((executable, [.. arguments], workingDirectory));
			return Task.FromResult(respond(arguments[1]));
		}
	}

	private static readonly string Dir = Path.Combine(Path.GetTempPath(), "livre");

	private static (PipelineContext Context, StringWriter Out, StringWriter Error) NewContext(string? typst = "typst", bool verbose = false, params string[] sources)
	{
		var layout = new BookLayout(Dir, Path.Combine(Dir, "Papier"), sources.Select(s => Path.Combine(Dir, s)).ToList(), null, []);
		var output = new StringWriter();
		var error = new StringWriter();
		var plan = new StagePlan(Stages.All, GenerateTypst: true, GeneratePaige: true, []);
		return (new PipelineContext(Path.Combine(Dir, "Livre.docx"), layout, plan, verbose, output, error, typst), output, error);
	}

	[Fact]
	public void Run_CompilesEachSourceFromTheBookDirectory()
	{
		var runner = new FakeRunner(_ => new ProcessResult(0, string.Empty));
		var (context, output, _) = NewContext("C:\bin\typst.exe", false, Path.Combine("Papier", "A.typ"), Path.Combine("Papier", "B.typ"));

		var outcome = new PdfStage(runner).Run(context);

		Assert.True(outcome.Ok);
		Assert.Equal(2, runner.Calls.Count);
		Assert.All(runner.Calls, call =>
		{
			Assert.Equal("C:\bin\typst.exe", call.Executable);
			Assert.Equal(Dir, call.WorkingDirectory);
			Assert.Equal(["compile", "--root", "."], new[] { call.Arguments[0], call.Arguments[2], call.Arguments[3] });
		});
		Assert.Equivalent(new[] { Path.Combine("Papier", "A.typ"), Path.Combine("Papier", "B.typ") }, runner.Calls.Select(c => c.Arguments[1]));
		Assert.Contains("A.pdf", output.ToString());
		Assert.Contains("B.pdf", output.ToString());
	}

	[Fact]
	public void Run_WhenATypstCompilationFails_ReportsItsOutputAndExitCode6()
	{
		var runner = new FakeRunner(file => file.EndsWith("B.typ") ? new ProcessResult(1, "error: fichier introuvable") : new ProcessResult(0, string.Empty));
		var (context, _, error) = NewContext("typst", false, Path.Combine("Papier", "A.typ"), Path.Combine("Papier", "B.typ"));

		var outcome = new PdfStage(runner).Run(context);

		Assert.Equal(ExitCodes.TypstFailed, outcome.ExitCode);
		Assert.Contains("B.typ", outcome.Message);
		Assert.DoesNotContain("A.typ", outcome.Message);
		Assert.Contains("error: fichier introuvable", error.ToString());
	}

	[Fact]
	public void Run_WithoutTypst_FailsWithMissingDependency()
	{
		var (context, _, _) = NewContext(null, false, Path.Combine("Papier", "A.typ"));

		var outcome = new PdfStage(new FakeRunner(_ => new ProcessResult(0, string.Empty))).Run(context);

		Assert.Equal(ExitCodes.MissingDependency, outcome.ExitCode);
	}

	[Fact]
	public void OmissionReason_WithoutPaperDirectory_ExplainsWhy()
	{
		var context = new PipelineContext("x.docx", new BookLayout(Dir, null, [], null, []), new StagePlan(Stages.All, true, true, []), false, TextWriter.Null, TextWriter.Null);

		Assert.Contains("Papier", new PdfStage().OmissionReason(context));
	}

	[Fact]
	public async Task SystemProcessRunner_CapturesExitCodeAndOutput()
	{
		var (exe, args) = OperatingSystem.IsWindows()
			? ("cmd.exe", new[] { "/c", "echo bonjour& exit 3" })
			: ("sh", ["-c", "echo bonjour; exit 3"]);

		var result = await new SystemProcessRunner().RunAsync(exe, args, Path.GetTempPath());

		Assert.Equal(3, result.ExitCode);
		Assert.Contains("bonjour", result.Output);
	}
}
