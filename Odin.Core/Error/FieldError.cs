using System.Text.RegularExpressions;

namespace Odin.Core.Error;

// todo: rename to FieldError
[GenerateSerializer]
public record ErrorDataField
{
	/// <summary>
	/// Gets or sets the error code e.g. 'error.required'.
	/// </summary>
	[Id(0)]
	public required string ErrorCode { get; set; }

	/// <summary>
	/// Gets or sets the error code e.g. 'Field is required.'.
	/// </summary>
	[Id(1)]
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// Gets or sets the field value. e.g. 'jon doe'
	/// </summary>
	[Id(2)]
	public object? Value { get; set; }

	/// <summary>
	/// Gets or sets field additional data related to the validation.
	/// </summary>
	[Id(3)]
	public object? Data { get; set; }

	public ErrorDataField WithErrorMessage(string? errorMessage)
	{
		ErrorMessage = errorMessage;
		return this;
	}

	public ErrorDataField WithErrorCode(string errorCode)
	{
		ErrorCode = errorCode;
		return this;
	}

	public ErrorDataField WithData(object data)
	{
		Data = data;
		return this;
	}
}

public record ErrorFieldBuilderContext(
	string FieldName,
	ErrorResult ErrorResult
);

public static class ErrorFieldBuilderContextExtensions
{
	/// <summary>
	/// Adds an entry in <see cref="ErrorResult.Fields"/>, <see cref="ErrorResult.FieldErrors"/> and <see cref="ErrorResult.Data"/> using a predefined templates.
	/// </summary>
	public static ErrorResult AddField(
		this ErrorResult errorResult,
		string key,
		Func<ErrorFieldBuilderContext, ErrorDataField> configureFieldError,
		Func<string, string>? keyFormatter = null
	) => errorResult.AddField(key, configureFieldError(new(key, errorResult)), keyFormatter: keyFormatter);

	/// <summary>
	/// Adds a field error using a predefined templates.
	/// </summary>
	public static FieldErrorState AddError(this FieldErrorState fieldErrorState, string key, Func<ErrorFieldBuilderContext, ErrorDataField> configureFieldError)
		=> fieldErrorState.AddError(key, configureFieldError(new(key, fieldErrorState.ErrorResult!)));

	/// <summary>
	/// Build error data field that is required.
	/// </summary>
	public static ErrorDataField AsRequired(this ErrorFieldBuilderContext builder)
		=> builder.AsInvalid(null, OdinErrorMessages.RequiredFieldTemplate)
			.WithErrorCode(OdinErrorCodes.Required);

	/// <summary>
	/// Build error data field that is not supported.
	/// </summary>
	public static ErrorDataField AsNotSupported(this ErrorFieldBuilderContext builder, object? value = null)
		=> builder.AsInvalid(value, OdinErrorMessages.NotSupportedFieldTemplate)
			.WithErrorCode(OdinErrorCodes.Unsupported);

	/// <summary>
	/// Build error data field that is required field groups e.g. with required 'id' OR ('timestampFrom' and 'timestampTo').
	/// </summary>
	public static ErrorDataField AsRequiredFieldGroups(this ErrorFieldBuilderContext builder, List<List<string>> fieldGroups)
	{
		return new()
		{
			ErrorCode = OdinErrorCodes.Required,
			ErrorMessage = OdinErrorMessages.RequiredFieldGroupsTemplate.FromTemplate(
				new Dictionary<string, object>
				{
					["PropertyName"] = builder.FieldName,
					["RequiredFieldGroups"] = BuildGroupString(fieldGroups)
				}
			),
			Data = new Dictionary<string, object>
			{
				["RequiredGroups"] = fieldGroups
			}
		};

		static string BuildGroupString(List<List<string>> fieldGroups)
		{
			var groupStrings = new List<string>();

			foreach (var fieldGroup in fieldGroups)
			{
				switch (fieldGroup.Count)
				{
					case 1:
						groupStrings.Add($"'{fieldGroup[0]}'");
						break;
					case > 1:
						{
							var groupString = $"({string.Join(" AND ", fieldGroup.Select(field => $"'{field}'"))})";
							groupStrings.Add(groupString);
							break;
						}
				}
			}

			return string.Join(" OR ", groupStrings);
		}
	}

	/// <summary>
	/// Build error data field which already exists.
	/// </summary>
	public static ErrorDataField AsAlreadyExists(this ErrorFieldBuilderContext builder, object? value)
		=> new()
		{
			ErrorCode = OdinErrorCodes.AlreadyExists,
			ErrorMessage = OdinErrorMessages.EntityAlreadyExists,
			Value = value
		};

	/// <summary>
	/// Build error data field which are duplicates.
	/// </summary>
	public static ErrorDataField AsDuplicates(this ErrorFieldBuilderContext builder, object? value)
		=> new()
		{

			ErrorCode = OdinErrorCodes.Duplicate,
			ErrorMessage = OdinErrorMessages.DuplicateDataTemplate,
			Value = value
		};

