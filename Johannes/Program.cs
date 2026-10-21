using System.CommandLine;
using System.Text;
using Johannes;

Console.OutputEncoding = Encoding.UTF8;

Option<FileInfo?> docxOption = new("--docx", "-d")
{
	Description = "Chemin vers un fichier .docx à traiter.",
	Required = true
};
Option<string?> onlyOption = new("--only") { Description = "N'exécute que ces étapes (check, generate, pdf, epub), séparées par des virgules." };
Option<string?> untilOption = new("--until") { Description = "S'arrête après cette étape (check, generate, pdf ou epub)." };
Option<string?> skipOption = new("--skip") { Description = "Saute ces étapes, séparées par des virgules. check ne peut pas être sauté si generate s'exécute." };
Option<string?> revisionsOption = new("--revisions") { Description = "Écrit une copie annotée du .docx (révisions suivies) ; --revisions=<fichier> pour choisir le nom.", Arity = ArgumentArity.ZeroOrOne };
Option<string?> authorOption = new("--author") { Description = "Auteur des révisions de la copie annotée." };
Option<bool> dryRunOption = new("--dry-run") { Description = "Affiche les étapes prévues sans rien exécuter." };
Option<bool> verboseOption = new("--verbose", "-v") { Description = "Affiche aussi les étapes non demandées." };
Option<bool> withoutTypstOption = new("--without-typst") { Description = "Ne pas exporter au format Typst (ni fabriquer les PDF)." };
Option<bool> withoutPaigeOption = new("--without-paige") { Description = "Ne pas exporter au format Paige (ni fabriquer l'EPUB)." };
Option<bool> typstOnlyOption = new("--typst") { Description = "Branche Typst uniquement : vérification, .typ et PDF. Incompatible avec --without-*." };
Option<bool> paigeOnlyOption = new("--paige") { Description = "Branche Paige uniquement : vérification, .paige et EPUB. Incompatible avec --without-*." };

var rootCommand = new RootCommand("johannes — vérifie un fichier .docx puis fabrique les versions Typst, PDF, Paige et EPUB du livre.")
{
	docxOption,
	onlyOption,
	untilOption,
	skipOption,
	revisionsOption,
	authorOption,
	dryRunOption,
	verboseOption,
	withoutTypstOption,
	withoutPaigeOption,
	typstOnlyOption,
	paigeOnlyOption
};

ParseResult parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0)
{
	foreach (var error in parseResult.Errors)
	{
		Console.Error.WriteLine($"Erreur : {error.Message}");
	}

	return ExitCodes.InvalidArguments;
}

if (!TryParseStages(parseResult.GetValue(onlyOption), "--only", out var only)
	|| !TryParseStages(parseResult.GetValue(untilOption), "--until", out var until)
	|| !TryParseStages(parseResult.GetValue(skipOption), "--skip", out var skip))
{
	return ExitCodes.InvalidArguments;
}

var plan = StagePlanner.Create(
	new PlanRequest(
		only,
		until,
		skip ?? Stages.None,
		parseResult.GetValue(typstOnlyOption),
		parseResult.GetValue(paigeOnlyOption),
		parseResult.GetValue(withoutTypstOption),
		parseResult.GetValue(withoutPaigeOption)),
	out var planError);

if (plan is null)
{
	Console.Error.WriteLine($"Erreur : {planError}");
	return ExitCodes.InvalidArguments;
}

var docxPath = parseResult.GetValue(docxOption)!.FullName;

var revisionsRequested = parseResult.GetResult(revisionsOption) is not null;
if (revisionsRequested && !plan.Includes(Stages.Check))
{
	Console.Error.WriteLine("Erreur : --revisions exige l'étape check.");
	return ExitCodes.InvalidArguments;
}

var revisions = revisionsRequested
	? new RevisionRequest(parseResult.GetValue(revisionsOption), parseResult.GetValue(authorOption))
	: null;

var preflight = Preflight.Run(docxPath, plan.Stages);
if (!preflight.Ok)
{
	foreach (var issue in preflight.Errors)
	{
		Console.Error.WriteLine($"Erreur : {issue.Message}");
	}

	return preflight.ExitCode;
}

var context = new PipelineContext(
	docxPath,
	preflight.Layout!,
	plan,
	parseResult.GetValue(verboseOption),
	Console.Out,
	Console.Error,
	preflight.TypstPath,
	revisions);

var pipeline = new Pipeline(
[
	new CheckStage(),
	new GenerateStage(),
	new PdfStage(),
	new PendingStage(Stages.Epub, phase: 7, layout => layout.EpubOmissionReason),
]);

Console.WriteLine($"Traitement de : {docxPath}");

if (parseResult.GetValue(dryRunOption))
{
	pipeline.Describe(context);
	return ExitCodes.Success;
}

foreach (var note in plan.Notes)
{
	Console.WriteLine(note);
}

return pipeline.Run(context);

static bool TryParseStages(string? text, string option, out Stages? stages)
{
	stages = null;
	if (text is null)
	{
		return true;
	}

	if (!StagePlanner.TryParse(text, out var parsed, out var error))
	{
		Console.Error.WriteLine($"Erreur : {option} : {error}");
		return false;
	}

	stages = parsed;
	return true;
}
