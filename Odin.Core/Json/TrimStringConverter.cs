using System.Text.Json;
using System.Text.Json.Serialization;

namespace Odin.Core.Json;

public class TrimStringConverter : JsonConverter<string>
{
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var value = reader.GetString();
			return value?.Trim();
		}

		return null;
	}

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value);
}

