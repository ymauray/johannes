namespace Johannes;

/// <summary>Étapes de la chaîne de fabrication, dans l'ordre d'exécution.</summary>
[Flags]
public enum Stages
{
	None = 0,
	Check = 1,
	Generate = 2,
	Pdf = 4,
	Epub = 8,
	All = Check | Generate | Pdf | Epub,
}
