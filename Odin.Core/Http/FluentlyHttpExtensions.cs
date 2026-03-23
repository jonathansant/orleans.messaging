using Odin.Core;
using Odin.Core.Error;
using Odin.Core.Http;

// ReSharper disable once CheckNamespace
namespace FluentlyHttpClient;

/// <summary>
/// Fluent HTTP request builder extensions.
/// </summary>
public static class FluentlyHttpExtensions
{
	private const string ErrorsKey = "ERRORS";
	private const string FieldErrorMappingKey = "FIELD_ERROR_MAPPING";
	private const string ErrorCodeMappingKey = "ERROR_CODE_MAPPING";
	private const string FeatureContextKey = "FEATURE_CONTEXT";
	private const string ServiceKey = "SERVICE_KEY";

	/// <summary>
	/// Get data from <see cref="HttpRequestClientContext"/> such as headers.
	/// </summary>
	/// <returns>Returns request builder for chaining.</returns>
	public static FluentHttpRequestBuilder FromClientContext(this FluentHttpRequestBuilder builder, HttpRequestClientContext? context)
	{
		if (context?.Headers != null)
			builder.WithHeaders(context.Headers);

		if (context?.Items != null)
			foreach (var contextItem in context.Items)
				builder.WithItem(contextItem.Key, contextItem.Value);

		return builder;
	}

	/// <summary>
	/// Set feature context which can be used additionally for additional information.
	/// </summary>
	/// <returns>Returns request builder for chaining.</returns>
	public static FluentHttpRequestBuilder WithFeatureContext(this FluentHttpRequestBuilder builder, string context)
		=> builder.WithItem(FeatureContextKey, context);

	/// <summary>
	/// Get feature context.
	/// </summary>
	/// <param name="message">message to get items from.</param>
	public static string? GetFeatureContext(this IFluentHttpMessageState message)
	{
		if (message.Items.TryGetValue(FeatureContextKey, out var value))
			return (string)value;
		return null;
	}

	/// <summary>
	/// Set field error mapping.
	/// </summary>
	/// <returns>Returns request builder for chaining.</returns>
	public static FluentHttpRequestBuilder WithFieldErrorMapping(this FluentHttpRequestBuilder builder, Dictionary<string, string> mapping)
	{
		var mappings = builder.GetFieldErrorMappings() ?? new List<Dictionary<string, string>>();
		mappings.Insert(0, mapping);

		return builder.WithItem(FieldErrorMappingKey, mappings);
	}

	/// <summary>
	/// Set error mapping.
	/// </summary>
	/// <returns>Returns request builder for chaining.</returns>
	public static FluentHttpRequestBuilder WithErrorCodeMapping(this FluentHttpRequestBuilder builder, Dictionary<string, string> mapping)
		=> builder.WithErrorCodeMapping(newMappings: mapping);

	/// <summary>
	/// Set multiple error mappings.
	/// </summary>
	/// <returns>Returns request builder for chaining.</returns>
	public static FluentHttpRequestBuilder WithErrorCodeMapping(this FluentHttpRequestBuilder builder, params Dictionary<string, string>[] newMappings)
	{
		var mappings = builder.GetErrorCodeMappings() ?? new List<Dictionary<string, string>>();
		mappings.InsertRange(0, newMappings);

		return builder.WithItem(ErrorCodeMappingKey, mappings);
	}

	/// <summary>
	/// Set secret authorization
	/// </summary>
	/// <param name="requestBuilder"></param>
	/// <param name="secret"></param>
	/// <returns></returns>
	public static FluentHttpRequestBuilder WithSecretAuthentication(this FluentHttpRequestBuilder requestBuilder, string secret)
		=> requestBuilder.WithHeader(HeaderTypes.Authorization, $"Secret {secret}");

	/// <summary>
	/// Set locale
	/// </summary>
	/// <param name="requestBuilder"></param>
	/// <param name="locale"></param>
	/// <returns></returns>
	public static FluentHttpRequestBuilder WithLocale(this FluentHttpRequestBuilder requestBuilder, string locale)
		=> requestBuilder.WithHeader(HttpHeaders.Odin.Locale, locale);

	/// <summary>
	/// Set geo-location country code
	/// </summary>
	/// <param name="requestBuilder"></param>
	/// <param name="countryCode">Country code to set e.g. "de"</param>
	/// <returns></returns>
	public static FluentHttpRequestBuilder WithCountryCode(this FluentHttpRequestBuilder requestBuilder, string countryCode)
		=> requestBuilder.WithHeader(HttpHeaders.Odin.CountryCode, countryCode);

	/// <summary>
	/// Get error code mapping.
	/// </summary>
	/// <param name="message">message to get items from.</param>
	public static List<Dictionary<string, string>>? GetErrorCodeMappings(this IFluentHttpMessageItems message)
	{
		if (message.Items.TryGetValue(ErrorCodeMappingKey, out var value))
			return (List<Dictionary<string, string>>)value;
		return null;
	}

	/// <summary>
	/// Get error code mapping.
	/// </summary>
	/// <param name="message">message to get items from.</param>
	public static List<Dictionary<string, string>>? GetFieldErrorMappings(this IFluentHttpMessageItems message)
	{
		if (message.Items.TryGetValue(FieldErrorMappingKey, out var value))
			return (List<Dictionary<string, string>>)value;
		return null;
	}

	/// <summary>
	/// Set errors for response.
	/// </summary>
	/// <param name="response">Response instance.</param>
	/// <param name="errors">Errors to set value.</param>
	public static void SetErrors(this FluentHttpResponse response, ErrorResult errors)
		=> response.Items[ErrorsKey] = errors;

	/// <summary>
	/// Get errors for the response. This is generally set via middleware.
	/// </summary>
	/// <param name="response">Response to get time from.</param>
	/// <returns>Returns errors from response.</returns>
	public static ErrorResult? GetErrors(this FluentHttpResponse response)
	{
		if (response.Items.TryGetValue(ErrorsKey, out var errorsValue))
			return (ErrorResult)errorsValue;
		return null;
	}

	/// <summary>
	/// Sets the service key to the request items to identify to which service the request is addressed to.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="serviceKey"></param>
	/// <returns></returns>
	public static FluentHttpRequestBuilder WithAuthServiceKey(this FluentHttpRequestBuilder builder, string serviceKey)
	{
		builder.Items.Add(ServiceKey, serviceKey);
		return builder;
	}

	/// <summary>
	/// Gets the service key to the request items to identify to which service the request is addressed to.
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	public static string? GetAuthServiceKey(this FluentHttpRequest request)
	{
		request.Items.TryGetValue(ServiceKey, out var key);
		return key as string;
	}

	/// <summary>
	/// Get header value as coerce bool <see cref="StringExtensions.ToBoolCoerce"/>.
	/// </summary>
	/// <param name="headers">Headers collection.</param>
	/// <param name="header">Header value to get.</param>
	public static bool? GetValueAsBoolCoerce(this FluentHttpHeaders headers, string header)
		=> headers.GetValue(header)?.ToBoolCoerce();
}
