namespace Odin.Core.Config;

public class MsSqlConfig : ConnectionStringConfig
{
	public CredentialsConfig Migration { get; set; }
	public CredentialsConfig App { get; set; }
	public bool IsEncrypted { get; set; }

	public string BuildAppConnectionString()
		=> BuildConnectionString(App);

	public string BuildMigrationConnectionString()
		=> BuildConnectionString(Migration);
}
