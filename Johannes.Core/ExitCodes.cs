namespace Johannes;

/// <summary>Codes de sortie de <c>johannes</c> (les codes 1 à 5 sont ceux de scrubx).</summary>
public static class ExitCodes
{
	public const int Success = 0;
	public const int InvalidArguments = 1;
	public const int DocumentNotFound = 2;
	public const int NotADocx = 3;
	public const int CheckFailed = 4;
	public const int RevisionsFailed = 5;
	public const int TypstFailed = 6;
	public const int PaigeFailed = 7;
	public const int MissingDependency = 8;
}
