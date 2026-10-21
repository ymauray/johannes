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
Option<bool> rebuildOption = new("--rebuild") { Description = "Ignore le cache (.build/) : refait toutes les étapes demandées." };
Option<bool> dryRunOption = new("--dry-run") { Description = "Affiche les étapes prévues sans rien exécuter." };
Option<bool> verboseOption = new("--verbose", "-v") { Description = "Affiche aussi les étapes non demandées." };
Option<bool> withoutTypstOption = new("--without-typst") { Description = "Ne pas exporter au format Typst (ni fabriquer les PDF)." };
Option<bool> withoutPaigeOption = new("--without-paige") { Description = "Ne pas exporter au format Paige (ni fabriquer l'EPUB)." };
Option<bool> typstOnlyOption = new("--typst") { Description = "Branche Typst uniquement : vérification, .typ et PDF. Incompatible avec --without-*." };
Option<bool> paigeOnlyOption = new("--paige") { Description = "Branche Paige uniquement : vérification, .paige et EPUB. Incompatible avec --without-*." };

Option<string?> paperDirectoryOption = new("--paper-directory") { Description = "Dossier des reliures Typst (remplace la configuration, défaut : Papier)." };
Option<string?> epubDirectoryOption = new("--epub-directory") { Description = "Dossier du projet EPUB (remplace la configuration, défaut : epub)." };
Option<string?> typstPathOption = new("--typst-path") { Description = "Chemin complet de typst (remplace la configuration et le PATH)." };

Argument<string?> configKeyArgument = new("key") { Description = "Clé de configuration (paper-directory, epub-directory, typst-path).", Arity = ArgumentArity.ZeroOrOne };
Argument<string?> configValueArgument = new("value") { Description = "Valeur à écrire ; sans valeur, affiche la valeur effective.", Arity = ArgumentArity.ZeroOrOne };
Option<bool> configListOption = new("--list", "-l") { Description = "Liste toutes les clés avec leur valeur effective et leur origine." };
Option<bool> configUnsetOption = new("--unset") { Description = "Retire la clé du fichier visé." };
Option<bool> configUserOption = new("--user") { Description = "Écrit dans le fichier de l'utilisateur plutôt que dans .johannes (dossier courant)." };

var configCommand = new Command("config", "Lit ou écrit la configuration (comme git config). Priorité : défauts, utilisateur, livre (.johannes), ligne de commande.")
{
	configKeyArgument,
	configValueArgument,
	configListOption,
	configUnsetOption,
	configUserOption
};

var rootCommand = new RootCommand("johannes — vérifie un fichier .docx puis fabrique les versions Typst, PDF, Paige et EPUB du livre.")
{
	configCommand,
	paperDirectoryOption,
	epubDirectoryOption,
	typstPathOption,
	docxOption,
	onlyOption,
	untilOption,
	skipOption,
	rebuildOption,
	revisionsOption,
	authorOption,
	dryRunOption,
	verboseOption,
	withoutTypstOption,
	withoutPaigeOption,
	typstOnlyOption,
	paigeOnlyOption
};

// Sans action, System.CommandLine exige une sous-commande dès qu'il y en a une ; le travail se fait plus bas.
rootCommand.SetAction(_ => ExitCodes.Success);

ParseResult parseResult = rootCommand.Parse(args);

if (args.Any(a => a is "--help" or "-h" or "-?" or "/?"))
{
	return parseResult.Invoke();
}

if (parseResult.Errors.Count > 0)
{
	foreach (var error in parseResult.Errors)
	{
		Console.Error.WriteLine($"Erreur : {error.Message}");
	}

	return ExitCodes.InvalidArguments;
}

if (parseResult.CommandResult.Command == configCommand)
{
	return ConfigCommand.Run(
		new ConfigRequest(
			parseResult.GetValue(configKeyArgument),
			parseResult.GetValue(configValueArgument),
			parseResult.GetValue(configListOption),
			parseResult.GetValue(configUnsetOption),
			parseResult.GetValue(configUserOption)),
		new Configuration(Configuration.LocalPathFor(Directory.GetCurrentDirectory()), Configuration.DefaultUserPath()),
		Console.Out,
		Console.Error);
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

var overrides = new Dictionary<string, string>();
AddOverride(ConfigKeys.PaperDirectory, parseResult.GetValue(paperDirectoryOption));
AddOverride(ConfigKeys.EpubDirectory, parseResult.GetValue(epubDirectoryOption));
AddOverride(ConfigKeys.TypstPath, parseResult.GetValue(typstPathOption));

var configuration = new Configuration(
	Configuration.LocalPathFor(Path.GetDirectoryName(docxPath)!),
	Configuration.DefaultUserPath());
var settings = configuration.Resolve(overrides, out var configErrors);
if (configErrors.Count > 0)
{
	foreach (var configError in configErrors)
	{
		Console.Error.WriteLine($"Erreur de configuration : {configError}");
	}

	return ExitCodes.InvalidArguments;
}

var preflight = Preflight.Run(docxPath, plan.Stages, settings: settings);
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
	revisions,
	new BuildCache(
		preflight.Layout!.Directory,
		BuildCache.VersionOf(typeof(DocumentParser).Assembly, typeof(BuildCache).Assembly, typeof(Johannes.Scrubx.DocxValidator).Assembly, typeof(Johannes.Paige.Epub).Assembly),
		parseResult.GetValue(rebuildOption)));

var pipeline = new Pipeline(
[
	new CheckStage(),
	new GenerateStage(),
	new PdfStage(),
	new EpubStage(),
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

void AddOverride(string key, string? value)
{
	if (value is not null)
	{
		overrides[key] = value;
	}
}

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
