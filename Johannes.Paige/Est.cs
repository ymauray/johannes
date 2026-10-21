namespace Johannes.Paige;

public record EpubDocument(
	EpubMetadata Metadata,
	IReadOnlyList<ManifestItem> Manifest
);

public record EpubMetadata(
	string Identifier,
	string Title,
	string Language
);

public record ManifestItem(
	string Id,
	string Href,
	string MediaType,
	string? Properties,
	string? Source,
	string? InlineContent,
	string? Nav,
	bool InSpine
);
