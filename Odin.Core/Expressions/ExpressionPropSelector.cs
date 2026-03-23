using Humanizer;
using System.Linq.Expressions;

namespace Odin.Core.Expressions;

/// <summary>
/// Extract an expression property selector to be cached and extract Name, Expression and Compiled function.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TKey"></typeparam>
public readonly struct ExpressionPropSelector<T, TKey>
{
	public string Name { get; }
	public Func<T, TKey> Func { get; }
	public Expression<Func<T, TKey>> Expression { get; }

	public ExpressionPropSelector(Expression<Func<T, TKey>> expr)
	{
		Expression = expr;
		Name = typeof(TKey) == typeof(object) ? expr.NamesOf().OrderBy(x => x).JoinTokens() : expr.NameOf();
		Func = expr.Compile();
	}
}

/// <summary>
/// Generic property accessor cache for efficient reflection-based property access
/// </summary>
/// <typeparam name="T">The type to cache property accessors for</typeparam>
public sealed class PropertyAccessorCache<T> where T : class
{
	private readonly Dictionary<string, Func<T, object>> _propertyAccessorCache = new();

	public PropertyAccessorCache()
	{
		CachePropertyAccessors();
	}

	private void CachePropertyAccessors()
	{
		var properties = typeof(T).GetProperties();

		foreach (var property in properties)
		{
			var parameter = Expression.Parameter(typeof(T), "x");
			var propertyAccess = Expression.Property(parameter, property);
			var lambda = Expression.Lambda<Func<T, object>>(
				Expression.Convert(propertyAccess, typeof(object)),
				parameter
			);

			_propertyAccessorCache[property.Name.Pascalize()] = lambda.Compile();
		}
	}

	/// <summary>
	/// Gets the property accessor function for the specified property name
	/// </summary>
	/// <param name="propertyName">The property name (will be pascalized)</param>
	/// <param name="accessor">The compiled property accessor function if found</param>
	/// <returns>True if the accessor was found, false otherwise</returns>
	public bool TryGet(string propertyName, out Func<T, object> accessor)
		=> _propertyAccessorCache.TryGetValue(propertyName.Pascalize(), out accessor);
}
