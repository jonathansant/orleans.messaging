using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers an existing registration of <typeparamref name="TImplementation"/> as a provider of service type <typeparamref name="TService"/>.
	/// </summary>
	/// <typeparam name="TService">The service type being provided.</typeparam>
	/// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
	/// <param name="services">The service collection.</param>
	public static IServiceCollection AddFromExisting<TService, TImplementation>(this IServiceCollection services) where TImplementation : TService
		=> services.AddFromExisting(typeof(TService), typeof(TImplementation));

	// Copied from: https://github.com/dotnet/orleans/blob/master/src/Orleans.Core/Configuration/ServiceCollectionExtensions.cs
	/// <summary>
	/// Registers an existing registration of <paramref name="implementation"/> as a provider of service type <paramref name="service"/>.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="service">The service type being provided.</param>
	/// <param name="implementation">The implementation of <paramref name="service"/>.</param>
	public static IServiceCollection AddFromExisting(this IServiceCollection services, Type service, Type implementation)
	{
		var registration = services.FirstOrDefault(s => s.ServiceType == implementation);

		if (registration == null) return services;

		var newRegistration = new ServiceDescriptor(
			service,
			sp => sp.GetRequiredService(implementation),
			registration.Lifetime);
		services.Add(newRegistration);
		return services;
	}

	/// <summary>
	/// Registers an existing registration of <typeparamref name="TImplementation"/> as a provider of <typeparamref name="TService"/> if there are no existing <typeparamref name="TService"/> implementations.
	/// </summary>
	/// <typeparam name="TService">The service type being provided.</typeparam>
	/// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
	/// <param name="services">The service collection.</param>
	public static IServiceCollection TryAddFromExisting<TService, TImplementation>(this IServiceCollection services) where TImplementation : TService
	{
		var providedService = services.FirstOrDefault(service => service.ServiceType == typeof(TService));
		if (providedService == null)
			services.AddFromExisting<TService, TImplementation>();
		return services;
	}

	public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<TOptions, IServiceProvider> configure)
		where TOptions : class
		=> services.Configure(Options.Options.DefaultName, configure);

	public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Action<TOptions, IServiceProvider> configure)
		where TOptions : class
		=> services.AddSingleton<IConfigureOptions<TOptions>>(sp
			=> new ConfigureNamedOptions<TOptions>(name, x => configure(x, sp))
		);
}
