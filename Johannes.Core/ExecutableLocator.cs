namespace Johannes;

/// <summary>Recherche d'un programme dans le <c>PATH</c>.</summary>
public static class ExecutableLocator
{
	private const string DefaultWindowsExtensions = ".COM;.EXE;.BAT;.CMD";

	/// <summary>
	/// Chemin complet du programme <paramref name="name"/>, ou <c>null</c> s'il n'est pas dans le <c>PATH</c>.
	/// </summary>
	/// <param name="name">Nom du programme, sans extension.</param>
	/// <param name="path">Valeur du <c>PATH</c> ; celle de l'environnement par défaut.</param>
	/// <param name="pathExt">Extensions exécutables (Windows) ; <c>PATHEXT</c> par défaut.</param>
	public static string? Find(string name, string? path = null, string? pathExt = null)
	{
		path ??= Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		var extensions = OperatingSystem.IsWindows()
			? (pathExt ?? Environment.GetEnvironmentVariable("PATHEXT") ?? DefaultWindowsExtensions)
				.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			: [];

		foreach (var rawDirectory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var directory = rawDirectory.Trim('"');
			if (!Directory.Exists(directory))
			{
				continue;
			}

			foreach (var candidate in Candidates(Path.Combine(directory, name), extensions))
			{
				if (IsExecutable(candidate))
				{
					return candidate;
				}
			}
		}

		return null;
	}

	private static IEnumerable<string> Candidates(string basePath, string[] windowsExtensions)
	{
		if (windowsExtensions.Length == 0)
		{
			yield return basePath;
			yield break;
		}

		foreach (var extension in windowsExtensions)
		{
			yield return basePath + extension;
		}
	}

	private static bool IsExecutable(string file)
	{
		if (!File.Exists(file))
		{
			return false;
		}

		return OperatingSystem.IsWindows()
			|| (File.GetUnixFileMode(file) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
	}
}
