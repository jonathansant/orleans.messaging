using Odin.Core.Json;
using System.Text.Json;

namespace Odin.Orleans.Core.Surrogates;

[GenerateSerializer]
public struct JsonElementSurrogate
{
	[Id(0)]
	public string Json;
}

[RegisterConverter]
public sealed class JsonElementSurrogateConverter : IConverter<JsonElement, JsonElementSurrogate>
{
	public JsonElementSurrogate ConvertToSurrogate(in JsonElement value)
		=> new() { Json = value.GetRawText() };

	public JsonElement ConvertFromSurrogate(in JsonElementSurrogate surrogate)
		=> JsonDocument.Parse(surrogate.Json, JsonUtils.JsonDocumentInputOptions).RootElement;
}

[GenerateSerializer]
public struct JsonDocumentSurrogate
{
	[Id(0)]
	public string Json;
}

[RegisterConverter]
public sealed class JsonDocumentSurrogateConverter : IConverter<JsonDocument, JsonDocumentSurrogate>
{
	public JsonDocumentSurrogate ConvertToSurrogate(in JsonDocument value)
		=> new() { Json = value.RootElement.GetRawText() };

	public JsonDocument ConvertFromSurrogate(in JsonDocumentSurrogate surrogate)
		=> JsonDocument.Parse(surrogate.Json, JsonUtils.JsonDocumentInputOptions);
}
