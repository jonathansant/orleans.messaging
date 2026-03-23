using FluentValidation;
using Odin.CodeAnnotations;
using System.Numerics;

namespace Odin.Core.Validation;

public static class NumericFluentValidationExtensions
{
	private static readonly NumericInfo DecimalDefaults = new()
	{
		Min = -decimal.MaxValue,
		Max = decimal.MaxValue,
		// 1_000_000_000.000
		Precision = 13,
		Scale = 3
	};

	public static IRuleBuilder<T, decimal> OdinNumeric<T>(
		this IRuleBuilder<T, decimal> ruleBuilder,
		Action<NumericInfo>? configure = null
	)
	{
		var settings = DecimalDefaults;

		if (configure != null)
		{
			settings = settings with { };
			configure(settings);
		}

		return ruleBuilder.OdinNumeric(settings);
	}

	public static IRuleBuilder<T, decimal?> OdinNumeric<T>(
		this IRuleBuilder<T, decimal?> ruleBuilder,
		Action<NumericInfo>? configure = null
	)
	{
		var settings = DecimalDefaults;

		if (configure != null)
		{
			settings = settings with { };
			configure(settings);
		}

		return ruleBuilder.OdinNumeric(settings);
	}

	public static IRuleBuilder<T, decimal> OdinNumeric<T>(
		this IRuleBuilder<T, decimal> ruleBuilder,
		NumericInfo settings
	)
	{
		ruleBuilder
			.OdinNumeric<T, decimal>(settings);

		if (settings.Precision.HasValue)
			ruleBuilder
				.PrecisionScale(settings.Precision.Value, settings.Scale!.Value, true)
				.WithErrorCode(OdinErrorCodes.Validation.NumericPrecisionExceeded)
				.WithState((_, _) => settings)
				;

		return ruleBuilder;
	}

	public static IRuleBuilder<T, decimal?> OdinNumeric<T>(
		this IRuleBuilder<T, decimal?> ruleBuilder,
		NumericInfo settings
	)
	{
		ruleBuilder
			.OdinNumeric<T, decimal>(settings);

		if (settings.Precision.HasValue)
			ruleBuilder
				.PrecisionScale(settings.Precision.Value, settings.Scale!.Value, true)
				.WithErrorCode(OdinErrorCodes.Validation.NumericPrecisionExceeded)
				.WithState((_, _) => settings)
				;

		return ruleBuilder;
	}

	public static IRuleBuilder<T, TProperty> OdinNumeric<T, TProperty>(
		this IRuleBuilder<T, TProperty> ruleBuilder,
		Action<NumericInfo>? configure = null
	) where TProperty : struct, INumber<TProperty>, IMinMaxValue<TProperty>
	{
		var settings = new NumericInfo
		{
			Min = -TProperty.MinValue,
			Max = TProperty.MaxValue,
		};

		configure?.Invoke(settings);

		return ruleBuilder.OdinNumeric(settings);
	}

	public static IRuleBuilder<T, TProperty?> OdinNumeric<T, TProperty>(
		this IRuleBuilder<T, TProperty?> ruleBuilder,
		Action<NumericInfo>? configure = null
	) where TProperty : struct, INumber<TProperty>, IMinMaxValue<TProperty>
	{
		var settings = new NumericInfo
		{
			Min = -TProperty.MinValue,
			Max = TProperty.MaxValue,
		};

		configure?.Invoke(settings);

		return ruleBuilder.OdinNumeric(settings);
	}

	public static IRuleBuilder<T, TProperty> OdinNumeric<T, TProperty>(
		this IRuleBuilder<T, TProperty> ruleBuilder,
		NumericInfo settings
	) where TProperty : struct, INumber<TProperty>, IMinMaxValue<TProperty>
	{
		var min = (TProperty)Convert.ChangeType(settings.Min, typeof(TProperty))!;
		var max = (TProperty)Convert.ChangeType(settings.Max, typeof(TProperty))!;

		if (!settings.AllowZero)
			ruleBuilder
				.Must(x => x != TProperty.Zero)
				.WithErrorCode(OdinErrorCodes.Validation.ZeroNotAllowed)
				.WithMessage("'{PropertyName}' must not be zero.")
				.WithState((_, _) => settings)
				;

		ruleBuilder
			.InclusiveBetween(min, max)
			.WithErrorCode(OdinErrorCodes.Validation.NumericRange)
			.WithState((_, _) => settings)
			;

		return ruleBuilder;
	}

	public static IRuleBuilder<T, TProperty?> OdinNumeric<T, TProperty>(
		this IRuleBuilder<T, TProperty?> ruleBuilder,
		NumericInfo settings
	) where TProperty : struct, INumber<TProperty>, IMinMaxValue<TProperty>
	{
		var min = (TProperty)Convert.ChangeType(settings.Min, typeof(TProperty))!;
		var max = (TProperty)Convert.ChangeType(settings.Max, typeof(TProperty))!;

		if (!settings.AllowZero)
			ruleBuilder
				.Must(x =>
					{
						if (!x.HasValue)
							return true;
						return x != TProperty.Zero;
					}
				)
				.WithMessage("'{PropertyName}' must not be zero.")
				.WithErrorCode(OdinErrorCodes.Validation.ZeroNotAllowed)
				.WithState((_, _) => settings)
				;

		ruleBuilder
			.InclusiveBetween(min, max)
			.WithErrorCode(OdinErrorCodes.Validation.NumericRange)
			.WithState((_, _) => settings)
			;

		return ruleBuilder;
	}
}
