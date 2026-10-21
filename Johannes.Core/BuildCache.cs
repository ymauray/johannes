using System.Reflection;
using System.Text.Json;

namespace Johannes;

/// <summary>
/// Mémoire des étapes déjà réussies, dans <c>.build/</c> à côté du <c>.docx</c> : pour chaque unité de travail,
/// l'empreinte de ses entrées au moment du dernier succès. Elle ne sert qu'à gagner du temps : toute
/// anomalie (fichier absent, illisible, autre version de johannes) revient à tout refaire.
/// </summary>
public sealed class BuildCache
{
	public const string DirectoryName = ".build";
	private const string FileName = "state.json";

	private readonly string _directory;
	private readonly string _version;
	private readonly bool _ignoreStored;
	private readonly object _lock = new();
	private Dictionary<string, string> _units;

	/// <param name="bookDirectory">Dossier du <c>.docx</c>.</param>
	/// <param name="version">Identifie la version de johannes ; une autre version invalide tout.</param>
	/// <param name="ignoreStored"><c>true</c> pour <c>--rebuild</c> : rien n'est jamais à jour, mais les succès sont enregistrés.</param>
	public BuildCache(string bookDirectory, string version, bool ignoreStored = false)
	{
		_directory = Path.Combine(bookDirectory, DirectoryName);
		_version = version;
		_ignoreStored = ignoreStored;
		_units = Load();
	}

	/// <summary>Identifie une version de johannes d'après le contenu des assemblies données.</summary>
	public static string VersionOf(params Assembly[] assemblies) =>
		string.Join(';', assemblies.Select(a => $"{a.GetName().Name}:{a.ManifestModule.ModuleVersionId}"));

	/// <summary>Vrai si l'unité a déjà réussi avec exactement cette empreinte.</summary>
	public bool IsFresh(string unit, string fingerprint)
	{
		lock (_lock)
		{
			return !_ignoreStored && _units.TryGetValue(unit, out var stored) && stored == fingerprint;
		}
	}

	/// <summary>Enregistre le succès d'une unité. L'échec de l'écriture est sans conséquence : l'étape sera simplement refaite.</summary>
	public void Record(string unit, string fingerprint)
	{
		lock (_lock)
		{
			_units[unit] = fingerprint;

			try
			{
				Directory.CreateDirectory(_directory);
				var ignoreFile = Path.Combine(_directory, ".gitignore");
				if (!File.Exists(ignoreFile))
				{
					File.WriteAllText(ignoreFile, "*\n");
				}

				File.WriteAllText(
					Path.Combine(_directory, FileName),
					JsonSerializer.Serialize(new State(_version, _units), new JsonSerializerOptions { WriteIndented = true }));
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
	}

	private Dictionary<string, string> Load()
	{
		try
		{
			var state = JsonSerializer.Deserialize<State>(File.ReadAllText(Path.Combine(_directory, FileName)));
			return state?.Version == _version && state.Units is not null
				? new Dictionary<string, string>(state.Units)
				: [];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
		{
			return [];
		}
	}

	private sealed record State(string Version, Dictionary<string, string> Units);
}
