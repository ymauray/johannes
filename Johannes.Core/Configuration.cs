namespace Johannes;

/// <summary>Réglages effectifs de johannes, après fusion des sources de configuration.</summary>
/// <param name="PaperDirectory">Dossier des reliures Typst, relatif au dossier du <c>.docx</c>.</param>
/// <param name="EpubDirectory">Dossier du projet EPUB, relatif au dossier du <c>.docx</c>.</param>
/// <param name="TypstPath">Chemin de <c>typst</c> ; <c>null</c> pour le chercher dans le <c>PATH</c>.</param>
public sealed record Settings(
	string PaperDirectory = BookLayout.PaperDirectoryName,
	string EpubDirectory = BookLayout.EpubDirectoryName,
	string? TypstPath = null)
{
	public static readonly Settings Default = new();
}

/// <summary>Clés de configuration connues.</summary>
public static class ConfigKeys
{
	public const string PaperDirectory = "paper-directory";
	public const string EpubDirectory = "epub-directory";
	public const string TypstPath = "typst-path";

	public sealed record KeyInfo(string Name, string? Default, string Description);

	public static readonly IReadOnlyList<KeyInfo> All =
	[
		new(PaperDirectory, BookLayout.PaperDirectoryName, "dossier des reliures Typst (un PDF par .typ), relatif au dossier du .docx"),
		new(EpubDirectory, BookLayout.EpubDirectoryName, "dossier du projet EPUB (un seul .paige), relatif au dossier du .docx"),
		new(TypstPath, null, "chemin complet de typst, à la place de la recherche dans le PATH"),
	];

	public static bool IsKnown(string key) => All.Any(k => k.Name == key);

	/// <summary>Vérifie qu'une valeur convient à une clé ; <paramref name="error"/> explique sinon pourquoi.</summary>
	public static bool TryValidate(string key, string value, out string? error)
	{
		error = null;

		if (!IsKnown(key))
		{
			error = $"clé inconnue '{key}' (clés connues : {string.Join(", ", All.Select(k => k.Name))}).";
			return false;
		}

		if (string.IsNullOrWhiteSpace(value))
		{
			error = $"la valeur de '{key}' ne peut pas être vide.";
			return false;
		}

		if (key is PaperDirectory or EpubDirectory
			&& (Path.IsPathRooted(value) || value.Split('/', '\\').Contains("..")))
		{
			error = $"'{key}' doit être un chemin relatif au dossier du livre, sans '..' (reçu : '{value}').";
			return false;
		}

		return true;
	}

	/// <summary>Applique une valeur validée à des réglages.</summary>
	public static Settings Apply(Settings settings, string key, string value) => key switch
	{
		PaperDirectory => settings with { PaperDirectory = value },
		EpubDirectory => settings with { EpubDirectory = value },
		TypstPath => settings with { TypstPath = value },
		_ => settings
	};
}

/// <summary>D'où vient une valeur.</summary>
public enum ConfigOrigin
{
	Default,
	User,
	Local,
	CommandLine
}

/// <summary>Une valeur effective et sa provenance.</summary>
public sealed record ConfigEntry(string Key, string? Value, ConfigOrigin Origin);

/// <summary>
/// Configuration façon git. Du plus faible au plus fort : valeurs par défaut, fichier de l'utilisateur,
/// fichier du livre (<c>.johannes</c> à côté du <c>.docx</c>), options de la ligne de commande.
/// </summary>
public sealed class Configuration
{
	public const string LocalFileName = ".johannes";

	private readonly ConfigFile _local;
	private readonly ConfigFile _user;

	/// <param name="localPath">Fichier du livre.</param>
	/// <param name="userPath">Fichier de l'utilisateur.</param>
	public Configuration(string localPath, string userPath)
	{
		_local = ConfigFile.Load(localPath);
		_user = ConfigFile.Load(userPath);
	}

	/// <summary>Fichier de configuration de l'utilisateur (<c>~/.config/johannes/config</c> ou <c>%APPDATA%\johannes\config</c>).</summary>
	public static string DefaultUserPath() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "johannes", "config");

	public static string LocalPathFor(string bookDirectory) => Path.Combine(bookDirectory, LocalFileName);

	/// <summary>Valeurs effectives de toutes les clés connues, avec leur provenance.</summary>
	public IReadOnlyList<ConfigEntry> Entries(IReadOnlyDictionary<string, string>? overrides = null) =>
		ConfigKeys.All.Select(k => Effective(k.Name, overrides)).ToList();

	public ConfigEntry Effective(string key, IReadOnlyDictionary<string, string>? overrides = null)
	{
		if (overrides is not null && overrides.TryGetValue(key, out var forced))
		{
			return new(key, forced, ConfigOrigin.CommandLine);
		}

		if (_local.Get(key) is { } local)
		{
			return new(key, local, ConfigOrigin.Local);
		}

		if (_user.Get(key) is { } user)
		{
			return new(key, user, ConfigOrigin.User);
		}

		return new(key, ConfigKeys.All.FirstOrDefault(k => k.Name == key)?.Default, ConfigOrigin.Default);
	}

	/// <summary>
	/// Réglages effectifs. Toute valeur invalide ou clé inconnue (dans un fichier ou en option) est signalée
	/// dans <paramref name="errors"/> plutôt qu'ignorée en silence.
	/// </summary>
	public Settings Resolve(IReadOnlyDictionary<string, string>? overrides, out IReadOnlyList<string> errors)
	{
		var problems = new List<string>();
		var settings = Settings.Default;

		var sources = new (string Label, IEnumerable<KeyValuePair<string, string>> Pairs)[]
		{
			("fichier de l'utilisateur", _user.Pairs),
			($"fichier '{LocalFileName}'", _local.Pairs),
			("ligne de commande", overrides ?? new Dictionary<string, string>())
		};

		foreach (var (label, pairs) in sources)
		{
			foreach (var (key, value) in pairs)
			{
				if (!ConfigKeys.TryValidate(key, value, out var error))
				{
					problems.Add($"{label} : {error}");
				}
			}
		}

		foreach (var entry in Entries(overrides).Where(e => e.Value is not null))
		{
			if (ConfigKeys.TryValidate(entry.Key, entry.Value!, out _))
			{
				settings = ConfigKeys.Apply(settings, entry.Key, entry.Value!);
			}
		}

		errors = problems;
		return settings;
	}

	/// <summary>Écrit une valeur dans l'un des deux fichiers.</summary>
	public void Set(bool user, string key, string value) => (user ? _user : _local).Set(key, value);

	/// <summary>Retire une clé de l'un des deux fichiers ; <c>false</c> si elle n'y était pas.</summary>
	public bool Unset(bool user, string key) => (user ? _user : _local).Unset(key);

	public string PathOf(bool user) => (user ? _user : _local).Path;
}
