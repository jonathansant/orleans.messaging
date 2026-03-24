using System.Collections.Concurrent;
using System.Text;

namespace Odin.Messaging.Utils;

public static class TypeExtensions
{
	private static readonly ConcurrentDictionary<Type, string> DemystifiedTypeNameCache = new();

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

	public static bool HasCustomAttribute<T>(this MemberInfo memberInfo, Func<T, bool>? predicate = null)
		where T : Attribute
	{
		var attr = memberInfo.GetCustomAttribute<T>();
		if (predicate == null)
			return attr is not null;
		return attr is not null && predicate(attr);
	}

	public static bool IsNullableEnum(this Type type)
		=> Nullable.GetUnderlyingType(type)?.IsEnum == true;

	public static bool IsNumericType(this Type type)
		=> NumericTypes.Contains(type) || NumericTypes.Contains(Nullable.GetUnderlyingType(type));

	public static Type GetCardinalType(this Type type)
		=> type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(type)! : type;

	extension(Type type)
	{
		public MethodInfo? GetInterfaceMethod(string methodName)
		{
			if (!type.IsInterface)
				throw new ArgumentException("Type must be an interface", nameof(type));

			var method = type.GetMethod(methodName);
			return method
					?? type.GetInterfaces()
						.Select(interfaceType => interfaceType.GetInterfaceMethod(methodName))
						.FirstOrDefault(subMethod => subMethod != null);
		}

		public IEnumerable<MethodInfo>? GetInterfaceMethods()
		{
			if (!type.IsInterface)
				throw new ArgumentException("Type must be an interface", nameof(type));

			var methods = type.GetMethods();
			return methods is not null and not []
					? type.GetInterfaces()
						.Select(GetInterfaceMethods)
						.SelectMany(x => x)
					: methods
				;
		}

		public string GetDemystifiedName()
			=> DemystifiedTypeNameCache.GetOrAdd(type, arg =>
			{
				if (type.GenericTypeArguments.Length == 0)
					return type.Name;

				var genericArgBuilder = new StringBuilder($"{type.Name.Remove(type.Name.Length - 2)}<");
				for (var index = 0; index < type.GenericTypeArguments.Length; index++)
				{
					var genericArg = type.GenericTypeArguments[index];
					var demystifiedGenericArgName = GetDemystifiedName(genericArg);
					genericArgBuilder.Append(demystifiedGenericArgName);
					if (type.GenericTypeArguments.Length - index > 1)
						genericArgBuilder.Append(", ");
				}

				genericArgBuilder.Append(">");
				return genericArgBuilder.ToString();
			});
	}
}
