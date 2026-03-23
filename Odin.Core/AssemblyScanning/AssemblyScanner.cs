using Microsoft.Extensions.Primitives;
using System.Reflection;

namespace Odin.Core.AssemblyScanning;

public static class AssemblyScanner
{
	public static IEnumerable<Type> Scan(this Assembly assembly, Action<AssemblyScannerFilterOptions>? configure = null)
		=> assembly.GetTypes().Scan(configure);

	public static IEnumerable<Type> Scan(this IEnumerable<Assembly> assemblies, Action<AssemblyScannerFilterOptions>? configure = null)
		=> assemblies.Distinct().SelectMany(x => x.GetTypes()).Scan(configure);

	public static IEnumerable<Type> Scan(this IEnumerable<Type> types, Action<AssemblyScannerFilterOptions>? configure = null)
	{
		var opts = new AssemblyScannerFilterOptions();
		configure?.Invoke(opts);

		return types.Where(x => opts.FilterPredicate?.Invoke(x) != false);
	}
}

public class AssemblyScannerFilterOptions
{
	/// <summary>
	/// Gets the compiled filter predicate.
	/// </summary>
	internal Func<Type, bool>? FilterPredicate { get; set; }

	/// <summary>
	/// Add a custom filter.
	/// </summary>
	/// <param name="filterFunc">Filter predicate.</param>
	/// <returns></returns>
	public AssemblyScannerFilterOptions WithFilter(Func<Type, bool> filterFunc)
	{
		var predicate = FilterPredicate ?? (x => true);
		FilterPredicate = x => predicate(x) && filterFunc(x);
		return this;
	}

	/// <summary>
	/// Add filter to exclude types which has the <see cref="DiscoveryIgnoreAttribute"/>.
	/// </summary>
	public AssemblyScannerFilterOptions WithIgnore()
		=> WithFilter(x => !x.HasAttribute<DiscoveryIgnoreAttribute>());

	/// <summary>
	/// Add filtering to include only types which matches the <see cref="DiscoveryTagAttribute"/> with the specified tag.
	/// </summary>
	/// <param name="includeSet">Tags to include.</param>
	/// <param name="excludeSet">Tags to exclude.</param>
	/// <returns></returns>
	public AssemblyScannerFilterOptions WithTags(StringValues? includeSet = null, StringValues? excludeSet = null)
	{
		var include = includeSet?.ToHashSet();
		var exclude = excludeSet?.ToHashSet();
		return WithFilter(x =>
			{
				var tags = x.GetCustomAttribute<DiscoveryTagAttribute>()?.Tags;
				if (!include.IsNullOrEmpty() && !tags.HasValue)
					return false;

				return tags?.IsEligibleAny(include, exclude) is not false;
			}
		);
	}
}
