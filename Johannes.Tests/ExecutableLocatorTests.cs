using Xunit;

namespace Johannes.Tests;

public sealed class ExecutableLocatorTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "johannes-tests-" + Guid.NewGuid().ToString("N"));

	public ExecutableLocatorTests() => Directory.CreateDirectory(_root);

	public void Dispose() => Directory.Delete(_root, recursive: true);

	private string Install(string directoryName, string program)
	{
		var directory = Directory.CreateDirectory(Path.Combine(_root, directoryName)).FullName;
		var file = Path.Combine(directory, OperatingSystem.IsWindows() ? program + ".exe" : program);
		File.WriteAllText(file, string.Empty);
		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}
		return file;
	}

	[Fact]
	public void Find_ReturnsTheFullPathOfTheProgram()
	{
		var installed = Install("a", "outil");

		var found = ExecutableLocator.Find("outil", Path.Combine(_root, "a"), ".EXE");

		Assert.Equal(installed, found, ignoreCase: OperatingSystem.IsWindows());
	}

	[Fact]
	public void Find_WithUnknownProgram_ReturnsNull()
	{
		Install("a", "outil");

		Assert.Null(ExecutableLocator.Find("autre", Path.Combine(_root, "a"), ".EXE"));
	}

	[Fact]
	public void Find_SearchesEveryDirectoryOfThePath()
	{
		Directory.CreateDirectory(Path.Combine(_root, "vide"));
		var installed = Install("b", "outil");
		var path = string.Join(Path.PathSeparator, Path.Combine(_root, "absent"), Path.Combine(_root, "vide"), Path.Combine(_root, "b"));

		Assert.Equal(installed, ExecutableLocator.Find("outil", path, ".EXE"), ignoreCase: OperatingSystem.IsWindows());
	}

	[Fact]
	public void Find_AcceptsQuotedDirectories()
	{
		var installed = Install("c", "outil");

		Assert.Equal(installed, ExecutableLocator.Find("outil", $"\"{Path.Combine(_root, "c")}\"", ".EXE"), ignoreCase: OperatingSystem.IsWindows());
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Find_WithEmptyPath_ReturnsNull(string path) =>
		Assert.Null(ExecutableLocator.Find("outil", path, ".EXE"));

	[Fact]
	public void Find_IgnoresFilesWithoutExecutePermissionOnUnix()
	{
		if (OperatingSystem.IsWindows())
		{
			return;
		}

		var installed = Install("d", "outil");
		File.SetUnixFileMode(installed, UnixFileMode.UserRead | UnixFileMode.UserWrite);

		Assert.Null(ExecutableLocator.Find("outil", Path.Combine(_root, "d")));
	}
}
