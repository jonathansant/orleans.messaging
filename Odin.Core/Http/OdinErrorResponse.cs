using Odin.Core.Error;

namespace Odin.Core.Http;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public record OdinErrorResponse
{
	protected string DebuggerDisplay => $"ErrorCode: '{ErrorCode}', Origin: '{Origin}', Context: '{Context}', Fields: '{Fields?.Count}'";

	public string ErrorCode { get; set; }
	public string Context { get; set; }
	public string Origin { get; set; }
	public Dictionary<string, List<string>> Fields { get; set; }
	public FieldErrorState FieldErrors { get; set; }
	public Dictionary<string, object>? Data { get; set; }
}

public static class ErrorResultExtensions
{
	public static ErrorResult UpdateFrom(this ErrorResult errorResult, OdinErrorResponse? errorResponse)
	{
		if (errorResponse == null)
			return errorResult;

		errorResult.ErrorCode = errorResponse.ErrorCode;
		errorResult.Fields.AddErrors(errorResponse.Fields);
		errorResult.FieldErrors.AddErrors(errorResponse.FieldErrors);

		if (errorResponse.Data is not null)
			errorResult.Data.AddRange(errorResponse.Data);

		errorResult.Origin = errorResponse.Origin;
		errorResult.Context = errorResponse.Context;

		return errorResult;
	}
}