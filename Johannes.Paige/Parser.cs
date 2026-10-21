namespace Johannes.Paige;

public static class Parser
{
	public static EpubDocument Parse(string source, string basePath = ".")
	{
		var tokens = Lexer.Tokenize(source);
		return new ParserState(tokens, basePath).ParseDocument();
	}

	private sealed class ParserState(Token[] tokens, string basePath)
	{
		private int _pos;

		private Token Current => tokens[_pos];

		private Token Consume() => tokens[_pos++];
		private Token Consume(TokenType type)
		{
			if (Current.Type != type)
				throw new InvalidOperationException($"Ligne {Current.Line} : attendu {type}, trouvé {Current.Type} ('{Current.Value}')");
			return Consume();
		}

		public EpubDocument ParseDocument()
		{
			EpubMetadata? metadata = null;
			var manifest = new List<ManifestItem>();

			while (Current.Type != TokenType.Eof)
			{
				if (Current.Type == TokenType.Directive && Current.Value == "metadata")
				{
					metadata = ParseMetadata();
				}
				else if (Current.Type == TokenType.Directive && Current.Value == "manifest.add")
				{
					manifest.Add(ParseManifestItem());
				}
				else if (Current.Type == TokenType.Directive && Current.Value == "include")
				{
					manifest.AddRange(ParseInclude());
				}
				else
				{
					Consume();
				}
			}

			if (metadata is null)
				throw new InvalidOperationException("Directive #metadata manquante.");

			return new EpubDocument(metadata, manifest);
		}

		private List<ManifestItem> ParseInclude()
		{
			Consume(TokenType.Directive); // "include"
			var includePath = Consume(TokenType.String).Value;
			var fullIncludePath = Path.Combine(basePath, includePath);

			if (!File.Exists(fullIncludePath))
				throw new FileNotFoundException($"Le fichier inclus est introuvable : {fullIncludePath}", fullIncludePath);

			var includedSource = File.ReadAllText(fullIncludePath);
			var includedBasePath = Path.GetDirectoryName(fullIncludePath) ?? ".";

			var tokens = Lexer.Tokenize(includedSource);
			var subState = new ParserState(tokens, includedBasePath);

			var items = new List<ManifestItem>();
			while (subState.Current.Type != TokenType.Eof)
			{
				if (subState.Current.Type == TokenType.Directive && subState.Current.Value == "manifest.add")
				{
					items.Add(subState.ParseManifestItem());
				}
				else if (subState.Current.Type == TokenType.Directive && subState.Current.Value == "include")
				{
					items.AddRange(subState.ParseInclude());
				}
				else
				{
					subState.Consume();
				}
			}
			return items;
		}

		private EpubMetadata ParseMetadata()
		{
			Consume(TokenType.Directive); // "metadata"
			Consume(TokenType.LParen);

			string? identifier = null, title = null, language = null;

			while (Current.Type != TokenType.RParen && Current.Type != TokenType.Eof)
			{
				if (Current.Type == TokenType.Comma) { Consume(); continue; }

				var key = Consume(TokenType.Ident).Value;
				Consume(TokenType.Colon);
				var value = ConsumeValue();

				switch (key)
				{
					case "identifier": identifier = value; break;
					case "title": title = value; break;
					case "language":
						if (value is not ("fr" or "en"))
							throw new InvalidOperationException($"Ligne {Current.Line} : langue '{value}' non supportée. Valeurs acceptées : fr, en.");
						language = value;
						break;
				}
			}

			Consume(TokenType.RParen);

			if (identifier is null || title is null || language is null)
				throw new InvalidOperationException("Champs obligatoires manquants dans #metadata.");

			return new EpubMetadata(identifier, title, language);
		}

		private ManifestItem ParseManifestItem()
		{
			Consume(TokenType.Directive); // "manifest.add"
			Consume(TokenType.LParen);

			string? id = null, href = null, mediaType = null;
			string? properties = null, source = null, nav = null;
			bool inSpine = false;

			while (Current.Type != TokenType.RParen && Current.Type != TokenType.Eof)
			{
				if (Current.Type == TokenType.Comma) { Consume(); continue; }

				var key = Consume(TokenType.Ident).Value;
				Consume(TokenType.Colon);
				var value = ConsumeValue();

				switch (key)
				{
					case "id": id = value; break;
					case "href": href = value; break;
					case "mediaType": mediaType = value; break;
					case "properties": properties = value; break;
					case "source": source = value; break;
					case "nav": nav = value; break;
					case "spine": inSpine = value == "true"; break;
				}
			}

			Consume(TokenType.RParen);

			if (id is null || href is null || mediaType is null)
				throw new InvalidOperationException("Champs obligatoires manquants dans #manifest.add.");

			string? inlineContent = null;
			if (Current.Type == TokenType.LBracket)
			{
				Consume(TokenType.LBracket);
				inlineContent = Consume(TokenType.BlockContent).Value;
				Consume(TokenType.RBracket);
				source = null;
			}

			return new ManifestItem(id, href, mediaType, properties, source, inlineContent, nav, inSpine);
		}

		// Consomme Int, String, Bool ou Ident et retourne la valeur textuelle.
		private string ConsumeValue()
		{
			if (Current.Type is TokenType.Int or TokenType.String or TokenType.Bool or TokenType.Ident)
				return Consume().Value;
			throw new InvalidOperationException($"Ligne {Current.Line} : valeur attendue, trouvé {Current.Type}.");
		}
	}
}
