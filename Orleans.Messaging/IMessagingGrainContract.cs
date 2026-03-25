using Orleans.Concurrency;

namespace Orleans.Messaging;

/// <summary>
/// Messaging Grain public contract interface. Grain interfaces should implement this.
/// </summary>
public interface IMessagingGrainContract
{
	/// <summary>
	/// Cause force activation in order for grain to be warmed up/preloaded.
	/// </summary>
	Task Activate();

	/// <summary>
	/// Cause force activation in order for grain to be warmed up/preloaded.
	/// </summary>
	[OneWay]
	Task ActivateOneWay();
}
