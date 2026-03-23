using Microsoft.Extensions.Configuration;
using Odin.Core;
using Odin.Core.App;
using Odin.Core.Realtime;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OdinMessagingCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddOdinMessaging(IConfiguration config, IAppInfo appInfo)
		{
			var messageBrokerConfig = config.GetSection(OdinMessageBrokerNames.Platform).Get<MessageBrokerConfig>()
			                          ?? new() { Type = MessageBrokerType.Memory };

			services
				.AddExternalMessaging(config, OdinMessageBrokerNames.Platform, messageBrokerConfig)
				.AddExternalMessaging(config, OdinMessageBrokerNames.LastCommittedExternal)
				.AddExternalMessaging(config, OdinMessageBrokerNames.JobScheduler)
				.AddInternalBifrostMessaging(config, appInfo)
				;

			return services;
		}

		public IServiceCollection AddExternalMessaging(
			IConfiguration config,
			string odinMessageBrokerConfigKey,
			MessageBrokerConfig? messageBrokerConfig = null
		)
		{
			messageBrokerConfig ??= config.GetSection(odinMessageBrokerConfigKey).Get<MessageBrokerConfig>();

			if (messageBrokerConfig != null)
				services.AddQueueConfigService(odinMessageBrokerConfigKey, messageBrokerConfig);

			return services;
		}

		private IServiceCollection AddInternalBifrostMessaging(IConfiguration config, IAppInfo appInfo)
			=> services.AddInternalMessaging(config, appInfo, OdinMessageBrokerNames.Bifrost);

		public IServiceCollection AddInternalMessaging(
			IConfiguration config,
			IAppInfo appInfo,
			string brokerName
		)
		{
			var messageBrokerConfig = config.GetSection(brokerName).Get<MessageBrokerConfig>();
			services.AddSingleton<IInternalQueueNameBuilder>(
				new InternalQueueNameBuilder(
					appInfo,
					new()
					{
						Template = messageBrokerConfig?.Queues.Template ?? "{env}_{serviceName}_{name}_{version}"
					}
				)
			);

			if (messageBrokerConfig != null)
				services.AddQueueConfigService(brokerName, messageBrokerConfig);

			return services;
		}

		private IServiceCollection AddMessagingProvider(
			string providerConfigSection,
			MessageBrokerConfig? messageBrokerConfig
		) => messageBrokerConfig == null ? services : services.AddQueueConfigService(providerConfigSection, messageBrokerConfig);

		private IServiceCollection AddQueueConfigService(
			string key,
			MessageBrokerConfig messageBrokerConfig
		) => services.AddKeyedSingleton<IQueueConfigService, QueueConfigService>(
			key,
			(provider, _) => ActivatorUtilities.CreateInstance<QueueConfigService>(
				provider,
				messageBrokerConfig,
				provider.GetRequiredService<IAppInfo>(),
				key
			)
		);
	}
}