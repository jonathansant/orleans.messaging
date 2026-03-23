using Odin.Core.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Odin.Logging.Serilog;

public class SerilogLoggingContext : ILoggingContext
{
	private readonly IDiagnosticContext _diagnosticContext;
	private readonly Dictionary<string, IDisposable> _disposables = new Dictionary<string, IDisposable>();
	protected readonly Dictionary<string, LogContextData> Data = new Dictionary<string, LogContextData>();

	public SerilogLoggingContext(
		IDiagnosticContext diagnosticContext
	)
	{
		_diagnosticContext = diagnosticContext;
	}

	public virtual IEnumerable<LogContextData> GetAll() => Data.Values;

	public virtual void Set(LogContextData contextData)
	{
		if (Data.TryGetValue(contextData.PropertyName, out var previousContextData) && previousContextData == contextData)
			return;

		Data[contextData.PropertyName] = contextData;

		if (_disposables.TryGetValue(contextData.PropertyName, out var propDisposable))
			propDisposable.Dispose();

		propDisposable = LogContext.PushProperty(contextData.PropertyName, contextData.Value, contextData.DestructureObject);
		_disposables[contextData.PropertyName] = propDisposable;
		_diagnosticContext.Set(contextData.PropertyName, contextData.Value, contextData.DestructureObject);
	}

	public virtual IDisposable BindScope()
	{
		var enrichers = GetAll().Select(x => x.ToLogEventEnricher()).ToArray();
		return LogContext.Push(enrichers);
	}

	public void Rebind()
	{
		foreach (var ctxData in GetAll().ToList())
			Set(ctxData);
	}

	public virtual void Dispose()
	{
		Data.Clear();
		_disposables.Values.ToList().ForEach(x => x.Dispose());
	}
}

public static class LogContextExtensions
{
	public static ILogEventEnricher ToLogEventEnricher(this LogContextData data)
		=> new PropertyEnricher(data.PropertyName, data.Value, data.DestructureObject);
}
