using FluentlyHttpClient;
using Newtonsoft.Json;
using Odin.Core.Auth;
using System.Net;

namespace Odin.Core.Error;

/// <summary>
/// Represents standard error result.
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record ErrorResult
{
	public static string DefaultOrigin { get; set; } = string.Empty;

	protected string DebuggerDisplay => $"Error: '{ErrorCode}', Origin: '{Origin}', Context: '{Context}', FieldErrors: {FieldErrors.Count}";

	// todo: remove after FE transitions to the new ApiError structure
	/// <summary>
	/// Gets or sets the fields with errors dictionary.
	/// </summary>
	//[Obsolete("Use FieldErrors instead.")] // breaking Id serializer
	[Id(0)]
	public FieldErrorDictionary Fields { get; set; }

	[Id(1)]
	public FieldErrorState FieldErrors { get; set; }

	[Id(2)]
	private string? _errorCode;

	/// <summary>
	/// Gets or sets the error code. When error mappings is specified and value is being set, it will try to be mapped from it.
	/// </summary>
	public string? ErrorCode
	{
		get => _errorCode;
		set => _errorCode = value.IsNullOrEmpty() ? value : Fields.MapErrorOrDefault(value) ?? value;
	}

	/// <summary>
	/// Gets or sets the error origination e.g. 'grizzly', 'midgard', etc...
	/// </summary>
	[Id(3)]
	public string Origin { get; set; }

	/// <summary>
	/// Gets or sets the error data e.g. '{ "activationChannel": "sms" }', etc...
	/// </summary>
	[Id(4)]
	public IDictionary<string, object> Data { get; set; }

	/// <summary>
	/// Gets or sets the context which contains scope for the error origination e.g. 'validation', 'auth' etc...
	/// </summary>
	[Id(5)]
	public string? Context { get; set; }

	/// <summary>
	/// Gets or sets the HttpStatus code to be used when thrown.
	/// </summary>
	[JsonIgnore]
	[Id(6)]
	public HttpStatusCode StatusCode { get; set; }

	public ErrorResult()
	{
		Origin ??= DefaultOrigin;
		Fields = new();
		FieldErrors = new(this);
		Data ??= new Dictionary<string, object>();
	}

	public ErrorResult(
		IDictionary<string, string>? errorCodeMapping = null,
		IDictionary<string, string>? fieldMapping = null
	) : this()
	{
		Fields.WithErrorMappings(errorCodeMapping)
			.WithFieldMappings(fieldMapping);

		FieldErrors.WithErrorMappings(errorCodeMapping)
			.WithFieldMappings(fieldMapping);
	}

	public ErrorResult(
		IEnumerable<IDictionary<string, string>>? errorCodeMapping = null,
		IEnumerable<IDictionary<string, string>>? fieldMapping = null
	) : this()
	{
		Fields.WithErrorMappings(errorCodeMapping)
			.WithFieldMappings(fieldMapping);

		FieldErrors.WithErrorMappings(errorCodeMapping)
			.WithFieldMappings(fieldMapping);
	}

	public ErrorResult(ApiErrorException? apiError)
		: this()
	{
		if (apiError == null) return;
		ErrorCode = apiError.ErrorCode;
		Origin = apiError.Origin ?? Origin;
		Context = apiError.Context;
		StatusCode = apiError.StatusCode;
		Fields = apiError.FieldErrorsV1 ?? new FieldErrorDictionary();
		FieldErrors = apiError.FieldErrors ?? new FieldErrorState(this);
		Data = apiError.DataErrors ?? new Dictionary<string, object>();
	}

	public ErrorResult(FluentHttpResponse response)
		: this(response.GetErrorCodeMappings(), response.GetFieldErrorMappings())
	{
		Context = response.GetFeatureContext();
	}

	/// <summary>
	/// Initializes a new error result with a validation error.
	/// </summary>
	public static ErrorResult AsValidationError(string? errorCode = null)
		=> new ErrorResult().WithValidationFailure(errorCode);

	/// <summary>
	/// Initializes a new error result as not found error.
	/// </summary>
	public static ErrorResult AsNotFound(string id)
		=> new ErrorResult().WithNotFound(id);

	/// <summary>
	/// Initializes a new error result as not found error.
	/// </summary>
	public static ErrorResult AsNotFound(string id, string key)
		=> new ErrorResult().WithNotFound(id, key);

	/// <summary>
	/// Initializes a new error result as not found error.
	/// </summary>
	public static ErrorResult AsNotFound(IEnumerable<string> ids)
		=> new ErrorResult().WithNotFound(ids);

	/// <summary>
	/// Shorthand for creating exception for required field.
	/// </summary>
	public static ApiErrorException RequiredFieldException(params string[] fields)
		=> RequiredFieldsException(fields.ToList());

	/// <summary>
	/// Shorthand for creating exception for required field.
	/// </summary>
	public static ApiErrorException RequiredFieldsException(List<string> fields)
	{
		var exception = AsValidationError();
		foreach (var field in fields) exception.AddField(field, x => x.AsRequired());
		return exception.AsApiErrorException();
	}

	/// <summary>
	/// Set error code.
	/// </summary>
	/// <param name="errorCode">Error code to set.</param>
	public ErrorResult WithErrorCode(string errorCode)
	{
		ErrorCode = errorCode;
		return this;
	}

	/// <summary>
	/// Set context.
	/// </summary>
	/// <param name="context">Set error context.</param>
	public ErrorResult WithContext(string context)
	{
		Context = context;
		return this;
	}

	/// <summary>
	/// Set error origin.
	/// </summary>
	/// <param name="origin">Set error origin.</param>
	public ErrorResult WithOrigin(string origin)
	{
		Origin = origin;
		return this;
	}

	public override string ToString() => DebuggerDisplay;
}

