namespace Odin.Orleans;

public static class OrleansStoreNames
{
	public const string GrainPersistence = "DDBStore"; // todo: rename to PersistenceStore
	public const string GrainMemory = "MemoryStore";
	public const string PubSub = "PubSubStore";
	public const string Crud = "Crud";
	public const string GrainDirectory = "OdinDirectory";
}

public static class OrleansStreamProviderNames
{
	public const string InternalMessaging = "InternalMessaging";
	public const string DataPlatformPrefix = "DataPlatform";
	public const string Bifrost = "Bifrost";
}

public static class SharedStreamNamespaceNames
{
	public const string ContentChanged = "ContentChanged";
	public const string InvalidateCache = "InvalidateCache";
}
