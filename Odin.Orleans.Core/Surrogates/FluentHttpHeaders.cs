using FluentlyHttpClient;

namespace Odin.Orleans.Core.Surrogates;

[GenerateSerializer]
public struct FluentHttpHeadersSurrogate
{
	[Id(0)]
	public Dictionary<string, string[]> Data;
}

[RegisterConverter]
public sealed class FluentHttpHeadersSurrogateConverter : IConverter<FluentHttpHeaders, FluentHttpHeadersSurrogate>
{
	public FluentHttpHeaders ConvertFromSurrogate(in FluentHttpHeadersSurrogate surrogate)
		=> new(surrogate.Data);

	public FluentHttpHeadersSurrogate ConvertToSurrogate(in FluentHttpHeaders value)
		=> new()
		{
			Data = value.ToDictionary()
		};
}
