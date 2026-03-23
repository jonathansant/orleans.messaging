using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Odin.Core.Json;

/// <summary>
/// Newtonsoft.Json converter for System.Text.Json.JsonElement.
/// Bridges between System.Text.Json and Newtonsoft.Json serialization.
/// </summary>
public sealed class JsonElementNewtonsoftConverter : JsonConverter<JsonElement>
{
	public override JsonElement ReadJson(
		JsonReader reader,
		Type objectType,
		JsonElement existingValue,
		bool hasExistingValue,
		Newtonsoft.Json.JsonSerializer serializer)
	{
		// Load the Newtonsoft JToken
		var jToken = JToken.Load(reader);

		// Convert JToken to string, then parse with System.Text.Json
		var jsonString = jToken.ToString(Formatting.None);
		return JsonDocument.Parse(jsonString).RootElement.Clone();
	}

	public override void WriteJson(
		JsonWriter writer,
		JsonElement value,
		Newtonsoft.Json.JsonSerializer serializer)
	{
		// Convert JsonElement to string, then parse with Newtonsoft.Json
		var jsonString = value.GetRawText();
		var jToken = JToken.Parse(jsonString);
		jToken.WriteTo(writer);
	}

	public override bool CanRead => true;
	public override bool CanWrite => true;
}

/// <summary>
/// Newtonsoft.Json converter for nullable System.Text.Json.JsonElement.
/// </summary>
public sealed class NullableJsonElementNewtonsoftConverter : JsonConverter<JsonElement?>
{
	public override JsonElement? ReadJson(
		JsonReader reader,
		Type objectType,
		JsonElement? existingValue,
		bool hasExistingValue,
		Newtonsoft.Json.JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;

		// Load the Newtonsoft JToken
		var jToken = JToken.Load(reader);

		// Convert JToken to string, then parse with System.Text.Json
		var jsonString = jToken.ToString(Formatting.None);
		return JsonDocument.Parse(jsonString).RootElement.Clone();
	}

	public override void WriteJson(
		JsonWriter writer,
		JsonElement? value,
		Newtonsoft.Json.JsonSerializer serializer)
	{
		if (value == null || value.Value.ValueKind == JsonValueKind.Null || value.Value.ValueKind == JsonValueKind.Undefined)
		{
			writer.WriteNull();
			return;
		}

		// Convert JsonElement to string, then parse with Newtonsoft.Json
		var jsonString = value.Value.GetRawText();
		var jToken = JToken.Parse(jsonString);
		jToken.WriteTo(writer);
	}

	public override bool CanRead => true;
	public override bool CanWrite => true;
}
