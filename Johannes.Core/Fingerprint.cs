using System.Security.Cryptography;
using System.Text;

namespace Johannes;

/// <summary>Empreinte SHA-256 d'un ensemble de fichiers et de dossiers, pour savoir si une étape a des entrées neuves.</summary>
public static class Fingerprint
{
	/// <summary>
	/// Empreinte du contenu et des chemins (relatifs à <paramref name="baseDirectory"/>) des entrées.
	/// Un dossier compte avec tous ses fichiers, récursivement ; une entrée absente compte comme telle.
	/// </summary>
	/// <param name="baseDirectory">Dossier de référence des chemins relatifs.</param>
	/// <param name="inputs">Fichiers ou dossiers.</param>
	/// <param name="salt">Texte supplémentaire qui entre dans l'empreinte (options, version…).</param>
	/// <param name="exclude">Fichiers à ignorer (chemin complet), par exemple le résultat de l'étape.</param>
	public static string Compute(string baseDirectory, IEnumerable<string> inputs, string salt = "", Func<string, bool>? exclude = null)
	{
		var entries = new SortedDictionary<string, string>(StringComparer.Ordinal);

		foreach (var input in inputs)
		{
			var full = Path.GetFullPath(input, baseDirectory);

			if (Directory.Exists(full))
			{
				foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
				{
					Add(entries, baseDirectory, file, exclude);
				}
			}
			else if (File.Exists(full))
			{
				Add(entries, baseDirectory, full, exclude);
			}
			else
			{
				entries[Relative(baseDirectory, full)] = "absent";
			}
		}

		var text = new StringBuilder(salt).Append('\n');
		foreach (var (path, hash) in entries)
		{
			text.Append(path).Append('\0').Append(hash).Append('\n');
		}

		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
	}

	private static void Add(SortedDictionary<string, string> entries, string baseDirectory, string file, Func<string, bool>? exclude)
	{
		if (exclude?.Invoke(file) == true)
		{
			return;
		}

		using var stream = File.OpenRead(file);
		entries[Relative(baseDirectory, file)] = Convert.ToHexString(SHA256.HashData(stream));
	}

	private static string Relative(string baseDirectory, string file) =>
		Path.GetRelativePath(baseDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
}
