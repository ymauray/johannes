using Xunit;

namespace Johannes.Tests;

public sealed class ConfigurationTests : IDisposable
{
	private readonly string _root = Directory.CreateTempSubdirectory("johannes-config-").FullName;

	public void Dispose() => Directory.Delete(_root, recursive: true);

	private string LocalPath => Path.Combine(_root, "livre", ".johannes");

	private string UserPath => Path.Combine(_root, "home", "johannes", "config");

	private Configuration New() => new(LocalPath, UserPath);

	[Fact]
	public void ConfigFile_RoundTripsAndKeepsCommentsAndUnknownLines()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(LocalPath)!);
		File.WriteAllLines(LocalPath, ["# réglages du livre", "", "paper-directory = Print", "n'importe quoi"]);

		var file = ConfigFile.Load(LocalPath);
		file.Set("paper-directory", "Papier2");
		file.Set("epub-directory", "ebook");

		Assert.Equal(["# réglages du livre", "", "paper-directory = Papier2", "n'importe quoi", "epub-directory = ebook"], File.ReadAllLines(LocalPath));
	}

	[Fact]
	public void ConfigFile_Unset_RemovesTheKeyOnly()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(LocalPath)!);
		File.WriteAllLines(LocalPath, ["# note", "paper-directory = Print", "epub-directory = ebook"]);
		var file = ConfigFile.Load(LocalPath);

		Assert.True(file.Unset("paper-directory"));
		Assert.False(file.Unset("paper-directory"));

		Assert.Equal(["# note", "epub-directory = ebook"], File.ReadAllLines(LocalPath));
	}

	[Fact]
	public void ConfigFile_LastValueOfARepeatedKeyWins()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(LocalPath)!);
		File.WriteAllLines(LocalPath, ["paper-directory = A", "paper-directory = B"]);

		Assert.Equal("B", ConfigFile.Load(LocalPath).Get("paper-directory"));
	}

	[Fact]
	public void ConfigFile_MissingFile_IsEmptyAndNotCreatedByReading()
	{
		var file = ConfigFile.Load(LocalPath);

		Assert.Null(file.Get("paper-directory"));
		Assert.False(File.Exists(LocalPath));
	}

	[Fact]
	public void Effective_WithoutAnyFile_UsesDefaults()
	{
		var entry = New().Effective("paper-directory");

		Assert.Equal(new ConfigEntry("paper-directory", "Papier", ConfigOrigin.Default), entry);
		Assert.Null(New().Effective("typst-path").Value);
	}

	[Fact]
	public void Effective_PrecedenceIsDefaultUserLocalCommandLine()
	{
		var configuration = New();
		configuration.Set(user: true, "paper-directory", "DepuisUtilisateur");
		Assert.Equal(ConfigOrigin.User, configuration.Effective("paper-directory").Origin);

		configuration.Set(user: false, "paper-directory", "DepuisLivre");
		Assert.Equal("DepuisLivre", configuration.Effective("paper-directory").Value);
		Assert.Equal(ConfigOrigin.Local, configuration.Effective("paper-directory").Origin);

		var overrides = new Dictionary<string, string> { ["paper-directory"] = "DepuisOption" };
		Assert.Equal(new ConfigEntry("paper-directory", "DepuisOption", ConfigOrigin.CommandLine), configuration.Effective("paper-directory", overrides));
	}

	[Fact]
	public void Set_IsPersistedAndReadByANewInstance()
	{
		New().Set(user: false, "epub-directory", "ebook");

		Assert.Equal("ebook", New().Effective("epub-directory").Value);
		Assert.False(File.Exists(UserPath));
	}

	[Fact]
	public void Resolve_MergesSourcesIntoSettings()
	{
		var configuration = New();
		configuration.Set(user: true, "typst-path", "C:\\outils\\typst.exe");
		configuration.Set(user: false, "paper-directory", "Print");

		var settings = configuration.Resolve(new Dictionary<string, string> { ["epub-directory"] = "ebook" }, out var errors);

		Assert.Empty(errors);
		Assert.Equal(new Settings("Print", "ebook", "C:\\outils\\typst.exe"), settings);
	}

	[Fact]
	public void Resolve_ReportsUnknownKeysAndInvalidValuesInFiles()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(LocalPath)!);
		File.WriteAllLines(LocalPath, ["papier = Print", "epub-directory = ../dehors"]);

		New().Resolve(null, out var errors);

		Assert.Equal(2, errors.Count);
		Assert.Contains(errors, e => e.Contains("clé inconnue 'papier'"));
		Assert.Contains(errors, e => e.Contains("epub-directory") && e.Contains("relatif"));
	}

	[Theory]
	[InlineData("paper-directory", "Print", true)]
	[InlineData("paper-directory", "Sorties/Papier", true)]
	[InlineData("paper-directory", "..", false)]
	[InlineData("paper-directory", "../Papier", false)]
	[InlineData("epub-directory", "C:\\absolu", false)]
	[InlineData("epub-directory", "/absolu", false)]
	[InlineData("epub-directory", "  ", false)]
	[InlineData("typst-path", "C:\\outils\\typst.exe", true)]
	[InlineData("couleur", "bleu", false)]
	public void TryValidate_AcceptsOnlyUsableValues(string key, string value, bool expected)
	{
		Assert.Equal(expected, ConfigKeys.TryValidate(key, value, out _));
	}

	[Fact]
	public void Discover_UsesTheConfiguredDirectoryNames()
	{
		var book = Directory.CreateDirectory(Path.Combine(_root, "livre")).FullName;
		Directory.CreateDirectory(Path.Combine(book, "Print"));
		File.WriteAllText(Path.Combine(book, "Print", "A.typ"), string.Empty);
		Directory.CreateDirectory(Path.Combine(book, "ebook"));
		File.WriteAllText(Path.Combine(book, "ebook", "livre.paige"), string.Empty);

		var layout = BookLayout.Discover(Path.Combine(book, "Livre.docx"), new Settings("Print", "ebook"));

		Assert.Single(layout.PaperSources);
		Assert.Single(layout.EpubSources);
		Assert.Equal("Print", layout.PaperName);
		Assert.Contains("Print", BookLayout.Discover(Path.Combine(_root, "Autre.docx"), new Settings("Print", "ebook")).PdfOmissionReason);
	}
}
