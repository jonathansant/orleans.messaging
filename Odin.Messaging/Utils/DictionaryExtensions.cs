using Humanizer;
using Newtonsoft.Json.Linq;

namespace Odin.Messaging.Utils;

public static class DictionaryExtensions
{
	public static TResult ToObject<TResult, TValue>(
		this IDictionary<string, TValue> source,
		bool throwIfNotFound = false,
		Func<string, string>? keyTransform = null
	) where TResult : new()
	{
		object obj = new TResult();
		var objType = obj.GetType();
		keyTransform ??= InflectorExtensions.Pascalize;

		foreach (var item in source)
		{
			var propName = keyTransform(item.Key);
			var prop = objType.GetProperty(propName);

			if (prop == null)
			{
				if (throwIfNotFound)
					throw new MissingMemberException(objType.GetDemystifiedName(), item.Key);
				continue;
			}

			if (prop.PropertyType.IsEnum)
			{
				var enumValue = item.Value is string strValue
					? Enum.Parse(prop.PropertyType, strValue, ignoreCase: true)
					: Enum.ToObject(prop.PropertyType, item.Value);
				prop.SetValue(obj, enumValue, null);
			}
			else if (prop.PropertyType.IsNullableEnum())
			{
				var enumValue = item.Value is string strValue
					? Enum.Parse(Nullable.GetUnderlyingType(prop.PropertyType), strValue, ignoreCase: true)
					: Enum.ToObject(Nullable.GetUnderlyingType(prop.PropertyType), item.Value);
				prop.SetValue(obj, enumValue, null);
			}
			else if (typeof(List<string>).IsAssignableFrom(prop.PropertyType) && item.Value is string valueStr)
			{
				if (valueStr.IsNullOrEmpty())
					continue;

				var value = valueStr.Split(',').ToList();
				prop.SetValue(obj, value, null);
			}
			else if (typeof(Dictionary<string, string>).IsAssignableFrom(prop.PropertyType) && item.Value is string valueDicStr)
			{
				if (valueDicStr.IsNullOrEmpty())
					continue;

				var value = valueDicStr.Split(',')
					.Select(
						x =>
						{
							var arr = x.Split('=');
							return new
							{
								Key = arr[0],
								Value = arr[1]
							};
						}
					)
					.ToDictionary(x => x.Key, x => x.Value);
				prop.SetValue(obj, value, null);
			}
			else if (item.Value is JArray array)
			{
				var listType = prop.PropertyType;
				var value = array.ToObject(listType);
				prop.SetValue(obj, value, null);
			}
			else if (item.Value is not null
					 && prop.PropertyType.IsNumericType()
					 && (item.Value.GetType().IsNumericType() || item.Value is string))
			{
				var targetType = prop.PropertyType.GetCardinalType();
				var value = Convert.ChangeType(item.Value, targetType);
				prop.SetValue(obj, value, null);
			}
			else if (prop.GetSetMethod() != null)
			{
				prop.SetValue(obj, item.Value, null);
			}
		}

		return (TResult)obj;
	}

	private static string ToPascalCase(string str) => str.Pascalize();
}
