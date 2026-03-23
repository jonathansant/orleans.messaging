using Orleans.Streams;

namespace Odin.Orleans.Core.Streaming;

internal sealed class StreamWrapper<TMessage> : StreamWrapper
{
	private readonly ILogger _logger;

	public Func<TMessage, StreamSequenceToken, Task> Handler { get; set; }
	public IAsyncStream<TMessage> Stream { get; set; }

	public StreamWrapper(ILogger logger)
	{
		_logger = logger;
	}

	public override async Task SubscribeAsync()
	{
		if (Resume)
		{
			var handlers = await Stream.GetAllSubscriptionHandles();
			if (handlers.Any())
			{
				var resumeTasks = handlers
					.Select(streamSubscriptionHandle => streamSubscriptionHandle.ResumeAsync(Handler, OnMessageDeliveryError))
					.Cast<Task>();

				await Task.WhenAll(resumeTasks);
				return;
			}
		}

		await Stream.SubscribeAsync(Handler, OnMessageDeliveryError);
	}

	public override async Task UnsubscribeAsync()
	{
		var handles = await Stream.GetAllSubscriptionHandles();
		await Task.WhenAll(handles.Select(handle => handle.UnsubscribeAsync()));
	}

	private Task OnMessageDeliveryError(Exception exception)
	{
		// todo: add retry logic
		_logger.Error(
			exception,
			"Delivering message failed: {ProviderName} - {Namespace} - {Guid} due to: {Message} exception data: {Data} target site: {TargetSite} source: {Source} inner exception: {InnerException} stack trace: {StackTrace}",
			Stream.ProviderName,
			Stream.StreamId.Key,
			Stream.StreamId.Namespace,
			exception.Message,
			exception.Data,
			exception.TargetSite,
			exception.Source,
			exception.InnerException,
			exception.StackTrace
		);

		return Task.CompletedTask;
	}
}

internal abstract class StreamWrapper
{
	public abstract Task SubscribeAsync();
	public abstract Task UnsubscribeAsync();
	public bool Resume { get; set; }
}
