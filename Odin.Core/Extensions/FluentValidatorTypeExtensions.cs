using FluentValidation;
using System.Collections.Concurrent;

namespace Odin.Core;

/// <summary>
/// Extension methods for extracting type information from validators.
/// </summary>
public static class FluentValidatorTypeExtensions
{
	private static readonly ConcurrentDictionary<Type, Type?> ValidatorInputTypeCache = new();

	/// <summary>
	/// Extracts the input type (T) from a validator that inherits from AbstractValidator&lt;T&gt;.
	/// This method walks up the inheritance chain to find AbstractValidator&lt;T&gt; even if
	/// the validator inherits from an intermediate validator class.
	/// Results are cached for improved performance on subsequent calls.
	/// </summary>
	/// <param name="validatorType">The validator type to extract the input type from.</param>
	/// <returns>The input type (T) if found; otherwise, null.</returns>
	/// <example>
	/// Given: TagInputCustomValidator : TagInputValidator : AbstractValidator&lt;TagInput&gt;
	/// Returns: typeof(TagInput)
	/// </example>
	public static Type? ExtractInputTypeFromAbstractValidator(this Type validatorType)
	{
		if (validatorType == null)
			throw new ArgumentNullException(nameof(validatorType));

		return ValidatorInputTypeCache.GetOrAdd(validatorType, static type =>
		{
			var currentType = type;
			while (currentType != null && currentType != typeof(object))
			{
				if (currentType.IsGenericType)
				{
					var genericTypeDef = currentType.GetGenericTypeDefinition();

					if (genericTypeDef == typeof(AbstractValidator<>))
					{
						// Return the generic argument (the T in AbstractValidator<T>)
						return currentType.GetGenericArguments()[0];
					}
				}

				currentType = currentType.BaseType;
			}

			return null;
		});
	}

	/// <summary>
	/// Extracts the input type (T) from a validator instance that inherits from AbstractValidator&lt;T&gt;.
	/// </summary>
	/// <param name="validator">The validator instance to extract the input type from.</param>
	/// <returns>The input type (T) if found; otherwise, null.</returns>
	public static Type? ExtractInputTypeFromAbstractValidator(this object validator)
	{
		if (validator == null)
			throw new ArgumentNullException(nameof(validator));

		return validator.GetType().ExtractInputTypeFromAbstractValidator();
	}
}

