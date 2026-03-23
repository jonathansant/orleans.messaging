using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using HeyRed.Mime;
using Odin.Core.Countries;
using Odin.Core.Error;
using Odin.Core.Files;
using Odin.Core.Querying.Filtering;
using Odin.Core.Validation;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Odin.Core;

public static class FluentValidationExtensions
{
	private const string DefaultMimeType = "application/octet-stream";
	private const string UpdateRuleSetName = "update";
	private const string CreateRuleSetName = "create";

	private static readonly ICountryService _countryService = new CountryService();

	public static IRuleBuilder<T, string> PasswordStrength<T>(this IRuleBuilder<T, string> ruleBuilder, int minLength = 8)
	{
		var options = ruleBuilder
			.MinimumLength(minLength).WithMessage($"Password must be at least {minLength} characters.")
			.WithErrorCode(OdinErrorCodes.Validation.MinLength)
			.Matches("[A-Z]").WithMessage("Password must have at least one upper case character.")
			.WithErrorCode("error.validation.password.uppercase")
			.Matches("[a-z]").WithMessage("Password must have at least one lower case character.")
			.WithErrorCode("error.validation.password.lowercase")
			.Matches("[0-9]").WithMessage("Password must have at least one digit/number")
			.WithErrorCode("error.validation.password.digit")
			.Matches("[^a-zA-Z0-9]").WithMessage("Password must have at least one special character.")
			.WithErrorCode("error.validation.password.special-char");
		return options;
	}

	public static IRuleBuilder<T, string> Locale<T>(this IRuleBuilder<T, string> ruleBuilder)
	{
		var options = ruleBuilder
			.Must(x =>
			{
				if (x.IsNullOrEmpty())
					return true;

				if (x.Length > OdinPropFields.LocaleCodeLength)
					return false;

				if (Const.CustomLocales.Contains(x))
					return true;

				if (CultureInfo.GetCultures(CultureTypes.AllCultures)
					.Any(culture => string.Equals(culture.Name, x, StringComparison.CurrentCultureIgnoreCase))) return true;
				return false;
			})
			.WithErrorCode(OdinErrorCodes.Invalid);
		return options;
	}

