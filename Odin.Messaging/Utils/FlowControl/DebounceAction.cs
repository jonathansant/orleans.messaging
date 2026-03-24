namespace Odin.Messaging.FlowControl;

public class DebounceAction : IAsyncDisposable
{
	private readonly Func<Task> _action;
	private readonly TimeSpan _debounceTime;
	private CancellationTokenSource _currentCancellation = new();
	private Func<Exception, Task> _onError;

	public DebounceAction(Func<Task> action, TimeSpan debounceTime)
	{
		_action = action;
		_debounceTime = debounceTime;
	}

	public void Execute()
	{
		Task.Yield();

		_currentCancellation.Cancel();
		_currentCancellation = new();

		Task.Delay(_debounceTime, _currentCancellation.Token)
			.ContinueWith(
				async task =>
				{
					if (!task.IsCompletedSuccessfully)
						return;

					try
					{
						await _action();
					}
					catch (Exception ex)
					{
						if (_onError is not null)
							await _onError(ex);
						else
							throw;
					}
				}
			);
	}

	public DebounceAction OnError(Func<Exception, Task> onError)
	{
		_onError += onError;
		return this;
	}

	public ValueTask DisposeAsync()
	{
		_currentCancellation.Cancel();
		return ValueTask.CompletedTask;
	}
}
