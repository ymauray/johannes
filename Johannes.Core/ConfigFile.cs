namespace Johannes;

/// <summary>
/// Fichier de configuration : une ligne <c>clé = valeur</c> par réglage. Les lignes vides, les commentaires
/// (<c>#</c> ou <c>;</c>) et les lignes illisibles sont conservés tels quels à l'écriture.
/// </summary>
public sealed class ConfigFile
{
	private readonly List<string> _lines;

	private ConfigFile(string path, List<string> lines)
	{
		Path = path;
		_lines = lines;
	}

	public string Path { get; }

	/// <summary>Réglages présents (une fois par clé, dernière valeur), dans l'ordre du fichier.</summary>
	public IEnumerable<KeyValuePair<string, string>> Pairs =>
		_lines.Select(Parse).Where(p => p is not null).Select(p => p!.Value.Key).Distinct().Select(k => KeyValuePair.Create(k, Get(k)!));

	/// <summary>Lit un fichier ; un fichier absent est un fichier vide.</summary>
	public static ConfigFile Load(string path) =>
		new(path, File.Exists(path) ? [.. File.ReadAllLines(path)] : []);

	/// <summary>Valeur d'une clé ; en cas de répétition, la dernière l'emporte.</summary>
	public string? Get(string key) =>
		_lines.Select(Parse).Where(p => p is not null && p.Value.Key == key).Select(p => p!.Value.Value).LastOrDefault();

	public void Set(string key, string value)
	{
		var line = $"{key} = {value}";
		var index = _lines.FindLastIndex(l => Parse(l)?.Key == key);

		if (index >= 0)
		{
			_lines[index] = line;
		}
		else
		{
			_lines.Add(line);
		}

		Save();
	}

	/// <summary>Retire toutes les lignes de la clé ; <c>false</c> si elle n'était pas là.</summary>
	public bool Unset(string key)
	{
		var removed = _lines.RemoveAll(l => Parse(l)?.Key == key) > 0;
		if (removed)
		{
			Save();
		}

		return removed;
	}

	private void Save()
	{
		if (System.IO.Path.GetDirectoryName(Path) is { Length: > 0 } directory)
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllLines(Path, _lines);
	}

	private static (string Key, string Value)? Parse(string line)
	{
		var text = line.Trim();
		if (text.Length == 0 || text[0] is '#' or ';')
		{
			return null;
		}

		var equals = text.IndexOf('=');
		if (equals <= 0)
		{
			return null;
		}

		return (text[..equals].Trim(), text[(equals + 1)..].Trim());
	}
}
