using Newtonsoft.Json.Linq;

namespace Odin.Orleans.Core.Surrogates;

[GenerateSerializer]
public struct JObjectSurrogate
{
	[Id(0)]
	public string Json;
}

[RegisterConverter]
public sealed class JObjectSurrogateConverter : IConverter<JObject, JObjectSurrogate>
{
	public JObjectSurrogate ConvertToSurrogate(in JObject value)
		=> new() { Json = value.ToString() };

	public JObject ConvertFromSurrogate(in JObjectSurrogate surrogate)
		=> JObject.Parse(surrogate.Json);
}