	/// <summary>
	/// Build error data field which is archived.
	/// </summary>
	public static ErrorDataField AsArchived(this ErrorFieldBuilderContext builder, object? value)
		=> new()
		{
			ErrorCode = OdinErrorCodes.Archived,
			ErrorMessage = OdinErrorMessages.Archived,
			Value = value
		};

	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	public static ErrorDataField AsInvalid(this ErrorFieldBuilderContext builder, object? value, string errorMessage)
		=> new()
		{
			ErrorCode = OdinErrorCodes.Invalid,
			ErrorMessage = errorMessage.FromTemplate(
				new Dictionary<string, object?>
				{
					["PropertyName"] = builder.FieldName,
					["Value"] = value.ToStringify(),
				}
			),
			Value = value
		};

	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	public static ErrorDataField AsInvalid(this ErrorFieldBuilderContext builder, object? value = null)
		=> builder.AsInvalid(value, OdinErrorMessages.InvalidTemplate);

	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	public static ErrorDataField AsInvalid<T>(this ErrorFieldBuilderContext builder, IEnumerable<T>? value = null)
		=> builder.AsInvalid((object?)value?.ToList());

	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	public static ErrorDataField AsInvalid<T>(this ErrorFieldBuilderContext builder, T? value)
		=> builder.AsInvalid((object?)value);

	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	public static ErrorDataField AsInvalid(this ErrorFieldBuilderContext builder, object? value, IEnumerable<object> validItems)
		=> builder.AsInvalid(value, OdinErrorMessages.InvalidItemTemplate)
			.WithData(new FieldErrorEnumData(validItems.ToList()));

	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value"></param>
	/// <param name="validItems">List of valid items.</param>
	/// <param name="itemTransformer">String formatter used for the <paramref name="validItems"/>.</param>
	public static ErrorDataField AsInvalid(this ErrorFieldBuilderContext builder, object? value,
		IEnumerable<string> validItems,
		Func<string, string>? itemTransformer = null
	)
	{
		itemTransformer ??= ItemTransformer;
		return builder.AsInvalid(value, (IEnumerable<object>)validItems.Select(x => itemTransformer(x)));

		static string ItemTransformer(string arg)
			=> arg.ToCamelCase();
	}
	/// <summary>
	/// Build error data field which is invalid.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="value"></param>
	/// <param name="enumType">Enum type to get names for.</param>
	/// <param name="itemTransformer">String formatter used for the <paramref name="enumType"/> names.</param>
	public static ErrorDataField AsInvalid(this ErrorFieldBuilderContext builder, object? value,
		Type enumType,
		Func<string, string>? itemTransformer = null
	) => builder.AsInvalid(value, Enum.GetNames(enumType), itemTransformer);

	/// <summary>
	/// Build error data field which is immutable data.
	/// </summary>
	public static ErrorDataField AsImmutableData(this ErrorFieldBuilderContext builder, object? value = null)
		=> builder.AsInvalid(value, OdinErrorMessages.ImmutableTemplate)
			.WithErrorCode(OdinErrorCodes.Immutable);

	/// <summary>
	/// Build error data field which as mismatched.
	/// </summary>
	public static ErrorDataField AsMismatch(this ErrorFieldBuilderContext builder, object? value, object? comparedValue)
		=> new()
		{
			ErrorCode = OdinErrorCodes.Mismatch,
			ErrorMessage = OdinErrorMessages.MismatchTemplate.FromTemplate(
				new Dictionary<string, object?>
				{
					["PropertyName"] = builder.FieldName,
					["Value"] = value.ToStringify(),
					["ComparedValue"] = comparedValue.ToStringify(),
				}
			),
			Value = value,
			Data = new Dictionary<string, object?>
			{
				["ComparedValue"] = comparedValue
			}
		};

	/// <summary>
	/// Builds error data field which is invalid amount.
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="actualItems"></param>
	/// <param name="minItems"></param>
	/// <param name="maxItems"></param>
	/// <returns></returns>
	public static ErrorDataField AsInvalidItemsAmount(this ErrorFieldBuilderContext builder,
		int actualItems,
		int minItems,
		int maxItems
	) => new()
	{
		ErrorCode = OdinErrorCodes.InvalidAmount,
		Value = actualItems,
		Data = new FieldErrorItemAmountData
		(
			MinItems: minItems,
			MaxItems: maxItems,
			ActualItems: actualItems
		)
	};

	/// <summary>
	/// Build error data field which is not found.
	/// </summary>
	public static ErrorDataField AsNotFound(this ErrorFieldBuilderContext builder, object? value = null)
		=> new()
		{
			ErrorCode = OdinErrorCodes.NotFound,
			ErrorMessage = OdinErrorMessages.EntityNotFound,
			Value = value
		};

	/// <summary>
	/// Build error data field which is forbidden.
	/// </summary>
	public static ErrorDataField AsForbidden(this ErrorFieldBuilderContext builder, object? value = null)
		=> new()
		{
			ErrorCode = OdinErrorCodes.Forbidden,
			ErrorMessage = OdinErrorMessages.ForbiddenAccess,
			Value = value
		};

	public static ErrorDataField AsUnrelatedEntity(this ErrorFieldBuilderContext builder, object? value = null)
		=> new()
		{
			ErrorCode = OdinErrorCodes.UnrelatedEntity,
			ErrorMessage = OdinErrorMessages.UnrelatedEntity,
			Value = value
		};
}

[GenerateSerializer]
public record FieldErrorItemAmountData
(
	int MinItems,
	int MaxItems,
	int ActualItems
);

[GenerateSerializer]
public record FieldErrorEnumData
(
	List<object> Items
);

[GenerateSerializer]
public record FieldErrorLengthData
(
	int Min,
	int Max,
	int Actual
);
