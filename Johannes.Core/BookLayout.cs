namespace Johannes;

/// <summary>
/// Organisation d'un livre autour de son <c>.docx</c>, reconnue par convention :
/// <c>Papier/*.typ</c> pour les PDF, <c>epub/</c> pour l'EPUB.
/// </summary>
/// <param name="Directory">Dossier du <c>.docx</c> : tous les chemins en dépendent.</param>
/// <param name="PaperDirectory">Dossier <c>Papier/</c>, s'il existe.</param>
/// <param name="PaperSources">Fichiers <c>Papier/*.typ</c> (un par reliure), triés.</param>
/// <param name="EpubDirectory">Dossier <c>epub/</c>, s'il existe.</param>
/// <param name="EpubSources">Fichiers <c>.paige</c> directement dans <c>epub/</c>, triés.</param>
public sealed record BookLayout(
	string Directory,
	string? PaperDirectory,
	IReadOnlyList<string> PaperSources,
	string? EpubDirectory,
	IReadOnlyList<string> EpubSources)
{
	public const string PaperDirectoryName = "Papier";
	public const string EpubDirectoryName = "epub";

	/// <summary>Raison pour laquelle l'étape pdf est sans objet, ou <c>null</c> s'il y a des reliures à fabriquer.</summary>
	public string? PdfOmissionReason =>
		PaperDirectory is null ? $"pas de dossier '{PaperDirectoryName}'"
		: PaperSources.Count == 0 ? $"aucun fichier .typ dans '{PaperDirectoryName}'"
		: null;

	/// <summary>Raison pour laquelle l'étape epub est sans objet, ou <c>null</c> s'il y a un projet EPUB.</summary>
	public string? EpubOmissionReason =>
		EpubDirectory is null ? $"pas de dossier '{EpubDirectoryName}'" : null;

	public static BookLayout Discover(string docxPath)
	{
		var directory = Path.GetDirectoryName(Path.GetFullPath(docxPath))!;

		var paper = Path.Combine(directory, PaperDirectoryName);
		var epub = Path.Combine(directory, EpubDirectoryName);
		var hasPaper = System.IO.Directory.Exists(paper);
		var hasEpub = System.IO.Directory.Exists(epub);

		return new BookLayout(
			directory,
			hasPaper ? paper : null,
			hasPaper ? Sorted(paper, "*.typ") : [],
			hasEpub ? epub : null,
			hasEpub ? Sorted(epub, "*.paige") : []);
	}

	private static string[] Sorted(string directory, string pattern)
	{
		var files = System.IO.Directory.GetFiles(directory, pattern);
		Array.Sort(files, StringComparer.Ordinal);
		return files;
	}
}
