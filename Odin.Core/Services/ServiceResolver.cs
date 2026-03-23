using System.Collections.ObjectModel;

namespace Odin.Core.Services;

/// <summary>
/// Abstraction for resolving services by type.
/// </summary>
/// <typeparam name="TService">Service to resolve by type.</typeparam>
public interface IServiceResolver<TService>
{
	/// <summary>
	/// Get all <typeparamref name="TService"/> instances.
	/// </summary>
	IEnumerable<TService> ResolveAll();

	/// <summary>
	/// Get all <typeparamref name="TService"/> instances as a map (lookupType -> instance).
	/// </summary>
	ReadOnlyDictionary<Type, TService> ResolveAllMap();

	/// <summary>
	/// Get the <typeparamref name="TService"/> instance for the <paramref name="lookupType"/> or null.
	/// </summary>
	/// <param name="lookupType">Type to get instance for.</param>
	TService? ResolveOrDefault(Type lookupType);

	/// <summary>
	/// Get the <typeparamref name="TService"/> instance for the <paramref name="lookupType"/> or throw.
	/// </summary>
	/// <param name="lookupType">Type to get instance for.</param>
	TService Resolve(Type lookupType);

	/// <inheritdoc cref="IServiceResolver{TService}.ResolveOrDefault" />
	/// <typeparam name="T">CrudModel type to get instance for.</typeparam>
	TService? ResolveOrDefault<T>();

	/// <inheritdoc cref="IServiceResolver{TService}.Resolve" />
	/// <typeparam name="T">CrudModel type to get instance for.</typeparam>
	TService Resolve<T>();
}

/// <summary>
/// Abstraction for resolving services by type.
/// </summary>
/// <typeparam name="TService">Service to resolve by type.</typeparam>
public interface ILazyServiceResolver<TService> : IServiceResolver<TService>
{
	/// <summary>
	/// Add a <paramref name="serviceType"/> for the <paramref name="lookupType"/>.
	/// </summary>
	/// <param name="lookupType">Lookup type to register.</param>
	/// <param name="serviceType">Service instance type to use.</param>
	void Add(Type lookupType, Type serviceType);

	/// <summary>
	/// Get the <typeparamref name="TService"/> type of the registered <paramref name="lookupType"/> or null.
	/// </summary>
	/// <param name="lookupType">Type to get service type for.</param>
	Type? GetServiceTypeOrDefault(Type lookupType);

	/// <summary>
	/// Gets all registered resolvers (lookupType -> <typeparamref name="TService"/> type).
	/// </summary>
	ReadOnlyDictionary<Type, Type> GetAllTypeMap();
}

/// <summary>
/// Base implementation for resolving services by type using <see cref="IServiceProvider"/> to resolve instances lazily.
/// </summary>
/// <typeparam name="TService">Service type to use.</typeparam>
public abstract class ServiceProviderServiceResolver<TService> : ILazyServiceResolver<TService>
	where TService : class
{
	private readonly IServiceProvider _serviceProvider;
	private readonly Dictionary<Type, Type> _types = [];
	private readonly ReadOnlyDictionary<Type, Type> _typesReadOnly;

	protected ServiceProviderServiceResolver(
		IServiceProvider serviceProvider
	)
	{
		_serviceProvider = serviceProvider;
		_typesReadOnly = new(_types);
	}

	public void Add(Type lookupType, Type serviceType)
		=> _types.TryAdd(lookupType, serviceType);

	public ReadOnlyDictionary<Type, Type> GetAllTypeMap()
		=> _typesReadOnly;

	public Type? GetServiceTypeOrDefault(Type lookupType)
		=> !_types.TryGetValue(lookupType, out var serviceType)
			? null
			: serviceType;

	public IEnumerable<TService> ResolveAll()
		=> _typesReadOnly.Select(x => Resolve(x.Key));

	public ReadOnlyDictionary<Type, TService> ResolveAllMap()
		=> _typesReadOnly.ToDictionary(x => x.Key, x => Resolve(x.Key)).AsReadOnly();

	public TService? ResolveOrDefault(Type lookupType)
		=> !_types.TryGetValue(lookupType, out var serviceType)
			? null
			: (TService?)_serviceProvider.GetService(serviceType);

	public TService Resolve(Type lookupType)
		=> ResolveOrDefault(lookupType)
		   ?? throw new InvalidOperationException($"{typeof(TService).GetDemystifiedName()} not resolved for type '{lookupType}'.");

	public TService? ResolveOrDefault<T>()
		=> ResolveOrDefault(typeof(T));

	public TService Resolve<T>()
		=> Resolve(typeof(T));
}
