using System.CommandLine;
using Johannes;

Option<FileInfo?> docxOption = new("--docx", "-d") { Description = "Chemin vers un fichier .docx à traiter." };
Option<bool> withoutTypstOption = new("--without-typst") { Description = "Ne pas exporter au format Typst." };
Option<bool> withoutPaigeOption = new("--without-paige") { Description = "Ne pas exporter au format Paige." };
Option<bool> typstOnlyOption = new("--typst") { Description = "Exporter UNIQUEMENT au format Typst. Aucune autre option autorisée (sauf --docx)." };
Option<bool> paigeOnlyOption = new("--paige") { Description = "Exporter UNIQUEMENT au format Paige. Aucune autre option autorisée (sauf --docx)." };

var rootCommand = new RootCommand("johannes — convertit des fichiers .docx vers les formats Typst et Paige.")
{
	docxOption,
	withoutTypstOption,
	withoutPaigeOption,
	typstOnlyOption,
	paigeOnlyOption
};

ParseResult parseResult = rootCommand.Parse(args);

bool typstOnly = parseResult.GetValue<bool>(typstOnlyOption);
bool paigeOnly = parseResult.GetValue<bool>(paigeOnlyOption);

if (typstOnly || paigeOnly)
{
	if (typstOnly && paigeOnly)
	{
		Console.WriteLine("Erreur : Les options --typst et --paige sont mutuellement exclusives.");
		return;
	}

	// Vérifie si une option interdite a été fournie.
	// --docx est autorisé, mais --without-typst et --without-paige ne le sont pas.
	var forbiddenOptions = parseResult.RootCommandResult.Children
		.OfType<System.CommandLine.Parsing.OptionResult>()
		.Where(o => o.Option.Name != "docx" && o.Option.Name != "typst" && o.Option.Name != "paige");

	if (forbiddenOptions.Any())
	{
		Console.WriteLine($"Erreur : Lors de l'utilisation de {(typstOnly ? "--typst" : "--paige")}, vous ne pouvez pas utiliser --without-typst ou --without-paige.");
		return;
	}
}

FileInfo? fileInfo = parseResult.GetValue<FileInfo?>("--docx");
bool withoutTypst = parseResult.GetValue<bool>(withoutTypstOption);
bool withoutPaige = parseResult.GetValue<bool>(withoutPaigeOption);

if (typstOnly)
{
	withoutTypst = false;
	withoutPaige = true;
}
else if (paigeOnly)
{
	withoutTypst = true;
	withoutPaige = false;
}

List<string> docxFiles = [];

if (fileInfo != null)
{
	if (fileInfo.Exists)
	{
		docxFiles = [fileInfo.FullName];
	}
}
else
{
	docxFiles = [.. Directory.GetFiles(Directory.GetCurrentDirectory(), "*.docx")
		.Where(f => !Path.GetFileName(f).StartsWith("~$"))
		.Select(f => Path.GetFileName(f))];
}
if (docxFiles.Count == 0)
{
	Console.WriteLine("Aucun fichier .docx trouvé dans le répertoire courant.");
	return;
}

foreach (string docxFile in docxFiles)
{
	Console.WriteLine($"Traitement de : {docxFile}");
	var documentParser = new DocumentParser(docxFile);
	var baseFile = Path.GetFileNameWithoutExtension(docxFile);

	if (!withoutTypst)
	{
		Console.WriteLine(" - Export vers Typst...");
		var typstExporter = new TypstExporter(baseFile);
		documentParser.ParseAndExport(typstExporter);
	}

	if (!withoutPaige)
	{
		Console.WriteLine(" - Export vers Paige...");
		var paigeExporter = new PaigeExporter(baseFile);
		documentParser.ParseAndExport(paigeExporter);
	}
}
