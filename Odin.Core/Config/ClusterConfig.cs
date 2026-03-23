namespace Odin.Core.Config;

public record ClusterConfig
{
	public int Size { get; set; } = 4;
	public bool ExtendProbeTimeout { get; set; } = false;
	public bool EnableIndirectProbes { get; set; } = true;
	public int LocalHealthDegradationMonitoringPeriod { get; set; } = 10;

	public int DefunctSiloCleanupPeriodSeconds { get; set; } = 15;
	public int DefunctSiloExpirationSeconds { get; set; } = 15;
	public int TableRefreshTimeoutSeconds { get; set; } = 10;
	public int IAmAliveTablePublishTimeoutSeconds { get; set; } = 10;
	public int ProbeTimeoutSeconds { get; set; } = 10;
	public int NumMissedProbesLimit { get; set; } = 15;
	public int MaxJoinAttemptTimeSeconds { get; set; } = 120;
	public int NumVotesForDeathDeclaration { get; set; } = 2;

	public int GracefulShutdownTimeoutSeconds { get; set; } = 240;

	public bool UseKubernetes { get; set; } = true;
}
