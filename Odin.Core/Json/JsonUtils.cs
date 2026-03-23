using Newtonsoft.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Odin.Core.Json;

public static class JsonUtils
{
	/// <summary>
	/// Predefined JSON settings which can be used for rendering JSON e.g. audit, file etc...
	/// </summary>
	public static readonly JsonSerializerOptions JsonRenderedSettings = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	}.With(opts =>
		{
			opts.WithOdinDataTypeConvertors();
			opts.Converters.Add(new JsonStringEnumConverter());
		}
	);

	/// <summary>
	/// Predefined JSON settings which can be used for input e.g. file, request etc...
	/// </summary>
	public static readonly JsonSerializerOptions JsonInputSettings = new JsonSerializerOptions
	{
		ReadCommentHandling = JsonCommentHandling.Skip,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		AllowTrailingCommas = true,
	}.With(opts => opts.WithOdinDataTypeConvertors());

	/// <summary>
	/// Predefined JSON settings which can be used for cloning, fast serializing etc...
	/// </summary>
	public static readonly JsonSerializerOptions JsonBasicSettings = new JsonSerializerOptions
	{
	}.With(opts =>
		{
			opts.WithOdinDataTypeConvertors();
			opts.TypeInfoResolver = new HashIgnoreResolver();
		}
	);

	/// <summary>
	/// Register convertors for data types for Newtonsoft.
	/// </summary>
	public static JsonSerializerSettings WithOdinDataTypeConvertors(this JsonSerializerSettings opts)
	{
		opts.Converters.Add(new MonthYearNewtonsoftJsonConverter());
		opts.Converters.Add(new JsonElementNewtonsoftConverter());
		opts.Converters.Add(new NullableJsonElementNewtonsoftConverter());
		return opts;
	}

	/// <summary>
	/// JsonDocument options which allow trailing commas and skip comments.
	/// </summary>

	public static readonly JsonDocumentOptions JsonDocumentInputOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

}
