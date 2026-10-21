namespace Johannes;

/// <summary>Issue d'une étape.</summary>
/// <param name="ExitCode">0 en cas de succès.</param>
/// <param name="Message">Explication affichée en cas d'échec.</param>
public sealed record StageOutcome(int ExitCode, string? Message = null)
{
	public static readonly StageOutcome Success = new(ExitCodes.Success);

	public bool Ok => ExitCode == ExitCodes.Success;

	public static StageOutcome Failure(int exitCode, string message) => new(exitCode, message);
}

/// <summary>Demande de copie annotée (<c>--revisions</c>, <c>--author</c>).</summary>
/// <param name="Path">Fichier de destination, ou <c>null</c> pour le nom par défaut.</param>
/// <param name="Author">Auteur des révisions, ou <c>null</c> pour la valeur par défaut.</param>
public sealed record RevisionRequest(string? Path, string? Author);

/// <summary>Ce que les étapes savent du travail en cours.</summary>
public sealed record PipelineContext(
	string DocxPath,
	BookLayout Layout,
	StagePlan Plan,
	bool Verbose,
	TextWriter Out,
	TextWriter Error,
	string? TypstPath = null,
	RevisionRequest? Revisions = null);

/// <summary>Une étape de la chaîne.</summary>
public interface IStage
{
	Stages Id { get; }

	/// <summary>Raison pour laquelle l'étape est sans objet pour ce livre, ou <c>null</c> si elle s'applique.</summary>
	string? OmissionReason(PipelineContext context);

	StageOutcome Run(PipelineContext context);
}

/// <summary>Enchaîne les étapes du plan, dans l'ordre, et s'arrête à la première qui échoue.</summary>
public sealed class Pipeline(IReadOnlyList<IStage> stages)
{
	public int Run(PipelineContext context)
	{
		foreach (var stage in stages.OrderBy(s => s.Id))
		{
			var name = StagePlanner.Name(stage.Id);

			if (!context.Plan.Includes(stage.Id))
			{
				if (context.Verbose)
				{
					context.Out.WriteLine($"[{name}] non demandée.");
				}
				continue;
			}

			if (stage.OmissionReason(context) is { } reason)
			{
				context.Out.WriteLine($"[{name}] omise : {reason}");
				continue;
			}

			context.Out.WriteLine($"[{name}] ...");
			var outcome = stage.Run(context);
			if (!outcome.Ok)
			{
				context.Error.WriteLine($"Erreur [{name}] : {outcome.Message}");
				return outcome.ExitCode;
			}
		}

		return ExitCodes.Success;
	}

	/// <summary>Affiche ce qui serait fait, sans rien exécuter.</summary>
	public void Describe(PipelineContext context)
	{
		foreach (var note in context.Plan.Notes)
		{
			context.Out.WriteLine(note);
		}

		foreach (var stage in stages.OrderBy(s => s.Id))
		{
			var name = StagePlanner.Name(stage.Id);
			var status = !context.Plan.Includes(stage.Id) ? "non demandée"
				: stage.OmissionReason(context) is { } reason ? $"omise ({reason})"
				: "à faire";
			context.Out.WriteLine($"{name,-9} {status}");
		}
	}
}
