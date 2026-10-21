namespace Johannes
{
	/// <summary>Ce que demande <c>johannes config</c>.</summary>
	/// <param name="Key">Clé à lire ou écrire ; <c>null</c> avec <paramref name="List"/>.</param>
	/// <param name="Value">Valeur à écrire ; <c>null</c> pour lire.</param>
	/// <param name="List">Liste toutes les clés avec leur valeur effective et leur origine.</param>
	/// <param name="Unset">Retire la clé du fichier visé.</param>
	/// <param name="User">Vise le fichier de l'utilisateur plutôt que celui du livre.</param>
	internal sealed record ConfigRequest(string? Key, string? Value, bool List, bool Unset, bool User);

	/// <summary>La sous-commande <c>johannes config</c>, façon <c>git config</c>.</summary>
	internal static class ConfigCommand
	{
		public static int Run(ConfigRequest request, Configuration configuration, TextWriter output, TextWriter error)
		{
			if (request.List)
			{
				if (request.Key is not null || request.Value is not null || request.Unset)
				{
					error.WriteLine("Erreur : --list ne s'utilise pas avec une clé, une valeur ou --unset.");
					return ExitCodes.InvalidArguments;
				}

				foreach (var entry in configuration.Entries())
				{
					output.WriteLine($"{entry.Key} = {entry.Value} ({Describe(entry.Origin)})");
				}

				return ExitCodes.Success;
			}

			if (request.Key is null)
			{
				error.WriteLine("Erreur : indiquez une clé (ou --list). Clés connues :");
				foreach (var key in ConfigKeys.All)
				{
					error.WriteLine($"  {key.Name,-16} {key.Description}");
				}

				return ExitCodes.InvalidArguments;
			}

			if (!ConfigKeys.IsKnown(request.Key))
			{
				ConfigKeys.TryValidate(request.Key, "x", out var unknown);
				error.WriteLine($"Erreur : {unknown}");
				return ExitCodes.InvalidArguments;
			}

			if (request.Unset)
			{
				if (request.Value is not null)
				{
					error.WriteLine("Erreur : --unset ne prend pas de valeur.");
					return ExitCodes.InvalidArguments;
				}

				if (!configuration.Unset(request.User, request.Key))
				{
					error.WriteLine($"La clé '{request.Key}' n'est pas dans '{configuration.PathOf(request.User)}'.");
					return ExitCodes.InvalidArguments;
				}

				return ExitCodes.Success;
			}

			if (request.Value is null)
			{
				var entry = configuration.Effective(request.Key);
				if (entry.Value is null)
				{
					return ExitCodes.InvalidArguments;
				}

				output.WriteLine(entry.Value);
				return ExitCodes.Success;
			}

			if (!ConfigKeys.TryValidate(request.Key, request.Value, out var invalid))
			{
				error.WriteLine($"Erreur : {invalid}");
				return ExitCodes.InvalidArguments;
			}

			configuration.Set(request.User, request.Key, request.Value);
			return ExitCodes.Success;
		}

		private static string Describe(ConfigOrigin origin) => origin switch
		{
			ConfigOrigin.Local => "livre",
			ConfigOrigin.User => "utilisateur",
			ConfigOrigin.CommandLine => "ligne de commande",
			_ => "défaut"
		};
	}
}
