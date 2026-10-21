using System.IO.Compression;
using System.Xml.Linq;

namespace Johannes;

/// <summary>
/// Accès commun au XML d'un document Word (<c>.docx</c>) : espace de noms et lecture des parties.
/// </summary>
public static class WordXml
{
	public static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

	public const string MainDocumentPart = "word/document.xml";

	/// <summary>Charge une partie XML d'une archive, en conservant les espaces significatifs.</summary>
	public static XDocument Load(ZipArchive archive, string partName)
	{
		var entry = archive.GetEntry(partName)
			?? throw new InvalidDataException($"Partie '{partName}' introuvable : ce fichier n'est pas un document Word valide.");
		using var stream = entry.Open();
		return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
	}

	/// <summary>Charge le corps du document (<c>word/document.xml</c>).</summary>
	public static XDocument LoadMainDocument(string docxPath)
	{
		using var archive = ZipFile.OpenRead(docxPath);
		return Load(archive, MainDocumentPart);
	}

	/// <summary>
	/// Nom local d'un élément de l'espace de noms Word ; pour tout autre espace de noms, le nom complet
	/// (<c>{espace}nom</c>), afin qu'un élément étranger homonyme ne soit jamais pris pour un élément Word.
	/// </summary>
	public static string Name(XElement element) =>
		element.Name.Namespace == W ? element.Name.LocalName : element.Name.ToString();

	/// <summary>
	/// Valeur d'une propriété booléenne Word (<c>w:b</c>, <c>w:i</c>...) : vraie sans attribut <c>w:val</c>,
	/// fausse pour <c>0</c>, <c>false</c> ou <c>off</c>.
	/// </summary>
	public static bool IsOn(XElement property)
	{
		var value = (string?)property.Attribute(W + "val");
		return value is null || value is not ("0" or "false" or "off");
	}

	/// <summary>Texte brut concaténé des <c>w:t</c> d'un élément.</summary>
	public static string PlainText(XElement element) =>
		string.Concat(element.Descendants(W + "t").Select(t => t.Value));
}
