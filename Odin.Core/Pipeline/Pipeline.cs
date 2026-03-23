using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Odin.Core.Pipeline;

public interface IPipelineInput;

public interface IPipelineOutput;

public abstract record PipelineRequest<TInput, TOutput>
	where TInput : IPipelineInput
	where TOutput : IPipelineOutput
{
	public TInput Input { get; set; }
	public Func<TInput, Task<TOutput>> Function { get; set; }
}

public record PipelineConfigBase
{
	public bool IsEnabled { get; set; }
	public int Capacity { get; set; } = 10;
	public int MaximumQueueSize { get; set; } = 500;
}

public interface IPipeline<in TRequest, TInput, TOutput>
	where TRequest : PipelineRequest<TInput, TOutput>
	where TInput : IPipelineInput
	where TOutput : IPipelineOutput
{
	ValueTask<Task<TOutput>> Add(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// A helper utility class that allows to control the rate of generation of asynchronous activities.
/// Maintains a pipeline of asynchronous operations up to a given maximal capacity and blocks the task if the pipeline
/// gets too deep before enqueued operations are not finished.
/// Effectively adds a back-pressure to the caller.
/// </summary>
public class Pipeline<TRequest, TInput, TOutput, TOptions> : IPipeline<TRequest, TInput, TOutput>
	where TRequest : PipelineRequest<TInput, TOutput>
	where TInput : IPipelineInput
	where TOutput : IPipelineOutput
	where TOptions : PipelineConfigBase
{
	private readonly ILogger<Pipeline<TRequest, TInput, TOutput, TOptions>> _logger;
	private readonly Channel<TRequest> _buffer;

	public Pipeline(IOptions<TOptions> options, ILogger<Pipeline<TRequest, TInput, TOutput, TOptions>> logger)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Value.Capacity, 1);

		_logger = logger;
		_buffer = Channel.CreateBounded<TRequest>(
			new BoundedChannelOptions(options.Value.Capacity)
		);
	}

	/// <summary>
	/// Do not use within a grain call
	/// </summary>
	public async ValueTask<Task<TOutput>> Add(TRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(request.Function);

		var tcs = new TaskCompletionSource<TOutput>();

		await _buffer.Writer.WriteAsync(request, cancellationToken);

		_ = Task.Run(
			async () =>
			{
				try
				{
					_logger.LogInformation("Processing request from pipeline buffer");
					var result = await request.Function.Invoke(request.Input);
					tcs.SetResult(result);
				}

				catch (Exception e)
				{
					tcs.SetException(e);
				}

				finally
				{
					await _buffer.Reader.ReadAsync(cancellationToken);
					_logger.LogInformation("Removing request from pipeline buffer");
				}
			},
			cancellationToken
		);

		return tcs.Task;
	}
}
