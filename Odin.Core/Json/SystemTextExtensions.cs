using Newtonsoft.Json.Linq;
using Odin.Core.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

public static class SystemTextExtensions
{
	extension(JsonSerializerOptions options)
	{
		/// <summary>
		/// Register convertors for data types for System.Text.
		/// </summary>
		public JsonSerializerOptions WithOdinDataTypeConvertors()
		{
			options.Converters.Add(new MonthYearJsonConverter());
			options.Converters.Add(new JsonElementToObjectConverter());
			options.Converters.Add(new JObjectConverter());

			return options;
		}

		/// <summary>
		/// Configure using Odin standard defaults for Apis e.g. mvc, gql, signalr.
		/// </summary>
		public JsonSerializerOptions WithOdinApiDefaults()
		{
			options.AllowTrailingCommas = true;
			options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.ReadCommentHandling = JsonCommentHandling.Skip;
			options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
			options.Converters.Insert(0, new TrimStringConverter());
			options.WithOdinDataTypeConvertors();

			return options;
		}
	}
}

public class JsonElementToObjectConverter : JsonConverter<object>
{
	public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType switch
		{
			JsonTokenType.String => reader.GetString(),
			JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
			JsonTokenType.Number when reader.TryGetDouble(out var d) => d,
			JsonTokenType.True => true,
			JsonTokenType.False => false,
			JsonTokenType.Null => null,
			JsonTokenType.StartObject or JsonTokenType.StartArray => JsonSerializer.Deserialize<JsonElement>(ref reader, options),
			_ => JsonSerializer.Deserialize<JsonElement>(ref reader, options)
		};

	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		=> writer.WriteRawValue(JsonSerializer.Serialize(value, value.GetType(), options));
}

public class JObjectConverter : JsonConverter<JObject>
{
	public override JObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// Read the JSON as a string and parse it with Newtonsoft
		using var doc = JsonDocument.ParseValue(ref reader);
		var json = doc.RootElement.GetRawText();

		return JObject.Parse(json);
	}

	public override void Write(Utf8JsonWriter writer, JObject value, JsonSerializerOptions options)
	{
		// Write the JObject as raw JSON
		writer.WriteRawValue(value.ToString());
	}
}
