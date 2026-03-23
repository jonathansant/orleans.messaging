namespace Odin.Microservice.Hosting.Orleans;

public class RedisConfig
{
	public List<string> Servers { get; set; }
	public string Password { get; set; }
	public bool UseSsl { get; set; }
	public string SslHost { get; set; }
	public float FieldSizeWarningThreshold { get; set; } = 2;
	public TimeSpan ExecutionDurationWarnThreshold { get; set; } = TimeSpan.FromMilliseconds(400);
}
