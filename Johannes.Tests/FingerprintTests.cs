using Xunit;

namespace Johannes.Tests;

public sealed class FingerprintTests : IDisposable
{
	private readonly string _root = Directory.CreateTempSubdirectory("johannes-fp-").FullName;

	public void Dispose() => Directory.Delete(_root, recursive: true);

	private string Write(string relative, string content)
	{
		var path = Path.GetFullPath(Path.Combine(_root, relative));
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	[Fact]
	public void Compute_IsStableForUnchangedInputs()
	{
		var file = Write("a.txt", "un");

		Assert.Equal(Fingerprint.Compute(_root, [file]), Fingerprint.Compute(_root, [file]));
	}

	[Fact]
	public void Compute_ChangesWhenContentChanges()
	{
		var file = Write("a.txt", "un");
		var before = Fingerprint.Compute(_root, [file]);

		File.WriteAllText(file, "deux");

		Assert.NotEqual(before, Fingerprint.Compute(_root, [file]));
	}

	[Fact]
	public void Compute_CoversDirectoriesRecursively()
	{
		Write("Assets/sub/image.txt", "v1");
		var before = Fingerprint.Compute(_root, [Path.Combine(_root, "Assets")]);

		Write("Assets/sub/image.txt", "v2");

		Assert.NotEqual(before, Fingerprint.Compute(_root, [Path.Combine(_root, "Assets")]));
	}

	[Fact]
	public void Compute_ChangesWhenAFileIsAddedToADirectory()
	{
		Write("Assets/a.txt", "x");
		var before = Fingerprint.Compute(_root, [Path.Combine(_root, "Assets")]);

		Write("Assets/b.txt", "x");

		Assert.NotEqual(before, Fingerprint.Compute(_root, [Path.Combine(_root, "Assets")]));
	}

	[Fact]
	public void Compute_DistinguishesAbsentFromEmptyFile()
	{
		var path = Path.Combine(_root, "a.txt");
		var absent = Fingerprint.Compute(_root, [path]);

		File.WriteAllText(path, string.Empty);

		Assert.NotEqual(absent, Fingerprint.Compute(_root, [path]));
	}

	[Fact]
	public void Compute_IgnoresExcludedFiles()
	{
		Write("epub/a.xhtml", "x");
		var output = Write("epub/livre.epub", "v1");
		var epub = Path.Combine(_root, "epub");
		var before = Fingerprint.Compute(_root, [epub], exclude: f => f == output);

		File.WriteAllText(output, "v2");

		Assert.Equal(before, Fingerprint.Compute(_root, [epub], exclude: f => f == output));
	}

	[Fact]
	public void Compute_TakesSaltIntoAccount()
	{
		var file = Write("a.txt", "un");

		Assert.NotEqual(Fingerprint.Compute(_root, [file], "typst=True"), Fingerprint.Compute(_root, [file], "typst=False"));
	}
}
