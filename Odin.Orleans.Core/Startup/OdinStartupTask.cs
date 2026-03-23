using Polly;
using Polly.Retry;

namespace Odin.Orleans.Core.Startup;

public abstract class OdinStartUpTask : IStartupTask
{
	private readonly AsyncRetryPolicy _retryPolicy;

	protected OdinStartUpTask(ILogger<OdinStartUpTask> logger)
    {
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
				retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), 60)), // exponential backoff capped at 1 minute
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning("Startup task retry {RetryCount} in {Delay}ms. Exception: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome?.Message);
                });
    }

    public virtual async Task Execute(CancellationToken cancellationToken)
	    => await _retryPolicy.ExecuteAsync(async () =>
	    {
		    await OdinExecute(cancellationToken);
	    });

    protected virtual Task OdinExecute(CancellationToken cancellationToken)
	    => throw new NotImplementedException();
}