public static class ErrorResultExt
{
	public static ErrorResult ToErrorResult(this UnauthorizedException ex)
	{
		var errorResult = new ErrorResult { Context = ErrorContextTypes.Authorization, ErrorCode = ex.ErrorCode, };

		if (!ex.Claims.IsNullOrEmpty())
			errorResult.AddData("requiredClaims", ex.Claims);

		return errorResult;
	}

	// todo: deprecate - instead if field error use AddField/FieldV2
	public static ErrorResult AddData(this ErrorResult errorResult, string key, ErrorDataField detail)
	{
		errorResult.Data.Add(key, detail);
		return errorResult;
	}

	// todo: implement AddError and replace this with it - which is not field errors e.g. brand requires activation, space configure, platform configured etc...
	public static ErrorResult AddData(this ErrorResult errorResult, string key, string error, object value)
		=> errorResult.AddData(key, new() { ErrorCode = error, Value = value });

	public static ErrorResult AddData(this ErrorResult errorResult, string key, object value, bool replace = true)
	{
		if (replace)
			errorResult.Data[key] = value;
		else
			errorResult.Data.TryAdd(key, value);
		return errorResult;
	}

	public static bool HasErrors(this ErrorResult? errorResult)
		=> errorResult is not null && (errorResult.Fields.Count > 0 || errorResult.FieldErrors.Count > 0);

	/// <summary>
	/// Adds an entry in both <see cref="ErrorResult.Fields"/> and <see cref="ErrorResult.Data"/>.
	/// </summary>
	public static ErrorResult AddField(this ErrorResult errorResult, string key, string error, object? value = null)
		=> errorResult.AddField(key, new ErrorDataField { ErrorCode = error, Value = value });

	/// <summary>
	/// Adds an entry in both <see cref="ErrorResult.Fields"/>, <see cref="ErrorResult.FieldErrors"/> and <see cref="ErrorResult.Data"/>.
	/// </summary>
	public static ErrorResult AddField(
		this ErrorResult errorResult,
		string key,
		ErrorDataField detail,
		bool replace = true,
		bool addData = true,
		Func<string, string>? keyFormatter = null
	)
	{
		keyFormatter ??= (x => x.ToCamelCase());
		key = errorResult.FieldErrors.MapFieldOrDefault(keyFormatter(key));
		detail.ErrorCode = errorResult.FieldErrors.MapErrorOrDefault(detail.ErrorCode);

		errorResult.Fields.AddError(key, detail.ErrorCode, x => x);

		// todo: remove AddData (only used for backward compatibility)
		if (addData)
			errorResult.AddData(key, detail, replace);

		errorResult.FieldErrors.AddError(key, detail, x => x);

		// todo: auto build errorMessage from errorCode + data (when available) + extendable - either here or in error middleware
		// e.g. when error.required '{property} is required.'
		// e.g. when error.invalidAmount '{property} must be between {minItems} and {maxItems} items, but {actualItems} were provided.'

		return errorResult;
	}

	public static ErrorResult WithValidationFailure(this ErrorResult errorResult, string? errorCode = null)
	{
		errorResult.ErrorCode = errorCode ?? OdinErrorCodes.ValidationFailed;
		errorResult.Context = ErrorContextTypes.Validation;
		errorResult.StatusCode = HttpStatusCode.BadRequest;
		return errorResult;
	}

	/// <summary>
	/// Configures the error result to be as not found.
	/// </summary>
	/// <param name="errorResult"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public static ErrorResult WithNotFound(this ErrorResult errorResult, params string[] id)
		=> errorResult.WithNotFound((IEnumerable<string>)id);

	/// <summary>
	/// Configures the error result to be as not found.
	/// </summary>
	/// <param name="errorResult"></param>
	/// <param name="key"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public static ErrorResult WithNotFound(this ErrorResult errorResult, string key, params string[] id)
		=> errorResult.WithNotFound(id, key);

	/// <summary>
	/// Configures the error result to be as not found.
	/// </summary>
	/// <param name="errorResult"></param>
	/// <param name="ids"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static ErrorResult WithNotFound(this ErrorResult errorResult, IEnumerable<string> ids, string key = "id")
	{
		errorResult.ErrorCode = OdinErrorCodes.NotFound;
		errorResult.Context = ErrorContextTypes.Validation;
		errorResult.StatusCode = HttpStatusCode.NotFound;
		if (ids.Any())
			errorResult.AddData(key, ids);
		return errorResult;
	}

	public static ErrorResult WithUnauthorized(this ErrorResult errorResult, string? id = null, string key = "id")
	{
		errorResult.ErrorCode = OdinErrorCodes.Auth.Unauthorized;
		errorResult.Context = ErrorContextTypes.Authorization;
		errorResult.StatusCode = HttpStatusCode.Unauthorized;
		if (!id.IsNullOrEmpty())
			errorResult.AddData(key, id);
		return errorResult;
	}

	/// <summary>
	/// Configures the error result to be as forbidden (not enough permissions).
	/// </summary>
	public static ErrorResult WithForbidden(this ErrorResult errorResult)
	{
		errorResult.ErrorCode = OdinErrorCodes.Forbidden;
		errorResult.Context = ErrorContextTypes.Authorization;
		errorResult.StatusCode = HttpStatusCode.Forbidden;
		return errorResult;
	}

	/// <summary>
	/// Configures the error result to be as internal server error.
	/// </summary>
	public static ErrorResult WithInternalServerError(this ErrorResult errorResult)
	{
		errorResult.ErrorCode = OdinErrorCodes.InternalServerError;
		errorResult.Context ??= ErrorContextTypes.Validation;
		errorResult.StatusCode = HttpStatusCode.InternalServerError;
		return errorResult;
	}
}
