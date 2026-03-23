using FluentlyHttpClient;
using FluentlyHttpClient.Middleware;
using Odin.Core.Error;
using System.Net;

namespace Odin.Core.Http;

public class OdinHttpMiddlewareErrorContext
{
	/// <summary>
	/// Gets the error result to be thrown as an api exception.
	/// </summary>
	public ErrorResult ErrorResult { get; internal set; }

	/// <summary>
	/// Gets the Http Request
	/// </summary>
	public FluentHttpRequest HttpRequest { get; internal set; }

	/// <summary>
	/// Gets the Http response.
	/// </summary>
	public FluentHttpResponse HttpResponse { get; internal set; }
}

public class OdinHttpMiddlewareErrorContext<TErrorResponse> : OdinHttpMiddlewareErrorContext
	where TErrorResponse : class
{
	/// <summary>
	/// Gets the deserialized error.
	/// </summary>
	public TErrorResponse ApiErrorResponse { get; internal set; }
}

public abstract class OdinErrorBaseHttpMiddleware<TErrorResponse> : IFluentHttpMiddleware
	where TErrorResponse : class
{
	private readonly string _origin;
	private readonly FluentHttpMiddlewareDelegate _next;
	private readonly FluentHttpMiddlewareClientContext _context;
	private readonly EventId _communicationEventId = new EventId(2000, "http-client.comm");
	protected readonly ILogger Logger;
	protected readonly string FeatureContext;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	protected OdinErrorBaseHttpMiddleware(
		FluentHttpMiddlewareDelegate next,
		FluentHttpMiddlewareClientContext context,
		ILogger logger,
		string? origin = null,
		string? featureContext = null
	)
	{
		_next = next;
		_context = context;
		Logger = logger;
		_origin = origin ?? GetOrigin(context.Identifier);
		FeatureContext = featureContext ?? GetFeatureContext(context.Identifier);
	}

	/// <inheritdoc />
	public async Task<FluentHttpResponse> Invoke(FluentHttpMiddlewareContext context)
	{
		var request = context.Request;
		FluentHttpResponse response;
		try
		{
			response = await _next(context);
		}
		catch (Exception ex)
		{
			Logger.Error(_communicationEventId, ex, "Exception thrown for request {request}", request);
			throw;
		}

		if (response.IsSuccessStatusCode)
			return response;

		var errorResult = new ErrorResult(response)
		{
			Origin = _origin
		};
		errorResult.Context ??= FeatureContext;
		response.SetErrors(errorResult);

		var contentType = response.Content.Headers.ContentType;
		var isSupportedMediaType = contentType != null && _context.Formatters.SelectMany(x => x.SupportedMediaTypes)
			.Any(x => x.MediaType == contentType.MediaType);

		await response.Content.LoadIntoBufferAsync();
		var responseContent = await response.Content.ReadAsStringAsync();

		var isRequestError = (int)response.StatusCode >= 400 && (int)response.StatusCode <= 499;
		if (!isRequestError)
		{
			errorResult.ErrorCode ??= OdinErrorCodes.InternalServerError;
			errorResult.AddData("clientErrorDetails", new OdinClientErrorDetails
			{
				StatusCode = response.StatusCode,
				ReasonPhrase = response.ReasonPhrase,
				RawContent = responseContent
			});

			var apiErrorException = errorResult.AsApiErrorException(HttpStatusCode.InternalServerError);
			Logger.Error(_communicationEventId, apiErrorException, "Unsuccessful response which is non 400-499. Response {response}; ContentType: {contentType}; Content: {content}",
				response, contentType, responseContent);

			throw apiErrorException;
		}

		if (!isSupportedMediaType)
		{
			Logger.Error("Unsupported content media type for unsuccessful response {response}; ContentType: {contentType}; Content: {content}",
				response, contentType, responseContent);
			errorResult.ErrorCode ??= OdinErrorCodes.InternalServerError;
			errorResult.AddData("clientErrorDetails", new OdinClientErrorDetails
			{
				StatusCode = response.StatusCode,
				ReasonPhrase = response.ReasonPhrase,
				RawContent = responseContent
			});
			throw errorResult.AsApiErrorException(HttpStatusCode.InternalServerError);
		}

		var errorContext = new OdinHttpMiddlewareErrorContext<TErrorResponse>
		{
			ErrorResult = errorResult,
			HttpResponse = response,
			ApiErrorResponse = await response.Content.ReadAsAsync<TErrorResponse>(_context.Formatters),
			HttpRequest = request
		};

		ExtractApiErrors(errorContext);
		EnsureSuccessfulApiResponse(errorContext);
		errorResult.StatusCode = MapStatusCode(errorContext);
		errorResult = ManipulateResponse(errorContext);
		if (errorResult == null)
			return response;

		if (errorResult.ErrorCode.IsNullOrEmpty())
		{
			errorResult.ErrorCode = MapGlobalErrors(errorContext);
			if (errorResult.ErrorCode.IsNullOrEmpty())
				errorResult.ErrorCode = OdinErrorCodes.InternalServerError;
		}

		Logger.Info("Response {response} has errors {errors}. ApiErrorResponse: {@apiError}. Content: {content}",
			response, errorResult, errorContext.ApiErrorResponse, responseContent);

		throw errorResult.AsApiErrorException();
	}

	/// <summary>
	/// Get feature context parse id by period e.g. 'midgard.user' => 'user'
	/// </summary>
	/// <param name="id">Id to parse feature context from.</param>
	protected static string GetFeatureContext(string id)
	{
		if (id.IsNullOrEmpty())
			return id;

		var periodIndex = id.IndexOf(".");
		if (periodIndex < 0)
			return null;

		return id.Substring(periodIndex + 1);
	}

	/// <summary>
	/// Get origin and strip feature context 'midgard.user' => 'midgard'
	/// </summary>
	/// <param name="id">Id to parse feature context from.</param>
	protected static string GetOrigin(string id)
	{
		if (id.IsNullOrEmpty())
			return id;

		var featureContext = GetFeatureContext(id);
		return featureContext.IsNullOrEmpty()
			? id
			: id.Replace(featureContext, string.Empty).TrimEnd('.');
	}

	/// <summary>
	/// Handler to extra api errors to result.
	/// </summary>
	/// <param name="context">Http middleware error context.</param>
	protected abstract void ExtractApiErrors(OdinHttpMiddlewareErrorContext<TErrorResponse> context);

	/// <summary>
	/// Ensures response is not an issue between us and server.
	/// </summary>
	/// <param name="context">Http middleware error context.</param>
	protected virtual void EnsureSuccessfulApiResponse(OdinHttpMiddlewareErrorContext<TErrorResponse> context) { }

	/// <summary>
	/// Map Http status code to return, by default take the response for 400-499.
	/// </summary>
	/// <param name="context">Http middleware error context.</param>
	protected virtual HttpStatusCode MapStatusCode(OdinHttpMiddlewareErrorContext<TErrorResponse> context)
		=> context.HttpResponse.StatusCode;

	/// <summary>
	/// Final hook to manipulate ErrorResult, can also return null to be handled and won't throw.
	/// </summary>
	/// <param name="context">Http middleware error context.</param>
	protected virtual ErrorResult ManipulateResponse(OdinHttpMiddlewareErrorContext<TErrorResponse> context)
		=> context.ErrorResult;

	/// <summary>
	/// Map generic global errors.
	/// </summary>
	/// <param name="context">Http middleware error context.</param>
	protected virtual string MapGlobalErrors(OdinHttpMiddlewareErrorContext<TErrorResponse> context) => null;
}
