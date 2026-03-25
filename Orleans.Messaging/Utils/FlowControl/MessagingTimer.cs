namespace Orleans.Messaging.FlowControl;

public class MessagingTimer : IAsyncDisposable
{
	private readonly Func<TimerControl, Task> _work;
	private readonly MessagingTimerOptions _opts;
	private PeriodicTimer _loopTimer;
	private Task _timerLoopTask;
	private Func<Exception, Task> _onError;
	private readonly TimerControl _timerControl;

	public MessagingTimer(
		Func<TimerControl, Task> work,
		MessagingTimerOptions opts
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

	public MessagingTimer OnError(Func<Exception, Task> onError)
	{
		_onError += onError;
		return this;
	}

	public async ValueTask DisposeAsync()
		=> await StopTimer();

	private async Task TimerLoop(Func<TimerControl, Task> work)
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

public record struct MessagingTimerOptions(TimeSpan Interval, TimeSpan? InitialDelay = null);

public class TimerControl
{
	private readonly MessagingTimer _timer;

	public TimerControl(MessagingTimer timer)
	{
		_timer = timer;
	}

	public ValueTask StopTimer()
		=> _timer.DisposeAsync();
}
