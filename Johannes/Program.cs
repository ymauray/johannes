using System.CommandLine;
using Johannes;

Option<FileInfo?> docxOption = new("--docx", "-d") { Description = "Path to a .docx file to process." };
Option<bool> withoutTypstOption = new("--without-typst") { Description = "Do not export to Typst format." };
Option<bool> withoutPaigeOption = new("--without-paige") { Description = "Do not export to Paige format." };
Option<bool> typstOnlyOption = new("--typst") { Description = "Export ONLY to Typst format. No other options allowed." };
Option<bool> paigeOnlyOption = new("--paige") { Description = "Export ONLY to Paige format. No other options allowed." };

var rootCommand = new RootCommand("johannes — converts .docx files to Typst and Paige formats.")
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
		Console.WriteLine("Error: Options --typst and --paige are mutually exclusive.");
		return;
	}

	// Check if any forbidden option was provided. 
	// --docx is allowed, but --without-typst and --without-paige are not.
	var forbiddenOptions = parseResult.RootCommandResult.Children
		.OfType<System.CommandLine.Parsing.OptionResult>()
		.Where(o => o.Option.Name != "docx" && o.Option.Name != "typst" && o.Option.Name != "paige");

	if (forbiddenOptions.Any())
	{
		Console.WriteLine($"Error: When using {(typstOnly ? "--typst" : "--paige")}, you cannot use --without-typst or --without-paige.");
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
	Console.WriteLine("No .docx files found in the current directory.");
	return;
}

foreach (string docxFile in docxFiles)
{
	Console.WriteLine($"Processing: {docxFile}");
	var documentParser = new DocumentParser(docxFile);
	var baseFile = Path.GetFileNameWithoutExtension(docxFile);

	if (!withoutTypst)
	{
		Console.WriteLine(" - Exporting to Typst...");
		var typstExporter = new TypstExporter(baseFile);
		documentParser.ParseAndExport(typstExporter);
	}

	if (!withoutPaige)
	{
		Console.WriteLine(" - Exporting to Paige...");
		var paigeExporter = new PaigeExporter(baseFile);
		documentParser.ParseAndExport(paigeExporter);
	}
}
