using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Odin.Core.Json;

[AttributeUsage(AttributeTargets.Property)]
public class HashIgnoreAttribute : Attribute;

public sealed class HashIgnoreResolver : DefaultJsonTypeInfoResolver
{
	private static readonly ConcurrentDictionary<Type, Func<JsonTypeInfo, JsonTypeInfo>> TypeProcessors = new();

	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var typeInfo = base.GetTypeInfo(type, options);

		if (typeInfo.Kind != JsonTypeInfoKind.Object)
			return typeInfo;

		var processor = GetTypeProcessor(type);
		return processor(typeInfo);

	}

	private static Func<JsonTypeInfo, JsonTypeInfo> GetTypeProcessor(Type type)
		=> TypeProcessors.GetOrAdd(type, static t =>
		{
			var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var ignoredProperties = properties.Where(prop => prop.GetCustomAttribute<HashIgnoreAttribute>() != null)
				.Select(x => x.Name)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			return typeInfo =>
			{
				foreach (var property in typeInfo.Properties)
					if (ignoredProperties.Contains(property.Name))
						property.ShouldSerialize = (_, _) => false;

				return typeInfo;
			};
		});
}
