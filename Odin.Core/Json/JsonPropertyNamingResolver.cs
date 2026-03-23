using System.Text.Json;

namespace Odin.Core.Json;

public enum OdinJsonNamingPolicy
{
	CamelCase,
	SnakeCaseLower,
	SnakeCaseUpper,
	KebabCaseLower,
	KebabCaseUpper
}

public static class JsonPropertyNamingResolver
{
	public static JsonNamingPolicy Resolve(OdinJsonNamingPolicy namingPolicy)
		=> namingPolicy switch
		{
			OdinJsonNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
			OdinJsonNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
			OdinJsonNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
			OdinJsonNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
			OdinJsonNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
			_ => throw new ArgumentOutOfRangeException(nameof(namingPolicy), namingPolicy, null)
		};
}
