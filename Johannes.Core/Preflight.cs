namespace Johannes;

/// <summary>Problème bloquant relevé avant le début de la chaîne.</summary>
public sealed record PreflightIssue(int ExitCode, string Message);

/// <summary>Résultat des contrôles préalables.</summary>
/// <param name="Layout">Organisation du livre ; <c>null</c> si le document est inutilisable.</param>
/// <param name="TypstPath">Chemin de <c>typst</c> trouvé dans le <c>PATH</c>, si la recherche a eu lieu.</param>
public sealed record PreflightReport(
	BookLayout? Layout,
	string? TypstPath,
	IReadOnlyList<PreflightIssue> Errors,
	IReadOnlyList<string> Notices)
{
	public bool Ok => Errors.Count == 0;

	/// <summary>Code de sortie du premier problème, ou 0.</summary>
	public int ExitCode => Errors.Count == 0 ? ExitCodes.Success : Errors[0].ExitCode;
}

/// <summary>
/// Contrôles faits avant la première étape, pour échouer vite et sans rien écrire.
/// </summary>
public static class Preflight
{
	public const string TypstProgram = "typst";

	/// <param name="docxPath">Document source.</param>
	/// <param name="requested">Étapes demandées : seuls leurs prérequis sont contrôlés.</param>
	/// <param name="pathVariable">Valeur du <c>PATH</c> (celle de l'environnement par défaut).</param>
	public static PreflightReport Run(string docxPath, Stages requested, string? pathVariable = null)
	{
		var errors = new List<PreflightIssue>();
		var notices = new List<string>();

		var fullPath = Path.GetFullPath(docxPath);
		BookLayout? layout = null;

		if (!File.Exists(fullPath))
		{
			errors.Add(new(ExitCodes.DocumentNotFound, $"Le fichier '{fullPath}' est introuvable."));
		}
		else if (!string.Equals(Path.GetExtension(fullPath), ".docx", StringComparison.OrdinalIgnoreCase))
		{
			errors.Add(new(ExitCodes.NotADocx, $"Le fichier '{fullPath}' n'a pas l'extension .docx."));
		}
		else
		{
			layout = BookLayout.Discover(fullPath);
		}

		string? typstPath = null;
		if (requested.HasFlag(Stages.Pdf) && (layout is null || layout.PaperSources.Count > 0))
		{
			typstPath = ExecutableLocator.Find(TypstProgram, pathVariable);
			if (typstPath is null)
			{
				errors.Add(new(ExitCodes.MissingDependency, "Le programme 'typst' est introuvable dans le PATH (nécessaire à la fabrication des PDF)."));
			}
		}

		if (layout is not null)
		{
			CheckPaper(layout, requested, notices);
			CheckEpub(layout, requested, errors, notices);
		}

		return new PreflightReport(layout, typstPath, errors, notices);
	}

	private static void CheckPaper(BookLayout layout, Stages requested, List<string> notices)
	{
		if (requested.HasFlag(Stages.Pdf) && layout.PdfOmissionReason is { } reason)
		{
			notices.Add($"{Capitalize(reason)} : l'étape pdf est omise.");
		}
	}

	private static void CheckEpub(BookLayout layout, Stages requested, List<PreflightIssue> errors, List<string> notices)
	{
		if (!requested.HasFlag(Stages.Epub))
		{
			return;
		}

		if (layout.EpubOmissionReason is { } reason)
		{
			notices.Add($"{Capitalize(reason)} : l'étape epub est omise.");
			return;
		}

		switch (layout.EpubSources.Count)
		{
			case 1:
				return;
			case 0:
				errors.Add(new(ExitCodes.PaigeFailed, $"Aucun fichier .paige dans '{layout.EpubDirectory}' : il en faut exactement un."));
				break;
			default:
				var names = string.Join(", ", layout.EpubSources.Select(f => $"'{Path.GetFileName(f)}'"));
				errors.Add(new(ExitCodes.PaigeFailed, $"{layout.EpubSources.Count} fichiers .paige dans '{layout.EpubDirectory}' ({names}) : il en faut exactement un."));
				break;
		}
	}

	private static string Capitalize(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
