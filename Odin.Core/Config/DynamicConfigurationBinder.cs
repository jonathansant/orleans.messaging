using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Reflection;

namespace Odin.Core.Config;

/// <summary>
/// Static helper class that allows binding strongly typed objects to configuration values.
/// </summary>
public static class DynamicConfigurationBinder
{
	public static DynamicSection? GetDynamic(this IConfiguration configuration)
		=> configuration.GetDynamic<DynamicSection>();

	/// <summary>
	/// Attempts to bind the configuration instance to a new instance of type T.
	/// If this configuration section has a value, that will be used.
	/// Otherwise, binding by matching property names against configuration keys recursively.
	/// If it recursively finds <see cref="DynamicSection"/> it will bind that property
	/// to a DynamicSection.
	/// </summary>
	/// <typeparam name="T">The type of the new instance to bind.</typeparam>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <returns>The new instance of T if successful, default(T) otherwise.</returns>
	public static T? GetDynamic<T>(this IConfiguration configuration)
	{
		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));
		var obj = configuration.Get(typeof(T));
		if (obj == null)
			return default;
		return (T)obj;
	}

	/// <summary>
	/// Attempts to bind the configuration instance to a new instance of type T.
	/// If this configuration section has a value, that will be used.
	/// Otherwise, binding by matching property names against configuration keys recursively.
	/// </summary>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="type">The type of the new instance to bind.</param>
	/// <returns>The new instance if successful, null otherwise.</returns>
	public static object? Get(this IConfiguration configuration, Type type)
	{
		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));
		return BindInstance(type, null, configuration);
	}

	private static void BindNonScalar(this IConfiguration configuration, object? instance)
	{
		if (instance == null)
			return;

		foreach (var allProperty in GetAllProperties(instance.GetType().GetTypeInfo()))
			BindProperty(allProperty, instance, configuration);
	}

	private static void BindProperty(PropertyInfo property, object instance, IConfiguration config)
	{
		if (property.GetMethod == null || !property.GetMethod.IsPublic || property.GetMethod.GetParameters().Length != 0)
			return;
		var value = property.GetValue(instance);
		var flag = property.SetMethod != null && property.SetMethod.IsPublic;
		if (value == null && !flag)
			return;
		var obj = BindInstance(property.PropertyType, value, config.GetSection(property.Name));
		if (!(obj != null & flag))
			return;
		property.SetValue(instance, obj);
	}

	private static object BindToCollection(TypeInfo typeInfo, IConfiguration config)
	{
		var type = typeof(List<>).MakeGenericType(typeInfo.GenericTypeArguments[0]);
		var instance = Activator.CreateInstance(type);
		BindCollection(instance, type, config);
		return instance;
	}

	private static object? AttemptBindToCollectionInterfaces(Type type, IConfiguration config)
	{
		var typeInfo = type.GetTypeInfo();
		if (!typeInfo.IsInterface)
			return null;
		if (FindOpenGenericInterface(typeof(IReadOnlyList<>), type) != null)
			return BindToCollection(typeInfo, config);
		if (FindOpenGenericInterface(typeof(IReadOnlyDictionary<,>), type) != null)
		{
			var type1 = typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0], typeInfo.GenericTypeArguments[1]);
			var instance = Activator.CreateInstance(type1);
			BindDictionary(instance, type1, config);
			return instance;
		}
		var genericInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
		if (genericInterface != null)
		{
			var instance = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0], typeInfo.GenericTypeArguments[1]));
			BindDictionary(instance, genericInterface, config);
			return instance;
		}
		if (FindOpenGenericInterface(typeof(IReadOnlyCollection<>), type) != null || FindOpenGenericInterface(typeof(ICollection<>), type) != null || FindOpenGenericInterface(typeof(IEnumerable<>), type) != null)
			return BindToCollection(typeInfo, config);
		return null;
	}

	private static object? BindInstance(Type type, object? instance, IConfiguration? config)
	{
		if (type == typeof(IConfigurationSection))
			return config;

		if (type == typeof(DynamicSection))
			return new DynamicSection(config);

		var str = (config as IConfigurationSection)?.Value;
		if (str != null && TryConvertValue(type, str, out var result, out var error))
		{
			if (error != null)
				throw error;
			return result;
		}
		if (config != null && config.GetChildren().Any())
		{
			if (instance == null)
			{
				instance = AttemptBindToCollectionInterfaces(type, config);
				if (instance != null)
					return instance;
				instance = CreateInstance(type);
			}
			var genericInterface1 = FindOpenGenericInterface(typeof(IDictionary<,>), type);
			if (genericInterface1 != null)
				BindDictionary(instance, genericInterface1, config);
			else if (type.IsArray)
			{
				instance = BindArray((Array)instance, config);
			}
			else
			{
				var genericInterface2 = FindOpenGenericInterface(typeof(ICollection<>), type);
				if (genericInterface2 != null)
					BindCollection(instance, genericInterface2, config);
				else
					config.BindNonScalar(instance);
			}
		}
		return instance;
	}

	private static object CreateInstance(Type type)
	{
		var typeInfo = type.GetTypeInfo();
		if (typeInfo.IsInterface || typeInfo.IsAbstract)
			throw new InvalidOperationException($"Cannot activate abstract or interface: {type.FullName}");
		if (type.IsArray)
		{
			if (typeInfo.GetArrayRank() > 1)
				throw new InvalidOperationException($"Unsupported multi-dimensional array: {type.FullName}");
			return Array.CreateInstance(typeInfo.GetElementType()!, 0);
		}
		var declaredConstructors = typeInfo.DeclaredConstructors;
		var func = (Func<ConstructorInfo, bool>)(ctor =>
		{
			if (ctor.IsPublic)
				return ctor.GetParameters().Length == 0;
			return false;
		});
		bool Predicate(ConstructorInfo info) => true;
		if (!declaredConstructors.Any(Predicate))
			throw new InvalidOperationException($"Missing parameterless constructor: {type.FullName}");
		try
		{
			return Activator.CreateInstance(type);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to instantiate: {type.FullName}", ex);
		}
	}

	private static void BindDictionary(object? dictionary, Type dictionaryType, IConfiguration config)
	{
		var typeInfo = dictionaryType.GetTypeInfo();
		var genericTypeArgument1 = typeInfo.GenericTypeArguments[0];
		var genericTypeArgument2 = typeInfo.GenericTypeArguments[1];
		var isEnum = genericTypeArgument1.GetTypeInfo().IsEnum;
		if (genericTypeArgument1 != typeof(string) && !isEnum)
			return;
		var declaredMethod = typeInfo.GetDeclaredMethod("Add")!;
		foreach (var child in config.GetChildren())
		{
			var obj = BindInstance(genericTypeArgument2, null, child);
			if (obj != null)
			{
				if (genericTypeArgument1 == typeof(string))
				{
					var key = child.Key;
					declaredMethod.Invoke(dictionary, [key, obj]);
				}
				else if (isEnum)
				{
					var int32 = Convert.ToInt32(Enum.Parse(genericTypeArgument1, child.Key));
					declaredMethod.Invoke(dictionary, [int32, obj]);
				}
			}
		}
	}

	private static void BindCollection(object collection, Type collectionType, IConfiguration config)
	{
		var typeInfo = collectionType.GetTypeInfo();
		var genericTypeArgument = typeInfo.GenericTypeArguments[0];
		var declaredMethod = typeInfo.GetDeclaredMethod("Add")!;
		foreach (var child in config.GetChildren())
		{
			try
			{
				var obj = BindInstance(genericTypeArgument, null, child);
				if (obj != null)
					declaredMethod.Invoke(collection, [obj]);
			}
			catch
			{
			}
		}
	}

	private static Array BindArray(Array source, IConfiguration config)
	{
		var array = config.GetChildren().ToArray();
		var length = source.Length;
		var elementType = source.GetType().GetElementType();
		var instance = Array.CreateInstance(elementType!, length + array.Length);
		if (length > 0)
			Array.Copy(source, instance, length);
		for (var index = 0; index < array.Length; ++index)
		{
			try
			{
				var obj = BindInstance(elementType!, null, array[index]);
				if (obj != null)
					instance.SetValue(obj, length + index);
			}
			catch
			{
			}
		}
		return instance;
	}

	private static bool TryConvertValue(Type type, string value, out object? result, out Exception? error)
	{
		error = null;
		result = null;

		if (type == typeof(object))
		{
			result = value;
			return true;
		}
		if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			return string.IsNullOrEmpty(value) || TryConvertValue(Nullable.GetUnderlyingType(type)!, value, out result, out error);
		}
		var converter = TypeDescriptor.GetConverter(type);
		if (!converter.CanConvertFrom(typeof(string)))
			return false;
		try
		{
			result = converter.ConvertFromInvariantString(value);
		}
		catch (Exception ex)
		{
			error = new InvalidOperationException($"Failed binding: {type.FullName}", ex);
		}
		return true;
	}

	private static Type? FindOpenGenericInterface(Type expected, Type actual)
	{
		var typeInfo = actual.GetTypeInfo();
		if (typeInfo.IsGenericType && actual.GetGenericTypeDefinition() == expected)
			return actual;
		foreach (var implementedInterface in typeInfo.ImplementedInterfaces)
		{
			if (implementedInterface.GetTypeInfo().IsGenericType && implementedInterface.GetGenericTypeDefinition() == expected)
				return implementedInterface;
		}
		return null;
	}

	private static IEnumerable<PropertyInfo> GetAllProperties(TypeInfo type)
	{
		var propertyInfoList = new List<PropertyInfo>();
		do
		{
			propertyInfoList.AddRange(type.DeclaredProperties);
			type = type.BaseType!.GetTypeInfo();
		}
		while (type != typeof(object).GetTypeInfo());
		return propertyInfoList;
	}
}
