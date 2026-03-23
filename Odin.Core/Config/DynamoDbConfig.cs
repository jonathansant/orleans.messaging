using Odin.Core.App;

namespace Odin.Core.Config;

public class DynamoDbConfig
{
	public string Prefix { get; set; }
	public string Service { get; set; }

	public DynamoDbTableConfig StateTable { get; set; } = new DynamoDbTableConfig("GrainState");
	public DynamoDbTableConfig PubSubTable { get; set; } = new DynamoDbTableConfig("PubSubState");
	public DynamoDbTableConfig ReminderTable { get; set; } = new DynamoDbTableConfig("Reminders") { WriteCapacityUnits = 10, ReadCapacityUnits = 10 };
	public DynamoDbTableConfig ClusterTable { get; set; } = new ClusteringDynamoTableConfig { WriteCapacityUnits = 10, ReadCapacityUnits = 10 };
}

public class DynamoDbTableConfig
{
	public DynamoDbTableConfig(string tableName)
	{
		TableName = tableName;
	}

	public string TableName { get; set; }
	public int ReadCapacityUnits { get; set; } = 30;
	public int WriteCapacityUnits { get; set; } = 30;
	public string Version { get; set; }

	protected virtual string GenerateTableNameTemplate(string prefix, IAppInfo appInfo)
		=> appInfo.ClusterGroup.IsNullOrEmpty()
			? $"{appInfo.ProductName}-{prefix}-{TableName}-{appInfo.Environment}"
			: $"{appInfo.ProductName}-{prefix}-{appInfo.ClusterGroup}.{TableName}-{appInfo.Environment}";

	public string GenerateTableName(string prefix, IAppInfo appInfo)
	{
		var tableName = GenerateTableNameTemplate(prefix, appInfo);

		if (!Version.IsNullOrEmpty())
			tableName = $"{tableName}-v{Version}";
		return tableName;
	}
}

public class ClusteringDynamoTableConfig : DynamoDbTableConfig
{
	public ClusteringDynamoTableConfig()
		: base("Clustering")
	{ }

	protected override string GenerateTableNameTemplate(string prefix, IAppInfo appInfo)
		=> $"{TableName}-{appInfo.Environment}";
}
