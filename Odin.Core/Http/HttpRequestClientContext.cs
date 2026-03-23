using FluentlyHttpClient;

namespace Odin.Core.Http;

[GenerateSerializer]
public class HttpRequestClientContext
{
	[Id(0)]
	public FluentHttpHeaders Headers { get; set; }
	[Id(1)]
	public Dictionary<string, object> Items { get; set; }
}
