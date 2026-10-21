using Xunit;

namespace Johannes.Tests;

public sealed class PreflightTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "johannes-tests-" + Guid.NewGuid().ToString("N"));
	private readonly string _bin;
	private readonly string _book;

	public PreflightTests()
	{
		_bin = Directory.CreateDirectory(Path.Combine(_root, "bin")).FullName;
		_book = Directory.CreateDirectory(Path.Combine(_root, "livre")).FullName;
	}

	public void Dispose() => Directory.Delete(_root, recursive: true);

	private string Docx(string name = "Livre.docx")
	{
		var path = Path.Combine(_book, name);
		File.WriteAllText(path, string.Empty);
		return path;
	}

	private void Touch(params string[] relativePath)
	{
		var path = Path.Combine([_book, .. relativePath]);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, string.Empty);
	}

	/// <summary>Installe un faux <c>typst</c> dans <c>bin/</c> et renvoie un <c>PATH</c> qui le contient.</summary>
	private string PathWithTypst()
	{
		var file = Path.Combine(_bin, OperatingSystem.IsWindows() ? "typst.exe" : "typst");
		File.WriteAllText(file, string.Empty);
		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}
		return _bin;
	}

	private string PathWithoutTypst() => Directory.CreateDirectory(Path.Combine(_root, "vide")).FullName;

	[Fact]
	public void Run_WithMissingDocument_ReturnsDocumentNotFound()
	{
		var report = Preflight.Run(Path.Combine(_book, "absent.docx"), Stages.Check, PathWithoutTypst());

		Assert.False(report.Ok);
		Assert.Equal(ExitCodes.DocumentNotFound, report.ExitCode);
		Assert.Null(report.Layout);
	}

	[Fact]
	public void Run_WithWrongExtension_ReturnsNotADocx()
	{
		var path = Path.Combine(_book, "Livre.txt");
		File.WriteAllText(path, string.Empty);

		var report = Preflight.Run(path, Stages.Check, PathWithoutTypst());

		Assert.Equal(ExitCodes.NotADocx, report.ExitCode);
	}

	[Fact]
	public void Run_WithUppercaseExtension_IsAccepted()
	{
		var report = Preflight.Run(Docx("Livre.DOCX"), Stages.Check, PathWithoutTypst());

		Assert.True(report.Ok);
		Assert.NotNull(report.Layout);
	}

	[Fact]
	public void Run_WithPaperSourcesAndNoTypst_ReturnsMissingDependency()
	{
		Touch("Papier", "Livre-broché.typ");

		var report = Preflight.Run(Docx(), Stages.All, PathWithoutTypst());

		Assert.Equal(ExitCodes.MissingDependency, report.ExitCode);
		Assert.Contains("typst", report.Errors[0].Message);
		Assert.Null(report.TypstPath);
	}

	[Fact]
	public void Run_WithPaperSourcesAndTypst_ReturnsItsPath()
	{
		Touch("Papier", "Livre-broché.typ");

		var report = Preflight.Run(Docx(), Stages.All, PathWithTypst());

		Assert.True(report.Ok);
		Assert.NotNull(report.TypstPath);
		Assert.True(File.Exists(report.TypstPath));
	}

	[Fact]
	public void Run_WithoutPdfStage_DoesNotRequireTypst()
	{
		Touch("Papier", "Livre-broché.typ");

		var report = Preflight.Run(Docx(), Stages.Check | Stages.Generate | Stages.Epub, PathWithoutTypst());

		Assert.True(report.Ok);
		Assert.Null(report.TypstPath);
	}

	[Fact]
	public void Run_WithoutPaperDirectory_OmitsPdfStageWithNoticeAndDoesNotRequireTypst()
	{
		var report = Preflight.Run(Docx(), Stages.All, PathWithoutTypst());

		Assert.True(report.Ok);
		Assert.Contains(report.Notices, n => n.Contains("Papier") && n.Contains("pdf"));
	}

	[Fact]
	public void Run_WithPaperDirectoryWithoutSources_OmitsPdfStageWithNotice()
	{
		Touch("Papier", "Assets", "common.typ");

		var report = Preflight.Run(Docx(), Stages.All, PathWithoutTypst());

		Assert.True(report.Ok);
		Assert.Empty(report.Layout!.PaperSources);
		Assert.Contains(report.Notices, n => n.Contains("Aucun fichier .typ"));
	}

	[Fact]
	public void Run_WithOnePaigeFile_ExposesItInTheLayout()
	{
		Touch("epub", "template.paige");

		var report = Preflight.Run(Docx(), Stages.Epub, PathWithoutTypst());

		Assert.True(report.Ok);
		Assert.Equal(Path.Combine(_book, "epub", "template.paige"), Assert.Single(report.Layout!.EpubSources));
	}

	[Fact]
	public void Run_WithTwoPaigeFiles_FailsAndListsThem()
	{
		Touch("epub", "a.paige");
		Touch("epub", "b.paige");

		var report = Preflight.Run(Docx(), Stages.Epub, PathWithoutTypst());

		Assert.Equal(ExitCodes.PaigeFailed, report.ExitCode);
		Assert.Contains("'a.paige'", report.Errors[0].Message);
		Assert.Contains("'b.paige'", report.Errors[0].Message);
	}

	[Fact]
	public void Run_WithEpubDirectoryWithoutPaigeFile_Fails()
	{
		Touch("epub", "cover.jpg");

		var report = Preflight.Run(Docx(), Stages.Epub, PathWithoutTypst());

		Assert.Equal(ExitCodes.PaigeFailed, report.ExitCode);
	}

	[Fact]
	public void Run_WithoutEpubDirectory_OmitsEpubStageWithNotice()
	{
		var report = Preflight.Run(Docx(), Stages.Epub, PathWithoutTypst());

		Assert.True(report.Ok);
		Assert.Contains(report.Notices, n => n.Contains("epub") && n.Contains("omise"));
	}

	[Fact]
	public void Run_WithoutEpubStage_IgnoresTheNumberOfPaigeFiles()
	{
		Touch("epub", "a.paige");
		Touch("epub", "b.paige");

		var report = Preflight.Run(Docx(), Stages.Check | Stages.Generate, PathWithoutTypst());

		Assert.True(report.Ok);
	}

	[Fact]
	public void Run_IgnoresPaigeFilesOutsideEpubDirectory()
	{
		Touch("Livre.paige");
		Touch("epub", "template.paige");

		var report = Preflight.Run(Docx(), Stages.Epub, PathWithoutTypst());

		Assert.True(report.Ok);
	}

	[Fact]
	public void Run_WithSeveralProblems_ReportsAllAndUsesTheFirstCode()
	{
		Touch("Papier", "Livre-broché.typ");
		Touch("epub", "a.paige");
		Touch("epub", "b.paige");

		var report = Preflight.Run(Docx(), Stages.All, PathWithoutTypst());

		Assert.Equal(2, report.Errors.Count);
		Assert.Equal(ExitCodes.MissingDependency, report.ExitCode);
		Assert.Equal(ExitCodes.PaigeFailed, report.Errors[1].ExitCode);
	}

	[Fact]
	public void Run_DoesNotWriteAnything()
	{
		Touch("Papier", "Livre-broché.typ");
		Touch("epub", "template.paige");
		var docx = Docx();
		var before = Directory.GetFileSystemEntries(_book, "*", SearchOption.AllDirectories).Order().ToArray();

		Preflight.Run(docx, Stages.All, PathWithTypst());

		Assert.Equal(before, Directory.GetFileSystemEntries(_book, "*", SearchOption.AllDirectories).Order().ToArray());
	}
}
