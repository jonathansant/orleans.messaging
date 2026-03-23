using Odin.Core.Timing;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Odin.Core;

public static class TypeExtensions
{
	private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, bool>> HasPropertyCache = new();
	private static readonly ConcurrentDictionary<PropertyInfo, bool> IsNullablePropertyCache = new();

	private static readonly HashSet<Type> NumericTypes =
	[
		typeof(byte),
		typeof(sbyte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(decimal),
	];

	public static T? GetAttribute<T>(this Type type, bool inherit = true)
		where T : Attribute
		=> type.GetTypeInfo().GetCustomAttribute<T>(inherit);

	public static bool HasAttribute<T>(this Type type, bool inherit = true, Func<T, bool>? predicate = null)
		where T : Attribute
	{
		var attribute = type.GetAttribute<T>(inherit);

		if (predicate == null)
			return attribute is not null;

		return attribute is not null && predicate(attribute);
	}

	public static T GetAttribute<T>(this MethodInfo method, bool inherit = true)
		where T : Attribute
		=> method.GetCustomAttribute<T>(inherit);

	public static T GetAttribute<T>(this PropertyInfo propInfo, bool inherit = true)
		where T : Attribute
		=> propInfo.GetCustomAttribute<T>(inherit);

	public static bool HasCustomAttribute<T>(this MemberInfo propInfo, Func<T, bool>? predicate = null)
		where T : Attribute
	{
		var attr = propInfo.GetCustomAttribute<T>();

		if (predicate == null)
			return attr is not null;

		return attr is not null && predicate(attr);
	}

	public static List<PropertyInfo> GetProperties<TFilterAttribute>(this Type type)
		=> type.GetProperties()
			.Where(x => x.GetCustomAttributes(typeof(TFilterAttribute), true).Length == 1)
			.ToList();

	public static List<PropertyInfo> GetProperties<TFilterAttribute, TIgnoreAttribute>(this Type type)
		=> type.GetProperties()
			.Where(x => x.GetCustomAttributes(typeof(TFilterAttribute), true).Length == 1
						&& x.GetCustomAttributes(typeof(TIgnoreAttribute), true).Length == 0
			)
			.ToList();

	public static bool IsNullableEnum(this Type type)
		=> Nullable.GetUnderlyingType(type)?.IsEnum == true;

	public static bool IsNullable(this Type type)
		=> Nullable.GetUnderlyingType(type) != null;

	private static readonly ThreadLocal<NullabilityInfoContext> NullabilityInfoContext = new(() => new());

	/// <summary>
	/// Determines whether the property is nullable or not (or explicitly nullable e.g. for `string?`, `MyClass?` etc...)
	/// </summary>
	public static bool IsNullable(this PropertyInfo property)
	{
		if (IsNullablePropertyCache.TryGetValue(property, out var cachedResult))
			return cachedResult;

		var info = NullabilityInfoContext.Value.Create(property);
		var result = info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable;

		IsNullablePropertyCache.TryAdd(property, result);
		return result;
	}

	/// <summary>
	/// Determines whether the class is a subclass of <paramref name="targetType"/> including open generic.
	/// </summary>
	/// <param name="type">Type to check.</param>
	/// <param name="targetType">Type to compare with, which can be open generic type.</param>
	/// <returns>Returns true if it's a subclass of.</returns>
	public static bool IsSubclassOfOpenGeneric(this Type type, Type targetType)
	{
		while (type != null && type != typeof(object))
		{
			var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
			if (targetType == cur)
				return true;
			type = type.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Determines whether the type implements/inherits as a subclass of <paramref name="targetType"/> including open generic.
	/// </summary>
	/// <param name="type">Type to check.</param>
	/// <param name="targetType">Type to compare with, which can be open generic type.</param>
	/// <returns>Returns true if it's a subclass of.</returns>
	public static bool InheritsFrom(this Type? type, Type? targetType)
	{
		if (type == null || targetType == null)
			return false;

		if (type.BaseType is { IsGenericType: true } && type.BaseType.GetGenericTypeDefinition() == targetType)
			return true;

		if (type.BaseType.InheritsFrom(targetType))
			return true;

		return
			(targetType.IsAssignableFrom(type) && type != targetType)
			|| type.GetInterfaces()
				.Any(x =>
					x.IsGenericType && x.GetGenericTypeDefinition() == targetType
				);
	}

	/// <summary>
	/// Ensures that the type inherits from the specified target type.
	/// </summary>
	/// <typeparam name="T">Type to ensure <see cref="Type"/> inherits from.</typeparam>
	/// <param name="type">Type to check.</param>
	/// <exception cref="ArgumentException">Thrown when type does not inherit from target type.</exception>
	public static Type EnsureInheritsFrom<T>(this Type type)
		=> type.EnsureInheritsFrom(typeof(T));

	/// <summary>
	/// Ensures that the type inherits from the specified target type.
	/// </summary>
	/// <param name="type">Type to check.</param>
	/// <param name="targetType">Type to ensure <see cref="Type"/> inherits from.</param>
	/// <exception cref="ArgumentException">Thrown when type does not inherit from target type.</exception>
	public static Type EnsureInheritsFrom(this Type type, Type targetType)
	{
		if (!type.InheritsFrom(targetType))
			throw new ArgumentException(
				$"Invalid type specified for '{type.GetDemystifiedName()}', does not implement {targetType.GetDemystifiedName()}.",
				nameof(type)
			);
		return type;
	}

	/// <summary>
	/// Determine whether the type is a generic type of the specified type.
	/// </summary>
	/// <param name="type">Type to check whether it matches the <paramref name="genericTypeCheck"/>.</param>
	/// <param name="genericTypeCheck">Open generic type to check.</param>
	public static bool IsGenericTypeOf(this Type type, Type genericTypeCheck)
		=> type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeCheck;

	/// <summary>
	/// Determine whether any generic type arg inherits from the specified type check.
	/// </summary>
	/// <param name="type">Generic type being checked.</param>
	/// <param name="genericArgTypeCheck">Type to compare against.</param>
	public static bool AnyGenericTypeArgsInheritsFrom(this Type type, Type genericArgTypeCheck)
		=> type.IsGenericType && type.GenericTypeArguments.Any(x => x.InheritsFrom(genericArgTypeCheck));

	/// <summary>
	/// Ensures that the type implements (inherits/subclass) of the specified target type.
	/// </summary>
	/// <param name="type">Type to check.</param>
	/// <param name="targetType">Type to ensure <see cref="Type"/> inherits from.</param>
	/// <exception cref="ArgumentException">Thrown when type does not inherit from target type.</exception>
	public static Type EnsureImplements(this Type type, Type targetType)
	{
		if (!type.IsSubclassOfOpenGeneric(targetType) && !type.InheritsFrom(targetType))
			throw new ArgumentException(
				$"Invalid type specified for '{type.GetDemystifiedName()}', does not implement {targetType.GetDemystifiedName()}.",
				nameof(type)
			);
		return type;
	}

	/// <summary>
	/// Checks whether property is numeric (according to NumericTypes)
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsNumericType(this Type type)
		=> NumericTypes.Contains(type) || NumericTypes.Contains(Nullable.GetUnderlyingType(type));

	/// <summary>
	/// Checks whether property is string.
	/// </summary>
	public static bool IsString(this Type type)
		=> type == typeof(string);

	/// <summary>
	/// Gets actual type of property. If property is of type null, the underlying type is returned instead.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static Type GetCardinalType(this Type type)
		=> type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(type) : type;

	/// <summary>
	/// Checks whether the property is a primitive type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsPrimitive(this Type type)
		=> type.IsPrimitive || type.IsValueType || (type == typeof(string));

	/// <summary>
	/// Checks whether the property is an anonymous type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsAnonymous(this Type type)
	{
		var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
		var nameContainsAnonymousType = type.FullName != null && type.FullName.Contains("AnonymousType");
		var isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

		return isAnonymousType;
	}

	/// <summary>
	/// Checks whether a type contains a property of another type.
	/// </summary>
	/// <typeparam name="T">Typeof to check if property exists.</typeparam>
	/// <param name="type">Type to check property on.</param>
	/// <returns></returns>
	public static bool HasAnyPropertyOfType<T>(this Type type)
	{
		var propTypes = type
			.GetProperties()
			.Select(x => x.PropertyType);

		return propTypes.Any(t => t == typeof(T));
	}

	public static bool HasAnyPropertyOfEnumerableOfType<T>(this Type type)
	{
		var propTypes = type
			.GetProperties()
			.Select(x => x.PropertyType);

		return propTypes.Any(t => typeof(IEnumerable<T>).IsAssignableFrom(t));
	}

	/// <summary>
	/// Checks whether a property exists on a type.
	/// </summary>
	/// <param name="type">Type on which to check property.</param>
	/// <param name="propertyName">Name of property to check.</param>
	public static bool HasProperty(this Type type, string propertyName)
	{
		if (HasPropertyCache.TryGetValue(type, out var typeCache)
			&& typeCache.TryGetValue(propertyName, out var result))
			return result;

		var hasProperty = type.GetProperty(propertyName) != null;

		typeCache ??= HasPropertyCache.GetOrAdd(type, _ => new());
		typeCache.TryAdd(propertyName, hasProperty);

		return hasProperty;
	}

	public static bool ShouldConvertToTimeSpan(this PropertyInfo propInfo)
		=> propInfo.HasCustomAttribute<JsonConverterAttribute>(attr => attr.ConverterType == typeof(DurationJsonConverter));

	public static MethodInfo? GetInterfaceMethod(this Type type, string methodName)
	{
		if (!type.IsInterface)
			throw new ArgumentException("Type must be an interface", nameof(type));

		var method = type.GetMethod(methodName);
		return method
			   ?? type.GetInterfaces()
				   .Select(interfaceType => interfaceType.GetInterfaceMethod(methodName))
				   .FirstOrDefault(subMethod => subMethod != null);
	}

	public static IEnumerable<MethodInfo>? GetInterfaceMethods(this Type type)
	{
		if (!type.IsInterface)
			throw new ArgumentException("Type must be an interface", nameof(type));

		var methods = type.GetMethods();
		return methods.IsNullOrEmpty()
				? type.GetInterfaces()
					.Select(GetInterfaceMethods)
					.SelectMany(x => x)
				: methods
			;
	}

	private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

	public static MethodInfo GetCachedMethod(this Type type, string methodName, Type[]? types = null)
	{
		ArgumentNullException.ThrowIfNull(methodName);

		var typesNames = types.IsNullOrEmpty() ? string.Empty : string.Join(", ", types.Select(x => x.FullName));
		var key = $"{type.FullName}.{methodName}+{typesNames}";

		return MethodCache.GetOrAdd(
			key,
			_ => types.IsNullOrEmpty()
				? type.GetMethod(methodName)
				: type.GetMethod(methodName, types)
		);
	}

	private static readonly ConcurrentDictionary<Type, Dictionary<object, Type>> DerivedTypeCache = new();

	public static bool IsJsonDerivedType(this Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		var result = DerivedTypeCache.GetOrAdd(
			type,
			_ => type.Assembly.GetTypes()
				.Where(t => t.IsSubclassOf(type) || t == type)
				.SelectMany(t => t.GetCustomAttributes<JsonDerivedTypeAttribute>())
				.ToDictionary(attr => attr.TypeDiscriminator, attr => attr.DerivedType)
		);

		return result.Count > 0;
	}

	public static Type? GetTypeByDiscriminator(this Type type, string discriminator)
	{
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(discriminator);

		var isDerivedType = type.IsJsonDerivedType();
		if (!isDerivedType)
			return null;

		var derivedTypes = DerivedTypeCache[type];
		return derivedTypes.TryGetValue(discriminator, out var derivedType) ? derivedType : null;
	}
}
