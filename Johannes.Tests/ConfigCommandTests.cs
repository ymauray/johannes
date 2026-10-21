using Xunit;

namespace Johannes.Tests;

public sealed class ConfigCommandTests : IDisposable
{
	private readonly string _root = Directory.CreateTempSubdirectory("johannes-configcmd-").FullName;

	public void Dispose() => Directory.Delete(_root, recursive: true);

	private Configuration NewConfiguration() => new(Path.Combine(_root, ".johannes"), Path.Combine(_root, "home", "config"));

	private (int Code, string Out, string Error) Run(ConfigRequest request)
	{
		var output = new StringWriter();
		var error = new StringWriter();
		var code = ConfigCommand.Run(request, NewConfiguration(), output, error);
		return (code, output.ToString().ReplaceLineEndings("\n"), error.ToString());
	}

	private static ConfigRequest Read(string key) => new(key, null, false, false, false);

	[Fact]
	public void Write_ThenRead_ReturnsTheValue()
	{
		Assert.Equal(0, Run(new ConfigRequest("paper-directory", "Print", false, false, false)).Code);

		var (code, output, _) = Run(Read("paper-directory"));

		Assert.Equal(0, code);
		Assert.Equal("Print\n", output);
		Assert.True(File.Exists(Path.Combine(_root, ".johannes")));
	}

	[Fact]
	public void Write_WithUserScope_GoesToTheUserFile()
	{
		Run(new ConfigRequest("epub-directory", "ebook", false, false, true));

		Assert.True(File.Exists(Path.Combine(_root, "home", "config")));
		Assert.False(File.Exists(Path.Combine(_root, ".johannes")));
		Assert.Equal("ebook\n", Run(Read("epub-directory")).Out);
	}

	[Fact]
	public void Read_OfADefault_PrintsIt()
	{
		Assert.Equal("Papier\n", Run(Read("paper-directory")).Out);
	}

	[Fact]
	public void Read_OfAnUnsetKeyWithoutDefault_PrintsNothingAndFails()
	{
		var (code, output, _) = Run(Read("typst-path"));

		Assert.Equal(ExitCodes.InvalidArguments, code);
		Assert.Equal(string.Empty, output);
	}

	[Fact]
	public void List_ShowsEveryKeyWithItsOrigin()
	{
		Run(new ConfigRequest("paper-directory", "Print", false, false, false));

		var (code, output, _) = Run(new ConfigRequest(null, null, true, false, false));

		Assert.Equal(0, code);
		Assert.Contains("paper-directory = Print (livre)", output);
		Assert.Contains("epub-directory = epub (défaut)", output);
		Assert.Contains("typst-path", output);
	}

	[Fact]
	public void Unset_RemovesTheKey_AndReportsWhenAbsent()
	{
		Run(new ConfigRequest("paper-directory", "Print", false, false, false));

		Assert.Equal(0, Run(new ConfigRequest("paper-directory", null, false, true, false)).Code);
		Assert.Equal("Papier\n", Run(Read("paper-directory")).Out);
		Assert.Equal(ExitCodes.InvalidArguments, Run(new ConfigRequest("paper-directory", null, false, true, false)).Code);
	}

	[Theory]
	[InlineData("couleur", "bleu")]
	[InlineData("paper-directory", "../dehors")]
	public void Write_OfAnInvalidSetting_FailsAndWritesNothing(string key, string value)
	{
		var (code, _, error) = Run(new ConfigRequest(key, value, false, false, false));

		Assert.Equal(ExitCodes.InvalidArguments, code);
		Assert.NotEmpty(error);
		Assert.False(File.Exists(Path.Combine(_root, ".johannes")));
	}

	[Fact]
	public void WithoutKeyNorList_ListsTheKnownKeys()
	{
		var (code, _, error) = Run(new ConfigRequest(null, null, false, false, false));

		Assert.Equal(ExitCodes.InvalidArguments, code);
		Assert.Contains("paper-directory", error);
	}

	[Fact]
	public void List_WithAKey_IsRejected()
	{
		Assert.Equal(ExitCodes.InvalidArguments, Run(new ConfigRequest("paper-directory", null, true, false, false)).Code);
	}
}
