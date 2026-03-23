using System.Collections;
using System.Collections.Concurrent;

namespace Odin.Core;

public static class ReflectionExtensions
{
	///// <summary>
	///// Invokes method async and return result.
	///// </summary>
	///// <param name="methodInfo"></param>
	///// <param name="obj">Object on which to invoke method.</param>
	///// <param name="parameters">Parameters to pass to the method.</param>
	///// <returns></returns>
	//public static async Task<object> InvokeAsync(this MethodInfo methodInfo, object obj, params object[] parameters)
	//{
	//	dynamic awaitable = methodInfo.Invoke(obj, parameters);
	//	await awaitable;
	//	return awaitable.GetAwaiter().GetResult();
	//}

	private static readonly ConcurrentDictionary<Type, string> DemystifiedTypeNameCache = new();

	/// <summary>
	/// Gets the type name as a more clarified name when having generics e.g.
	/// returns "Odin.MultiGen`2[Odin.Hero,System.Collections.Generic.List`1[Odin.Heroic.Role]]" into "MultiGen&lt;Hero, List&lt;Role&gt;&gt;".
	/// </summary>
	/// <param name="type">Type to get demystified name for.</param>
	/// <returns></returns>
	public static string GetDemystifiedName(this Type type)
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

	public static IList CreateGenericList(this Type targetType, IEnumerable<object> items)
	{
		var genericListType = typeof(List<>).MakeGenericType(targetType);
		var genericList = Activator.CreateInstance(genericListType);
		var addMethod = genericListType.GetMethod("Add");

		if (addMethod is null)
			return default;

		foreach (var item in items)
			addMethod.Invoke(genericList, [item]);

		return (IList)genericList;
	}
}
