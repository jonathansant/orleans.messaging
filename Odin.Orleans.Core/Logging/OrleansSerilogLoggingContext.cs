using Odin.Logging.Serilog;
using Serilog;

namespace Odin.Orleans.Core.Logging;

public class OrleansSerilogLoggingContext : SerilogLoggingContext
{
	public OrleansSerilogLoggingContext(
		IDiagnosticContext diagnosticContext
	) : base(diagnosticContext)
	{
	}

	public override IEnumerable<LogContextData> GetAll()
	{
		var orleansLogContext = OdinOrleansRequestContext.GetLogContext();

		if (orleansLogContext?.Count > 0)
			Data.AddRangeOverride(orleansLogContext);

		return Data.Values;
	}

	public override void Set(LogContextData contextData)
	{
		base.Set(contextData);

		var data = OdinOrleansRequestContext.GetLogContext();
		data ??= new();
		data[contextData.PropertyName] = contextData;

		OdinOrleansRequestContext.SetLogContext(data);
	}
}
