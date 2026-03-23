using Odin.Core.Error;
using System.Net;
using System.Runtime.Serialization;

namespace Odin.Core;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ApiResult<T>
{
	protected string DebuggerDisplay => $"StatusCode: {StatusCode}, Errors: '{{ {Errors} }}', Data: {{ {Data} }}";

	public HttpStatusCode StatusCode { get; set; }
	public T Data { get; set; }
	public ErrorResult Errors { get; set; }

	[IgnoreDataMember]
	public bool HasErrors
	{
		get
		{
			if (Errors == null) return false;
			return !Errors.ErrorCode.IsNullOrEmpty();
		}
	}
}
