using System.Diagnostics;
using System.Text;

namespace Johannes
{
	/// <summary>Issue d'un programme externe : code de sortie et sortie (standard puis erreur) mêlées.</summary>
	internal sealed record ProcessResult(int ExitCode, string Output);

	/// <summary>Lance un programme externe ; séparé de l'étape pour pouvoir être simulé dans les tests.</summary>
	internal interface IProcessRunner
	{
		Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string workingDirectory);
	}

	internal sealed class SystemProcessRunner : IProcessRunner
	{
		public async Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string workingDirectory)
		{
			var startInfo = new ProcessStartInfo(executable)
			{
				WorkingDirectory = workingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			foreach (var argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			using var process = Process.Start(startInfo)!;
			var output = process.StandardOutput.ReadToEndAsync();
			var error = process.StandardError.ReadToEndAsync();
			await process.WaitForExitAsync();

			return new ProcessResult(process.ExitCode, (await output + await error).TrimEnd());
		}
	}
}
