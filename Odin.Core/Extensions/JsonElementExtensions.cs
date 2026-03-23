using System.Text.Json;
using Odin.Core.Error;
using Odin.Core.Json;

namespace Odin.Core;

public static class JsonElementExtensions
{
	/// <param name="element">Json element to convert.</param>
	extension(JsonElement element)
	{
		/// <summary>
		/// Converts <see cref="JsonElement"/> to a specified type with validation.
		/// </summary>
		/// <typeparam name="T">Type to convert to.</typeparam>
		/// <param name="propName">Property name for error reporting.</param>
		/// <returns>Returns the value converted.</returns>
		/// <exception cref="ApiErrorException">Thrown when conversion fails.</exception>
		public T? ToValue<T>(string? propName = null)
		{
			var result = element.ToValue(typeof(T), propName);
			return result == null ? default : (T)result;
		}

		/// <summary>
		/// Converts <see cref="JsonElement"/> to a specified type with validation.
		/// </summary>
		/// <param name="targetType">Type to convert to.</param>
		/// <param name="propName">Property name for error reporting.</param>
		/// <returns>Returns the value converted.</returns>
		/// <exception cref="ApiErrorException">Thrown when conversion fails.</exception>
		public object? ToValue(Type targetType, string? propName = null)
		{
			if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
				return null;

			var cardinalType = targetType.GetCardinalType();

			try
			{
				if (cardinalType == typeof(string))
					return element.GetString();

				if (cardinalType == typeof(bool))
					return element.ValueKind switch
					{
						JsonValueKind.True => true,
						JsonValueKind.False => false,
						JsonValueKind.String => bool.Parse(element.GetString()!),
						_ => throw new InvalidCastException()
					};

				if (cardinalType == typeof(int))
					return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : int.Parse(element.GetString()!);

				if (cardinalType == typeof(long))
					return element.ValueKind == JsonValueKind.Number ? element.GetInt64() : long.Parse(element.GetString()!);

				if (cardinalType == typeof(double))
					return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : double.Parse(element.GetString()!);

				if (cardinalType == typeof(decimal))
					return element.ValueKind == JsonValueKind.Number ? element.GetDecimal() : decimal.Parse(element.GetString()!);

				if (cardinalType == typeof(DateTime))
				{
					if (element.ValueKind == JsonValueKind.String)
					{
						if (element.TryGetDateTime(out var dt))
							return dt;
						return DateTime.Parse(element.GetString()!);
					}
					if (element.ValueKind == JsonValueKind.Number)
						return element.GetInt64().ToDateTimeFromUnix();
					throw new InvalidCastException();
				}

				if (cardinalType == typeof(DateOnly))
				{
					if (element.ValueKind == JsonValueKind.String)
					{
						return DateOnly.Parse(element.GetString()!);
					}
					if (element.ValueKind == JsonValueKind.Number)
						return element.GetInt64().ToDateOnlyFromUnix();
					throw new InvalidCastException();
				}

				if (cardinalType == typeof(Guid))
				{
					if (element.ValueKind == JsonValueKind.String)
					{
						if (element.TryGetGuid(out var g))
							return g;
						return Guid.Parse(element.GetString()!);
					}
					throw new InvalidCastException();
				}

				if (cardinalType.IsEnum)
				{
					var valueStr = element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString() : element.GetString();
					if (Enum.TryParse(cardinalType, valueStr, true, out var enumValue))
						return enumValue;
					throw new InvalidCastException();
				}

				return JsonSerializer.Deserialize(element, targetType, JsonUtils.JsonBasicSettings);
			}
			catch (Exception)
			{
				throw GetErrorResult().AsApiErrorException();
			}

			ErrorResult GetErrorResult()
			{
				var errorResult = ErrorResult.AsValidationError();
				var value = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();

				if (!propName.IsNullOrEmpty())
					errorResult.AddField(propName, x => ErrorFieldBuilderContextExtensions.AsInvalid(x, (object?)value)
						.WithErrorMessage($"Cannot convert '{value}' to '{cardinalType}'.")
					);
				else
					errorResult.FieldErrors.AddError("error", x => ErrorFieldBuilderContextExtensions.AsInvalid(x, (object?)value)
						.WithErrorMessage($"Cannot convert '{value}' to '{cardinalType}'.")
					);

				return errorResult;
			}
		}
	}
}
