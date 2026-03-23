using Humanizer;
using Odin.Core.Error;
using System.Reflection;
using System.Runtime.Serialization;

namespace Odin.Core;

public static class EnumExtensions
{
	public static string GetAlias<T>(this T value)
		where T : Enum
	{
		var type = value.GetType();
		if (!type.IsEnum)
			throw new ArgumentException("Value must be of Enum type", nameof(value));

		var memberInfo = type.GetMember(value.ToString());

		if (memberInfo.Length <= 0)
			return value.ToString().ToLowerInvariant();

		var attrs = memberInfo.First().GetCustomAttributes(typeof(AliasAttribute), false);
		return attrs.Length > 0
			? ((AliasAttribute)attrs.First()).Alias
			: value.ToString().ToLowerInvariant();
	}

	public static string GetEnumMemberValue<TEnum>(this TEnum value)
		where TEnum : Enum
		=> value.GetType().GetEnumMemberValue(value.ToString());

	public static object GetEnumMemberValue(this Type type, object value)
		=> type.GetEnumMemberValue(value.ToString());

	public static string GetEnumMemberValue(this Type type, string value)
	{
		if (!type.IsEnum)
		{
			if (type.IsNullable())
			{
				var underlyingType = Nullable.GetUnderlyingType(type);
				if (underlyingType!.IsEnum)
					return underlyingType.GetEnumMemberValue(value);
			}

			throw new ArgumentException("Value must be of Enum type", nameof(value));
		}

		var memberInfo = type.GetMember(value!).FirstOrDefault() ?? throw new ArgumentOutOfRangeException(
			nameof(value),
			value,
			$"Invalid enum value '{value}', member not found."
		);
		return memberInfo.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? value;
	}

	public static Dictionary<string, int> ToDictionary<T>()
		where T : struct, Enum
		=> Enum.GetValues<T>().ToDictionary(x => Enum.GetName(x)!.Camelize(), x => (int)(object)(x));

	public static IEnumerable<TEnum> ToEnumList<TEnum>(this IEnumerable<string> stringEnums, string propName)
	{
		var invalidStrings = new List<string>();
		var parsed = new List<TEnum>();
		foreach (var stringEnum in stringEnums)
		{
			if (Enum.TryParse(typeof(TEnum), stringEnum, true, out var enumValue))
				parsed.Add((TEnum)enumValue);
			else
				invalidStrings.Add(stringEnum);
		}

		if (invalidStrings.Any())
			throw ErrorResult.AsValidationError()
				.AddField(propName, x => x.AsInvalid(invalidStrings, typeof(TEnum)))
				.AsApiErrorException();

		return parsed;
	}
}
