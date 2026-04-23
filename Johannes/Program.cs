using System.CommandLine;
using Johannes;

Option<FileInfo?> docxOption = new("--docx", "-d") { Description = "Path to a .docx file to process." };
Option<FileInfo?> epubOption = new("--epub", "-e") { Description = "Directory to store the epub files." };

var rootCommand = new RootCommand("johannes — converts .docx files to Epub and Typst formats.")
{
	docxOption,
	epubOption
};

ParseResult parseResult = rootCommand.Parse(args);
FileInfo? fileInfo = parseResult.GetValue<FileInfo?>("--docx");

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

FileInfo? epubDir = parseResult.GetValue<FileInfo?>("--epub");

if (epubDir != null)
{
	if (!epubDir.Exists)
	{
		Console.WriteLine($"The specified epub directory does not exist: {epubDir.FullName}");
		return;
	}
}

foreach (string docxFile in docxFiles)
{
	Console.WriteLine($"Processing: {docxFile}");
	var baseFile = Path.GetFileNameWithoutExtension(docxFile);
	var typstExporter = new TypstExporter(baseFile);
	var epubExporter = new EpubExporter(epubDir!);
	var documentParser = new DocumentParser(docxFile);
	documentParser.ParseAndExport(typstExporter);
	documentParser.ParseAndExport(epubExporter);
}
