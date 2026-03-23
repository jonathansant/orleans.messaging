using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Odin.Core.Services;

public interface IKeyServiceFactory<TService>
	where TService : class
{
	TService Get(string key);
	TService GetOrCreate(string key, params object[] parameters);
	TService GetOrCreate(string key, Func<object[]> onCreate, Action<TService>? onCreated = null);
	TService GetOrCreate(string key, Func<string, object[]> onCreate, Action<TService>? onCreated = null);
	IEnumerable<TService> GetAll();
	TService Create(params object[] parameters);
	void Add(string key, TService service);
}

/// <summary>
/// Factory class to use as base in order to build Keyed services.
/// </summary>
/// <typeparam name="TService"></typeparam>
/// <typeparam name="TServiceImplementation"></typeparam>
public class KeyServiceFactory<TService, TServiceImplementation> : IKeyServiceFactory<TService>
	where TService : class
	where TServiceImplementation : TService
{
	private readonly IServiceProvider _serviceProvider;
	// https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/#ensuring-the-delegate-only-runs-once-with-lazy
	protected readonly ConcurrentDictionary<string, Lazy<TService>> Services = new();

	public KeyServiceFactory(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public TService Get(string key)
	{
		if (!Services.TryGetValue(key, out var service))
			throw new OdinKeyNotFoundException(key);
		return service.Value;
	}

	public TService GetOrCreate(string key, params object[] parameters)
		=> GetOrCreate(key, () => parameters);

	public TService GetOrCreate(string key, Func<object[]> onCreate, Action<TService>? onCreated = null)
		=> GetOrCreate(key, _ => onCreate(), onCreated);

	public TService GetOrCreate(string key, Func<string, object[]> onCreate, Action<TService>? onCreated = null)
		=> Services.GetOrAdd(key, k =>
			new(() =>
				{
					var parameters = onCreate(k);
					var service = Create(parameters);
					onCreated?.Invoke(service);
					return service;
				}
			)
		).Value;

	public IEnumerable<TService> GetAll()
		=> Services.Values.Select(x => x.Value);

	public TService Create(params object[] parameters)
		=> ActivatorUtilities.CreateInstance<TServiceImplementation>(_serviceProvider, parameters);

	public void Add(string key, TService service) => Services.TryAdd(key, new(() => service));
}
