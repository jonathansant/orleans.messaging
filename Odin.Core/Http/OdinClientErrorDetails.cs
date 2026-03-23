using System.Net;

namespace Odin.Core.Http;

[GenerateSerializer]
public record OdinClientErrorDetails
{
	[Id(0)]
	public HttpStatusCode? StatusCode { get; set; }
	[Id(1)]
	public string? ReasonPhrase { get; set; }
	[Id(2)]
	public string? RawContent { get; set; }
}
