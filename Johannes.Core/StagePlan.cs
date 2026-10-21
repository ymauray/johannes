namespace Johannes;

/// <summary>Options de ligne de commande qui déterminent les étapes à exécuter.</summary>
/// <param name="Only">Étapes à exécuter exclusivement (<c>--only</c>).</param>
/// <param name="Until">Dernière étape à exécuter (<c>--until</c>).</param>
/// <param name="Skip">Étapes à sauter (<c>--skip</c>).</param>
/// <param name="TypstOnly"><c>--typst</c> : branche Typst uniquement.</param>
/// <param name="PaigeOnly"><c>--paige</c> : branche Paige uniquement.</param>
/// <param name="WithoutTypst"><c>--without-typst</c>.</param>
/// <param name="WithoutPaige"><c>--without-paige</c>.</param>
public sealed record PlanRequest(
	Stages? Only = null,
	Stages? Until = null,
	Stages Skip = Stages.None,
	bool TypstOnly = false,
	bool PaigeOnly = false,
	bool WithoutTypst = false,
	bool WithoutPaige = false);

/// <summary>Étapes retenues et formats à générer.</summary>
/// <param name="Stages">Étapes à exécuter.</param>
/// <param name="GenerateTypst">L'étape <c>generate</c> écrit le <c>.typ</c>.</param>
/// <param name="GeneratePaige">L'étape <c>generate</c> écrit le <c>.paige</c>.</param>
/// <param name="Notes">Précisions à donner à l'utilisateur (ex. étape ajoutée d'office).</param>
public sealed record StagePlan(Stages Stages, bool GenerateTypst, bool GeneratePaige, IReadOnlyList<string> Notes)
{
	public bool Includes(Stages stage) => Stages.HasFlag(stage);
}

public static class StagePlanner
{
	/// <summary>Étapes dans l'ordre d'exécution.</summary>
	public static readonly IReadOnlyList<Stages> Ordered = [Stages.Check, Stages.Generate, Stages.Pdf, Stages.Epub];

	/// <summary>
	/// Calcule le plan. <c>check</c> est un prérequis de <c>generate</c> (D15) : il est ajouté d'office
	/// et ne peut pas être sauté.
	/// </summary>
	/// <returns>Le plan, ou <c>null</c> avec <paramref name="error"/> si la demande est incohérente.</returns>
	public static StagePlan? Create(PlanRequest request, out string? error)
	{
		error = null;

		if (request.TypstOnly && request.PaigeOnly)
		{
			error = "Les options --typst et --paige sont mutuellement exclusives.";
			return null;
		}

		if ((request.TypstOnly || request.PaigeOnly) && (request.WithoutTypst || request.WithoutPaige))
		{
			error = $"Lors de l'utilisation de {(request.TypstOnly ? "--typst" : "--paige")}, vous ne pouvez pas utiliser --without-typst ou --without-paige.";
			return null;
		}

		if (request.Until is { } until && !IsSingleStage(until))
		{
			error = "L'option --until attend une seule étape.";
			return null;
		}

		var stages = Stages.All;
		var typst = true;
		var paige = true;

		if (request.TypstOnly || request.WithoutPaige)
		{
			stages &= ~Stages.Epub;
			paige = false;
		}

		if (request.PaigeOnly || request.WithoutTypst)
		{
			stages &= ~Stages.Pdf;
			typst = false;
		}

		if (!typst && !paige)
		{
			stages &= ~Stages.Generate;
		}

		if (request.Only is { } only)
		{
			stages &= only;
		}

		if (request.Until is { } last)
		{
			stages &= (Stages)(((int)last << 1) - 1);
		}

		stages &= ~request.Skip;

		var notes = new List<string>();
		if (stages.HasFlag(Stages.Generate))
		{
			if (request.Skip.HasFlag(Stages.Check))
			{
				error = "L'étape check ne peut pas être sautée : elle est un prérequis de generate.";
				return null;
			}

			if (!stages.HasFlag(Stages.Check))
			{
				stages |= Stages.Check;
				notes.Add("L'étape check est ajoutée d'office : elle est un prérequis de generate.");
			}
		}

		if (stages == Stages.None)
		{
			error = "Aucune étape à exécuter.";
			return null;
		}

		return new StagePlan(stages, typst, paige, notes);
	}

	/// <summary>Analyse une liste d'étapes séparées par des virgules (<c>check,generate</c>).</summary>
	public static bool TryParse(string? text, out Stages stages, out string? error)
	{
		stages = Stages.None;
		error = null;

		foreach (var name in (text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (!Enum.TryParse<Stages>(name, ignoreCase: true, out var stage) || !IsSingleStage(stage))
			{
				error = $"Étape inconnue : '{name}' (attendu : {string.Join(", ", Ordered.Select(Name))}).";
				return false;
			}

			stages |= stage;
		}

		if (stages == Stages.None)
		{
			error = "Aucune étape indiquée.";
			return false;
		}

		return true;
	}

	public static string Name(Stages stage) => stage.ToString().ToLowerInvariant();

	private static bool IsSingleStage(Stages stage) => Ordered.Contains(stage);
}
