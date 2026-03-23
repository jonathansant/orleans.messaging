namespace Odin.Core.Config;

public record TelemetryConfig
{
	public bool IsEnabled { get; set; }
	public TracesConfig Traces { get; set; }
	public MetricsConfig Metrics { get; set; }
}

public record TracesConfig
{
	public bool IsEnabled { get; set; }
	public ApmTracesConfig Apm { get; set; }
	public bool IsHttpEventListenerEnabled { get; set; }
}

public record MetricsConfig
{
	public bool IsEnabled { get; set; }
	public ConsoleMetricsConfig Console { get; set; }
	public PrometheusMetricsConfig Prometheus { get; set; }
	public string[] MeterNames { get; set; }
}

public record ConsoleMetricsConfig
{
	public bool IsEnabled { get; set; }
}

public record PrometheusMetricsConfig
{
	public bool IsEnabled { get; set; }
	public string Path { get; set; }
}

public record ApmTracesConfig
{
	public bool IsEnabled { get; set; }
	public string Url { get; set; }
	public string SecureToken { get; set; }
	public bool IsHttp { get; set; }
	public bool VerifyServerCert { get; set; }
	public LogLevel LogLevel { get; set; }
	public string CaptureBody { get; set; }

	public int TransactionMaxSpans { get; set; }
	public int TransactionSampleRate { get; set; }

	public int StackTraceLimit { get; set; }
	public int SpanFramesMinDurationInMilliseconds { get; set; }
	public int SpanSampleRate { get; set; }

	public int FlushInterval { get; set; }
}
