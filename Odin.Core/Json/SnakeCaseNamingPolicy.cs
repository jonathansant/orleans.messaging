using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
	public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

	public override string ConvertName(string name)
		=> name.ToSnakeCase();
}