	public static IRuleBuilder<T, string> OdinAlphanumeric<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	)
	{
		var settings = new OdinAlphanumericValidationSettings
		{
			AllowWhitespace = true,
			AllowUpperCasing = true,
			AllowLeadingNumeric = true,
			AllowLeadingSymbols = false,
			AllowLeadingWhitespace = true,
			AllowNumeric = true,
			AllowSymbols = true,
			AllowTrailingNumeric = true,
			AllowTrailingSymbols = true,
			AllowTrailingWhitespace = true,
			AllowMimeTypeExtensionLike = true,
			NonAllowedSymbols = new() { "<", ">" },
			MaxLength = 1024
		};

		configure?.Invoke(settings);

		return ruleBuilder.OdinAlphanumeric(settings);
	}

	public static IRuleBuilder<T, string> OdinAlphanumeric<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		OdinAlphanumericValidationSettings settings
	)
	{
		if (!settings.AllowMimeTypeExtensionLike)
		{
			ruleBuilder
				// when the supplied string is not a valid extension, MimeTypesMap.GetMimeType will return DefaultMimeType
				.Must(x => x.IsNullOrEmpty() || MimeTypesMap.GetMimeType(x).Equals(DefaultMimeType, StringComparison.OrdinalIgnoreCase))
				.WithMessage("'{PropertyName}' must not end with a mime-type extension e.g. '.jpg'.")
				.WithErrorCode(OdinErrorCodes.Validation.MimeTypeExtensionLikeFormat);
		}

		return ruleBuilder
			.AddNumericRules(settings)
			.AddWhitespaceRules(settings)
			.AddLengthRules(settings)
			.AddSymbolRules(settings)
			.AddCasingRules(settings)
			;
	}

	private static IRuleBuilder<T, string> AddNumericRules<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		OdinAlphanumericValidationSettings settings
	)
	{
		if (!settings.AllowNumeric)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || !Regexes.Numeric.IsMatch(x))
				.WithMessage("'{PropertyName}' must not contain numeric characters.")
				.WithErrorCode(OdinErrorCodes.Validation.NumericNotAllowed);
		}
		else
		{
			if (!settings.AllowLeadingNumeric)
			{
				ruleBuilder
					.Must(x => x.IsNullOrEmpty() || !Regexes.NumericLeading.IsMatch(x))
					.WithMessage("'{PropertyName}' must not contain leading numeric characters.")
					.WithErrorCode(OdinErrorCodes.Validation.LeadingNumericNotAllowed);
			}

			if (!settings.AllowTrailingNumeric)
			{
				ruleBuilder
					.Must(x => x.IsNullOrEmpty() || !Regexes.NumericTrailingSingle.IsMatch(x))
					.WithMessage("'{PropertyName}' must not contain trailing numeric characters.")
					.WithErrorCode(OdinErrorCodes.Validation.TrailingNumericNotAllowed);
			}
		}

		return ruleBuilder;
	}

	private static IRuleBuilder<T, string> AddWhitespaceRules<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		OdinAlphanumericValidationSettings settings
	)
	{
		if (!settings.AllowWhitespace)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || !Regexes.WhiteSpace.IsMatch(x))
				.WithMessage("'{PropertyName}' must not contain whitespace characters.")
				.WithErrorCode(OdinErrorCodes.Validation.WhitespaceNotAllowed);
		}

		if (!settings.AllowLeadingWhitespace)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || !Regexes.WhitespaceLeading.IsMatch(x))
				.WithMessage("'{PropertyName}' must not contain leading whitespace.")
				.WithErrorCode(OdinErrorCodes.Validation.LeadingWhitespaceNotAllowed);
		}

		if (!settings.AllowTrailingWhitespace)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || !Regexes.WhitespaceTrailing.IsMatch(x))
				.WithMessage("'{PropertyName}' must not contain trailing whitespace.")
				.WithErrorCode(OdinErrorCodes.Validation.TrailingWhitespaceNotAllowed);
		}

		return ruleBuilder;
	}

	private static IRuleBuilder<T, string> AddLengthRules<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		OdinAlphanumericValidationSettings settings
	)
	{
		if (settings.MinLength > 0)
		{
			ruleBuilder
				.OdinMinLength(settings.MinLength, settings.AllowEmpty)
				.WithMessage("'{PropertyName}' must be at least {MinLength} characters long. Current length is {TotalLength} characters.")
				.WithErrorCode(OdinErrorCodes.Validation.MinLength)
				.WithState((_, v) => new FieldErrorLengthData(settings.MinLength, settings.MaxLength, v.Length))
				;
		}

		if (settings.MaxLength != int.MaxValue)
		{
			ruleBuilder
				.MaximumLength(settings.MaxLength)
				.WithMessage("'{PropertyName}' must not exceed {MaxLength} characters long. Current length is {TotalLength} characters.")
				.WithErrorCode(OdinErrorCodes.Validation.MaxLength)
				.WithState((_, v) => new FieldErrorLengthData(settings.MinLength, settings.MaxLength, v.Length))
				;
		}

		return ruleBuilder;
	}

	private static IRuleBuilder<T, string> AddSymbolRules<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		OdinAlphanumericValidationSettings settings
	)
	{
		if (!settings.AllowSymbols)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || !Regexes.Symbols.IsMatch(x))
				.WithMessage("'{PropertyName}' must not contain symbol characters other than '.', ',' or '-'.")
				.WithErrorCode(OdinErrorCodes.Validation.SymbolsNotAllowed);
		}
		else
		{
			if (!settings.AllowLeadingSymbols)
			{
				var regex = settings.AllowDiacriticSymbol
					? Regexes.SymbolsLeadingAllowDiacritic
					: Regexes.SymbolsLeading;

				ruleBuilder
					.Must(x => x.IsNullOrEmpty() || !regex.IsMatch(x))
					.WithMessage("'{PropertyName}' must not contain leading symbol characters.")
					.WithErrorCode(OdinErrorCodes.Validation.LeadingSymbolsNotAllowed);
			}

			if (!settings.AllowTrailingSymbols)
			{
				ruleBuilder
					.Must(x => x.IsNullOrEmpty() || !Regexes.SymbolsTrailing.IsMatch(x))
					.WithMessage("'{PropertyName}' must not contain trailing symbol characters.")
					.WithErrorCode(OdinErrorCodes.Validation.TrailingSymbolsNotAllowed);
			}
		}

		var hasAllowedSymbols = settings.AllowedSymbols != null && settings.AllowedSymbols.Any();
		var hasNonAllowedSymbols = settings.NonAllowedSymbols != null && settings.NonAllowedSymbols.Any();

		if (hasAllowedSymbols && hasNonAllowedSymbols)
			settings.AllowedSymbols = settings.AllowedSymbols.Except(settings.NonAllowedSymbols).ToList();

		if (hasAllowedSymbols)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || Regexes.StringWith(settings.AllowedSymbols).IsMatch(x))
				.WithMessage("'{PropertyName}' must only contain allowed symbols.")
				.WithErrorCode(OdinErrorCodes.Validation.SymbolsNotAllowed)
				.WithState(_ => new Dictionary<string, object>
				{
					["allowedSymbols"] = settings.AllowedSymbols
				})
				;

			return ruleBuilder;
		}

		if (!hasNonAllowedSymbols)
			return ruleBuilder;

		ruleBuilder
			.Must(x => x.IsNullOrEmpty() || !settings.NonAllowedSymbols.Any(x.Contains))
			.WithMessage("'{PropertyName}' must not contain non allowed symbols.")
			.WithErrorCode(OdinErrorCodes.Validation.SymbolsNotAllowed)
			.WithState(_ => new Dictionary<string, object>
			{
				["nonAllowedSymbols"] = settings.NonAllowedSymbols
			})
			;
		return ruleBuilder;
	}

	private static IRuleBuilder<T, string> AddCasingRules<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		OdinAlphanumericValidationSettings settings
	)
	{
		if (!settings.AllowUpperCasing)
		{
			ruleBuilder
				.Must(x => x.IsNullOrEmpty() || !Regexes.UpperCase.IsMatch(x))
				.WithMessage("'{PropertyName}' must not contain upper case characters.")
				.WithErrorCode(OdinErrorCodes.Validation.UpperCaseCharactersNotAllowed);
		}

		return ruleBuilder;
	}

	/// <summary>
	/// Defines validation for Naming.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinNaming<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowLeadingSymbols(false)
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.NamingLength)
				.WithAllowDiacriticSymbol()
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for Field Names.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinFieldName<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowLeadingSymbols()
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.FieldNameLength)
				.WithAllowedSymbols(["_", "$"])
				.WithAllowLeadingNumeric(false)
				.WithAllowDiacriticSymbol(false)
				.WithAllowWhitespace(false)
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for Postcode.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinPostcode<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts
				.WithAllowLeadingWhitespace(false)
				.WithAllowedSymbols(["-"])
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.PostcodeLength)
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for Persona Naming.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinPersonaNaming<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowNumeric(false)
				.WithAllowLeadingSymbols(false)
				.WithAllowedSymbols(new()
				{
					"-",
					".",
					"'",
					"\""
				})
				.WithAllowLeadingWhitespace(false)
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.PersonaNamingLength)
				.WithAllowDiacriticSymbol()
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for ExternalKey like strings
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinExternalKey<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinKey(opts =>
		{
			opts
				.WithMinLength(1)
				.WithAllowEmpty(false);

			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for Key like strings e.g. Key etc...
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinKey<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowTrailingSymbols(false)
				.WithAllowWhitespace(false)
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.KeyLength)
				.WithNonAllowedSymbols(new()
				{
					"^",
					"*",
					"(",
					")",
					"{",
					"}",
					"[",
					"]",
					"'",
					"\"",
					"/",
					"~",
					"@",
					"=",
					"<",
					">"
				})
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for Slug
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinSlug<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinKey(opts =>
		{
			opts.WithAllowMimeTypeExtensionLike(false)
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for Guid like strings e.g. primary/foreign keys etc...
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinGuidString<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinKey(opts =>
		{
			opts.WithAllowLeadingNumeric()
				.WithMinLength(1)
				.WithMaxLength(OdinPropFields.GuidLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinId<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinKey(opts =>
		{
			opts.WithAllowLeadingNumeric()
				.WithMinLength(1)
				.WithMaxLength(OdinPropFields.IdLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinCompositeId<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinId(opts =>
		{
			opts.WithMaxLength(OdinPropFields.CompositeIdLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinFilePath<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinKey(
			opts =>
			{
				opts.WithAllowWhitespace()
					.WithMaxLength(OdinPropFields.FilePathLength)
					.WithNonAllowedSymbols(
						new()
						{
							"?",
							"*",
							"\"",
							"<",
							">",
							"|"
						}
					);
				configure?.Invoke(opts);
			}
		);

	public static IRuleBuilder<T, string> OdinFileName<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinNaming(opts =>
		{
			opts.WithAllowLeadingSymbols()
				.WithAllowLeadingNumeric()
				.WithMinLength(1)
				.WithMaxLength(OdinPropFields.FileNameLength)
				.WithNonAllowedSymbols(
					new()
					{
						"\"",
						"/",
						":",
						"?",
						"*",
						"\"",
						"<",
						">",
						"|"
					}
				);
			configure?.Invoke(opts);
		});

	public static IRuleBuilderOptions<T, string> Url<T>(this IRuleBuilder<T, string> ruleBuilder)
		=> ruleBuilder.Must(url =>
			{
				if (url.IsNullOrEmpty())
					return true;
				return Uri.TryCreate(url, UriKind.Absolute, out var outUri) &&
					   (outUri.Scheme != Uri.UriSchemeHttp || outUri.Scheme != Uri.UriSchemeHttps)
					;
			})
			.WithErrorCode(OdinErrorCodes.Validation.InvalidUrl)
			.WithMessage("Url is invalid.");

	public static IRuleBuilderOptions<T, string> CountryCode<T>(this IRuleBuilder<T, string> ruleBuilder, ICountryService? service = null)
		=> ruleBuilder.Must(value =>
			{
				if (value.IsNullOrEmpty())
					return true;

				if (value.Length > OdinPropFields.CountryCodeLength)
					return false;

				service ??= _countryService;

				return service.GetByCodeIso2OrDefault(value) != null
					;
			})
			.WithErrorCode(OdinErrorCodes.Invalid)
			.WithMessage("Country code is invalid.");

	public static IRuleBuilder<T, string> OdinPhonePrefix<T>(this IRuleBuilder<T, string> ruleBuilder)
		=> ruleBuilder
			.Must(x => x.IsNullOrEmpty() || !Regexes.WhitespaceLeading.IsMatch(x))
			.Matches(@"^[+]\d{1,3}(?!\s)$")
			.WithErrorCode(OdinErrorCodes.Validation.InvalidPhonePrefix);

	public static IRuleBuilder<T, FileModel> File<T>(
		this IRuleBuilder<T,
			FileModel> ruleBuilder,
		double maxSizeMb,
		HashSet<string> supportedMimeTypes,
		double? minSizeMb = null
	) => ruleBuilder.SetValidator(new FileModelValidator(maxSizeMb, supportedMimeTypes, minSizeMb));

	public static IRuleBuilder<T, FileModel> File<T>(this IRuleBuilder<T, FileModel> ruleBuilder, FileUploadConfig config)
		=> ruleBuilder.File(
			config.FileMaxSizeLimitMb.GetValueOrDefault(5),
			config.SupportedMimeTypes,
			config.FileMinSizeLimitMb
		);

	public static IRuleBuilder<T, string> OdinJsonString<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowLeadingSymbols()
				.WithAllowLeadingNumeric(false)
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.JsonLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinReason<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinContentLong(opts =>
		{
			opts.WithMaxLength(OdinPropFields.ReasonLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinComment<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinContentLong(opts =>
		{
			opts.WithMaxLength(OdinPropFields.CommentLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinDescription<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinContentLong(opts =>
		{
			opts.WithMaxLength(OdinPropFields.DescriptionLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinContentShort<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinContentLong(opts =>
		{
			opts.WithMaxLength(OdinPropFields.ContentShortLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinContentLong<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowLeadingSymbols()
				.WithMaxLength(OdinPropFields.ContentLongLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilder<T, string> OdinCorrelationIdString<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinGuidString(opts =>
		{
			opts.WithMaxLength(OdinPropFields.CorrelationIdLength)
				;
			configure?.Invoke(opts);
		});

	public static IRuleBuilderOptions<T, TimeSpan?> OdinDuration<T>(this IRuleBuilder<T, TimeSpan?> ruleBuilder)
		=> ruleBuilder.Must(_ => true) // todo: implement proper validation
			.WithErrorCode(OdinErrorCodes.Validation.InvalidDuration)
			.WithMessage("Duration is invalid.");

	/// <summary>
	/// Defines a length validator on the current rule builder, but only for string properties.
	/// Validation will fail if the length of the string is less than the length specified (ignores empty string).
	/// </summary>
	/// <typeparam name="T">Type of object being validated</typeparam>
	/// <param name="ruleBuilder">The rule builder on which the validator should be defined</param>
	/// <param name="minimumLength"></param>
	/// <param name="allowEmpty">If set to true, empty strings are considered valid</param>
	/// <returns></returns>
	public static IRuleBuilderOptions<T, string> OdinMinLength<T>(this IRuleBuilder<T, string> ruleBuilder, int minimumLength, bool allowEmpty)
		=> ruleBuilder.SetValidator(new OdinMinLengthValidator<T>(minimumLength, allowEmpty));

	public static async Task<ValidationResult> ValidateAsync<T>(
		this IValidator<T> @this,
		T instance,
		IEnumerable<string> properties,
		IEnumerable<string> ruleSets,
		CancellationToken cancellationToken = default)
	{
		var memberNameValidator = ValidatorOptions
			.Global
			.ValidatorSelectors
			.MemberNameValidatorSelectorFactory(properties);

		var ruleSetValidator = ValidatorOptions
			.Global
			.ValidatorSelectors
			.RulesetValidatorSelectorFactory(ruleSets);

		var validationContext = new ValidationContext<T>(
			instance,
			new(),
			new CompositeValidatorSelector(new[] {
				memberNameValidator,
				ruleSetValidator
			}));

		var validationResult = await @this.ValidateAsync(validationContext, cancellationToken);

		return validationResult;
	}

	/// <summary>
	/// Defines validation for Address.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinAddressLine<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowLeadingSymbols(false)
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.AddressLineLength)
				.WithAllowDiacriticSymbol()
				;
			configure?.Invoke(opts);
		});

	/// <summary>
	/// Defines validation for City.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IRuleBuilder<T, string> OdinCity<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		Action<OdinAlphanumericValidationSettings>? configure = null
	) => ruleBuilder
		.OdinAlphanumeric(opts =>
		{
			opts.WithAllowLeadingSymbols(false)
				.WithMinLength(2)
				.WithMaxLength(OdinPropFields.CityLength)
				.WithAllowDiacriticSymbol()
				;
			configure?.Invoke(opts);
		});

	public static async Task<ValidationContextableResult<T>> ValidateWithContextAsync<T>(
		this IValidator<T> @this,
		T instance,
		Action<ValidationStrategy<T>> options,
		CancellationToken cancellationToken = default
	)
	{
		var context = ValidationContext<T>.CreateWithOptions(instance, options);
		var validationResult = await @this.ValidateAsync(context, cancellationToken);

		return new()
		{
			Context = context,
			Result = validationResult
		};
	}

	public static ValidationStrategy<T> IncludeUpdateRuleSet<T>(this ValidationStrategy<T> validationStrategy)
	{
		validationStrategy.IncludeRuleSets(UpdateRuleSetName);
		return validationStrategy;
	}

	public static ValidationStrategy<T> IncludeCreateRuleSet<T>(this ValidationStrategy<T> validationStrategy)
	{
		validationStrategy.IncludeRuleSets(CreateRuleSetName);
		return validationStrategy;
	}

	public static IRuleBuilderOptions<T, string> OdinRequired<T>(
		this AbstractValidator<T> validator,
		Expression<Func<T, string>> exp
	)
		=> validator.RuleFor(exp)
			.Must(x => !x.IsNullOrEmpty())
			.WithErrorCode(OdinErrorCodes.Required);

	/// <summary>
	/// Checks that the filter (And &amp; Or) only contains the allowed fields.
	/// </summary>
	public static IRuleBuilderOptions<T, OdinFilterInput> MustOnlyHave<T>(
		this IRuleBuilder<T, OdinFilterInput> ruleBuilder,
		List<string> allowedFields
	)
		=> ruleBuilder
			.Must(
				filter =>
				{
					var andFields = filter.And.Select(andFilter => andFilter.Key);
					var orFields = filter.Or.Select(orFilter => orFilter.Key);
					var allFields = andFields.Union(orFields);

					return !allFields.Except(allowedFields, StringComparer.OrdinalIgnoreCase).Any();
				}
			)
			.WithName(OdinErroneousParts.Filter)
			.WithErrorCode(OdinErrorCodes.InvalidRequest)
			.WithMessage($"Only the following fields are allowed in the filter: [{allowedFields.JoinTokens(", ")}]")
		;

	/// <summary>
	/// Checks that the filter <br/>
	/// - contains the specified <see cref="field"/> <br/>
	/// - that it is not empty, and <br/>
	/// - that it has the required amount of items (if <see cref="requiredAmount"/> is specified).
	/// </summary>
	public static IRuleBuilderOptions<T, OdinFilterInput> MustHave<T>(
		this IRuleBuilder<T, OdinFilterInput> ruleBuilder,
		string field,
		int? requiredAmount = null
	)
	{
		ruleBuilder
			.Must(x => !x.Find(field).IsNullOrEmpty())
			.When(_ => requiredAmount == null)
			.WithName(OdinErroneousParts.Filter)
			.WithErrorCode(OdinErrorCodes.Required)
			.WithMessage(OdinErrorMessages.RequiredFieldTemplate.FromTemplate(new Dictionary<string, object> { ["PropertyName"] = field }))
			;

		var options = ruleBuilder
			.Must(
				x =>
				{
					var filterOperatorInputs = x.Find(field);
					var hasField = !filterOperatorInputs.IsNullOrEmpty();

					if (!hasField)
						return false;

					var items = filterOperatorInputs.SelectMany(input => input.Items).ToHashSet();
					return items.Count == requiredAmount;
				})
			.When(_ => requiredAmount != null)
			.WithName(OdinErroneousParts.Filter)
			.WithErrorCode(OdinErrorCodes.InvalidAmount)
			.WithMessage($"Cannot have less/more than {requiredAmount} of '{field}'")
			;

		return options;
	}

	/// <param name="ruleBuilder">The rule builder.</param>
	/// <typeparam name="T">The type being validated.</typeparam>
	/// <typeparam name="TElement">The type of elements in the collection.</typeparam>
	extension<T, TElement>(IRuleBuilder<T, IEnumerable<TElement>> ruleBuilder)
	{
		/// <summary>
		/// Validates that all items in a collection are contained within the specified set of valid values.
		/// </summary>
		/// <param name="allowedValues">A collection of valid values that elements must be contained in.</param>
		/// <returns>A rule builder options to continue the validation chain.</returns>
		/// <remarks>
		/// This extension validates that every item in the target collection exists in the provided set of valid values.
		/// If any invalid items are found, the error message will list both the invalid items and the complete set of valid values.
		/// </remarks>
		public IRuleBuilderOptions<T, IEnumerable<TElement>> OdinIn(
			IEnumerable<TElement> allowedValues
		)
		{
			var validSet = allowedValues.ToHashSet();

			var options = ruleBuilder
					.Must(items => items.All(validSet.Contains))
					.WithErrorCode(OdinErrorCodes.Invalid)
					.WithMessage((_, items) =>
					{
						var invalidItems = items.Where(item => !validSet.Contains(item)).ToList();
						return $"'{{PropertyName}}' contains invalid values: '{invalidItems.ToDebugString()}'.";
					}).WithState(_ => new Dictionary<string, object>
					{
						["allowedValues"] = validSet
					})
				;

			return options;
		}
	}
}
