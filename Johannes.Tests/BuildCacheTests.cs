using Xunit;

namespace Johannes.Tests;

public sealed class BuildCacheTests : IDisposable
{
	private readonly string _book = Directory.CreateTempSubdirectory("johannes-cache-").FullName;

	public void Dispose() => Directory.Delete(_book, recursive: true);

	[Fact]
	public void IsFresh_BeforeAnyRecord_IsFalse()
	{
		Assert.False(new BuildCache(_book, "v1").IsFresh("check", "abc"));
	}

	[Fact]
	public void IsFresh_AfterRecord_IsTrueForSameFingerprintOnly()
	{
		var cache = new BuildCache(_book, "v1");
		cache.Record("check", "abc");

		Assert.True(cache.IsFresh("check", "abc"));
		Assert.False(cache.IsFresh("check", "autre"));
		Assert.False(cache.IsFresh("generate", "abc"));
	}

	[Fact]
	public void Record_IsPersistedAcrossInstances()
	{
		new BuildCache(_book, "v1").Record("epub", "abc");

		Assert.True(new BuildCache(_book, "v1").IsFresh("epub", "abc"));
	}

	[Fact]
	public void IsFresh_WithAnotherJohannesVersion_IsFalse()
	{
		new BuildCache(_book, "v1").Record("epub", "abc");

		Assert.False(new BuildCache(_book, "v2").IsFresh("epub", "abc"));
	}

	[Fact]
	public void IgnoreStored_NeverFresh_ButStillRecords()
	{
		new BuildCache(_book, "v1").Record("epub", "abc");

		var rebuild = new BuildCache(_book, "v1", ignoreStored: true);
		Assert.False(rebuild.IsFresh("epub", "abc"));

		rebuild.Record("epub", "def");
		Assert.True(new BuildCache(_book, "v1").IsFresh("epub", "def"));
	}

	[Fact]
	public void Load_WithCorruptedFile_StartsEmpty()
	{
		Directory.CreateDirectory(Path.Combine(_book, BuildCache.DirectoryName));
		File.WriteAllText(Path.Combine(_book, BuildCache.DirectoryName, "state.json"), "{ pas du json");

		Assert.False(new BuildCache(_book, "v1").IsFresh("check", "abc"));
	}

	[Fact]
	public void Record_CreatesDirectoryIgnoredByGit_AndNothingBeforehand()
	{
		var cache = new BuildCache(_book, "v1");
		Assert.False(Directory.Exists(Path.Combine(_book, BuildCache.DirectoryName)));

		cache.Record("check", "abc");

		Assert.Equal("*", File.ReadAllText(Path.Combine(_book, BuildCache.DirectoryName, ".gitignore")).Trim());
	}
}
