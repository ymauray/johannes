using System.CommandLine;
using Johannes;

Option<FileInfo?> docxOption = new("--docx", "-d") { Description = "Path to a .docx file to process." };
Option<bool> withoutTypstOption = new("--without-typst") { Description = "Do not export to Typst format." };
Option<bool> withoutPaigeOption = new("--without-paige") { Description = "Do not export to Paige format." };

var rootCommand = new RootCommand("johannes — converts .docx files to Typst and Paige formats.")
{
	docxOption,
	withoutTypstOption,
	withoutPaigeOption
};

ParseResult parseResult = rootCommand.Parse(args);
FileInfo? fileInfo = parseResult.GetValue<FileInfo?>("--docx");
bool withoutTypst = parseResult.GetValue<bool>(withoutTypstOption);
bool withoutPaige = parseResult.GetValue<bool>(withoutPaigeOption);

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
