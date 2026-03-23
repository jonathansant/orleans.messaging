using Odin.Core.FlowControl;
using Orleans.Concurrency;

namespace Odin.Orleans.Core;

public interface IOdinFetcherGrain : IOdinGrain;

public interface IOdinFetcherGrainContract<TResult> : IOdinGrainContract
{
	[AlwaysInterleave]
	Task<TResult> GetById(string id);

	[AlwaysInterleave]
	Task<List<TResult>> GetAll();

	Task<List<TResult>> RefetchAll();
}

/// <summary>
/// Base Grain for fetching data remotely as a collection and cache locally in order to avoid n+1 requests.
/// </summary>
/// <typeparam name="TResult"></typeparam>
public abstract class OdinFetcherGrain<TResult>(
	ILogger<OdinFetcherGrain<TResult>> logger,
	ILoggingContext loggingContext
) : OdinGrain(logger, loggingContext), IOdinFetcherGrainContract<TResult>, IIncomingGrainCallFilter
{
	private readonly Cooldown _fetchCooldown = new(TimeSpan.FromSeconds(5), o => o.FailureMode = CooldownFailureMode.InvalidState);

	protected Dictionary<string, TResult> AllData { get; private set; }

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();

		await LoadIfNeeded(rethrow: false);
	}

	public Task<TResult> GetById(string id)
	{
		AllData.TryGetValue(id, out var result);
		return Task.FromResult(result);
	}

	public Task<List<TResult>> GetAll()
		=> Task.FromResult(AllData.Values.ToList());

	public async Task<List<TResult>> RefetchAll()
	{
		await LoadIfNeeded(skipDataAlreadySetCheck: true);
		return await GetAll();
	}

	public virtual async Task Invoke(IIncomingGrainCallContext context)
	{
		await LoadIfNeeded();
		await context.Invoke();
	}

	private async Task LoadIfNeeded(bool rethrow = true, bool skipDataAlreadySetCheck = false)
	{
		if (!skipDataAlreadySetCheck && AllData != null)
			return;

		try
		{
			await _fetchCooldown.TryExecuteAsync(async () => AllData = await FetchAll());
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Error while fetching for {primaryKey}", PrimaryKey);

			if (rethrow)
				throw;
		}
	}

	protected abstract Task<Dictionary<string, TResult>> FetchAll();
}
