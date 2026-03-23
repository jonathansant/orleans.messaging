namespace Odin.Core.FlowControl;

public class OdinTimer : IAsyncDisposable
{
	private readonly Func<OdinTimerControl, Task> _work;
	private readonly OdinTimerOptions _opts;
	private PeriodicTimer _loopTimer;
	private Task _timerLoopTask;
	private Func<Exception, Task> _onError;
	private readonly OdinTimerControl _timerControl;

	public OdinTimer(
		Func<OdinTimerControl, Task> work,
		OdinTimerOptions opts
	)
	{
		_work = work;
		_opts = opts;
		_timerControl = new(this);
	}

	public void StartTimer()
	{
		_loopTimer = new(_opts.Interval);
		_timerLoopTask = TimerLoop(_work);
	}

	public async Task StopTimer()
	{
		_loopTimer?.Dispose();
		if (_timerLoopTask is { } task)
		{
			await task;
			_timerLoopTask = null;
		}
	}

	public OdinTimer OnError(Func<Exception, Task> onError)
	{
		_onError += onError;
		return this;
	}

	public async ValueTask DisposeAsync()
		=> await StopTimer();

	private async Task TimerLoop(Func<OdinTimerControl, Task> work)
	{
		await Task.Yield();

		if (_opts.InitialDelay is { } delay)
			await Task.Delay(delay);

		do
		{
			try
			{
				await work(_timerControl);
			}
			catch (Exception ex)
			{
				if (_onError != null)
					await _onError(ex);
				else
					throw;
			}
		} while (await _loopTimer.WaitForNextTickAsync());
	}
}

public record struct OdinTimerOptions(TimeSpan Interval, TimeSpan? InitialDelay = null);

public class OdinTimerControl
{
	private readonly OdinTimer _timer;

	public OdinTimerControl(OdinTimer timer)
	{
		_timer = timer;
	}

	public ValueTask StopTimer()
		=> _timer.DisposeAsync();
}
