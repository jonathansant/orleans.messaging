namespace Odin.Orleans.Core;

public interface IOdinReminderRegistrationGrain : IGrainWithStringKey
{
	ValueTask<bool> TryRegister(string grainKey);
	ValueTask Remove(string grainKey);
}

public class OdinReminderRegistrationGrain : OdinGrain, IOdinReminderRegistrationGrain
{
	private readonly HashSet<string> _grainKeys = new();

	public OdinReminderRegistrationGrain(
		ILogger<OdinReminderRegistrationGrain> logger,
		ILoggingContext loggingContext
	) : base(logger, loggingContext)
	{
	}

	/// <summary>
	/// returns false if already registered
	/// </summary>
	/// <param name="grainKey">grainKey</param>
	/// <returns></returns>
	public ValueTask<bool> TryRegister(string grainKey)
		=> ValueTask.FromResult(_grainKeys.Add(grainKey));

	public ValueTask Remove(string grainKey)
	{
		_grainKeys.Remove(grainKey);
		return ValueTask.CompletedTask;
	}
}

internal static class OdinReminderRegistrationGrainExtensions
{
	private const string GrainKey = "reminderRegistration";

	public static IOdinReminderRegistrationGrain GetOdinReminderRegistrationGrain(this IGrainFactory grainFactory)
		=> grainFactory.GetGrain<IOdinReminderRegistrationGrain>(GrainKey);
}
