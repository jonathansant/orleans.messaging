using Odin.Core.FlowControl;
using Orleans.Runtime;
using Orleans.Runtime.Services;
using Orleans.Services;
using System.Collections.Concurrent;
using System.Reflection;

namespace Odin.Orleans.Core;

public interface IOdinSchedulerGrainService : IGrainService
{
	ValueTask SetReminder<T>(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay = null,
		int? runAfterHourUtc = null
	);

	ValueTask SetReminderAsSingleShot<T>(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay = null,
		int? runAfterHourUtc = null
	);
}

public abstract class OdinSchedulerGrainService : GrainService, IOdinSchedulerGrainService
{
	private readonly IGrainFactory _grainFactory;

	protected OdinSchedulerGrainService(
		GrainId id,
		Silo silo,
		ILoggerFactory loggerFactory,
		IGrainFactory grainFactory
	) : base(id, silo, loggerFactory)
	{
		_grainFactory = grainFactory;
	}

	public ValueTask SetReminder<T>(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay = null,
		int? runAfterHourUtc = null
	)
		=> SetReminder(grainKey, reminderName, timeSpan, initialDelay, runAfterHourUtc, false, typeof(T).AssemblyQualifiedName);

	public ValueTask SetReminderAsSingleShot<T>(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay = null,
		int? runAfterHourUtc = null
	)
		=> SetReminder(grainKey, reminderName, timeSpan, initialDelay, runAfterHourUtc, true, typeof(T).AssemblyQualifiedName);

	protected async ValueTask SetReminder(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay,
		int? runAfterHourUtc,
		bool singleShot,
		string grainType,
		bool isLoading = false
	)
	{
		var reminderRegistration = _grainFactory.GetOdinReminderRegistrationGrain();
		if (!await reminderRegistration.TryRegister(grainKey))
			return;

		var odinTimer = new OdinTimer(
			async ctrl =>
			{
				if (!runAfterHourUtc.HasValue || DateTime.UtcNow.TimeOfDay.Hours > runAfterHourUtc.Value)
				{
					var grain = _grainFactory.GetGrainInstanceByStringId<ISchedulable>(grainKey, grainType);
					await grain.Execute(reminderName);
					if (singleShot)
					{
						await reminderRegistration.Remove(grainKey);
						await DeleteReminder(grainKey);
						await ctrl.StopTimer();
					}
				}
			},
			new(timeSpan, initialDelay)
		);

		if (!isLoading)
			await AddReminder(grainKey, reminderName, timeSpan, initialDelay, runAfterHourUtc, singleShot, grainType);

		odinTimer.StartTimer();
	}

	protected virtual Task AddReminder(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay,
		int? runAfterHourUtc,
		bool singleShot,
		string grainType
	) => Task.CompletedTask;

	protected virtual Task DeleteReminder(string grainKey)
		=> Task.CompletedTask;
}

public interface IOdinSchedulerServiceClient : IGrainServiceClient<IOdinSchedulerGrainService>, IOdinSchedulerGrainService
{
}

public class OdinSchedulerServiceClient : GrainServiceClient<IOdinSchedulerGrainService>, IOdinSchedulerServiceClient
{
	public OdinSchedulerServiceClient(IServiceProvider serviceProvider)
		: base(serviceProvider)
	{
	}

	public ValueTask SetReminder<T>(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay,
		int? runAfterHourUtc = null
	)
		=> GetGrainService(0).SetReminder<T>(grainKey, reminderName, timeSpan, initialDelay, runAfterHourUtc);

	public ValueTask SetReminderAsSingleShot<T>(
		string grainKey,
		string reminderName,
		TimeSpan timeSpan,
		TimeSpan? initialDelay,
		int? runAfterHourUtc = null
	)
		=> GetGrainService(0).SetReminderAsSingleShot<T>(grainKey, reminderName, timeSpan, initialDelay, runAfterHourUtc);
}

public interface ISchedulable : IGrainWithStringKey
{
	ValueTask Execute(string reminderName);
}

internal static class Extensions
{
	private static readonly ConcurrentDictionary<string, MethodInfo> MethodInfoByGrainKey = new();

	private static readonly Lazy<MethodInfo> GetGrainMethod = new(() => typeof(IGrainFactory).GetCachedMethod(nameof(IGrainFactory.GetGrain), [typeof(string), typeof(string)]));
	public static T GetGrainInstanceByStringId<T>(this IGrainFactory grainFactory, string grainKey, string typeString)
	{
		var genericMethodInfo = MethodInfoByGrainKey.GetOrAdd(
			grainKey,
			_ =>
			{
				var type = Type.GetType(typeString)!;
				return GetGrainMethod.Value.MakeGenericMethod(type);
			}
		);

		return (T)(genericMethodInfo.Invoke(grainFactory, [grainKey, null]));
	}
}
