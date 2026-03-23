using FluentlyHttpClient;
using System.Net;

namespace Odin.Core.Error;

[GenerateSerializer]
public record struct ApiErrorData
{
	[Id(0)]
	public string? ErrorCode { get; set; }
	[Id(1)]
	public string? Context { get; set; }
	[Id(2)]
	public string? Origin { get; set; }
	[Id(3)]
	public HttpStatusCode StatusCode { get; set; }
	[Id(4)]
	public FieldErrorDictionary? FieldErrorsV1 { get; set; }
	[Id(5)]
	public FieldErrorState? FieldErrors { get; set; }
	[Id(6)]
	public IDictionary<string, object>? Data { get; set; }
}

[GenerateSerializer]
public class ApiErrorException : Exception
{
	[Id(0)]
	public string? ErrorCode { get; set; }
	[Id(1)]
	public HttpStatusCode StatusCode { get; set; }

	[Id(2)]
	public FieldErrorDictionary? FieldErrorsV1 { get; set; }
	[Id(3)]
	public FieldErrorState? FieldErrors { get; set; }

	[Id(4)]
	public IDictionary<string, object>? DataErrors { get; set; }

	/// <summary>
	/// Gets or sets the error origination e.g. 'grizzly', 'midgard', etc...
	/// </summary>
	[Id(5)]
	public string? Origin { get; set; } = ErrorResult.DefaultOrigin;

	/// <summary>
	/// Gets or sets the context which contains scope for the error's origin e.g. 'validation', 'auth' etc...
	/// </summary>
	[Id(6)]
	public string? Context { get; set; }

	public ApiErrorException(
		string? errorCode,
		HttpStatusCode statusCode,
		FieldErrorDictionary? fieldErrorV1Dictionary = null,
		FieldErrorState? fieldErrorState = null,
		string? context = null,
		string? origin = null,
		IDictionary<string, object>? dataErrors = null,
		Exception? inner = null
	) : this($"ErrorCode: {errorCode}, Status: {statusCode}, Origin: {origin}, Context: {context}", inner)
	{
		ErrorCode = errorCode;
		FieldErrorsV1 = fieldErrorV1Dictionary;
		StatusCode = statusCode;
		Origin = origin;
		Context = context;
		DataErrors = dataErrors;
		FieldErrors = fieldErrorState;
	}

	public ApiErrorException(ErrorResult errorResult, HttpStatusCode statusCode, Exception? inner = null)
		: this(errorResult.ErrorCode, statusCode, errorResult.Fields, errorResult.FieldErrors, errorResult.Context, errorResult.Origin, errorResult.Data, inner)
	{
	}

	public ApiErrorException(FluentHttpResponse response, HttpStatusCode? statusCode = null)
		: this($"ErrorCode: {response.GetErrors()?.ErrorCode}, Status: {statusCode ?? response.StatusCode}")
	{
		if (response == null) throw new ArgumentNullException(nameof(response));

		var errors = response.GetErrors();
		ErrorCode = errors?.ErrorCode;
		Origin = errors?.Origin;
		Context = errors?.Context ?? response.GetFeatureContext();
		FieldErrorsV1 = errors?.Fields;
		StatusCode = statusCode ?? response.StatusCode;
		DataErrors = errors?.Data;
		FieldErrors = errors?.FieldErrors;
	}

	public ApiErrorException(string message, Exception? inner = null) : base(message, inner)
	{
	}

	public ApiErrorException()
	{
	}

	public ApiErrorData ToData()
		=> new()
		{
			ErrorCode = ErrorCode,
			Origin = Origin,
			Context = Context,
			FieldErrorsV1 = FieldErrorsV1,
			FieldErrors = FieldErrors,
			StatusCode = StatusCode,
			Data = DataErrors
		};

	public static ApiErrorException AsValidation(string? errorCode = null)
		=> new(errorCode ?? OdinErrorCodes.ValidationFailed, HttpStatusCode.BadRequest, context: ErrorContextTypes.Validation);

	private static IDictionary<string, object>? MaterializeAnyEnumerables(IDictionary<string, object>? dataDictionary)
	{
		if (dataDictionary == null) return dataDictionary;

		foreach (var kvp in dataDictionary)
			dataDictionary[kvp.Key] = MaterializeAnyEnumerables(kvp.Value);

		return dataDictionary;
	}

	private static object MaterializeAnyEnumerables(object data)
	{
		if (data is not ErrorDataField errorDataField) return MaterializeIfEnumerable(data);

		errorDataField.Value = MaterializeIfEnumerable(errorDataField.Value);
		return errorDataField;
	}

	private static object MaterializeIfEnumerable(object data)
		=> data is IEnumerable<string> enumerable
			? enumerable.ToList()
			: data;
}

public static class ApiErrorExceptionExtensions
{
	public static ApiErrorException AsApiErrorException(this ErrorResult errorResult,
		HttpStatusCode? statusCode = null,
		Exception? inner = null
	) => new(errorResult, statusCode ?? errorResult.StatusCode, inner);
}
