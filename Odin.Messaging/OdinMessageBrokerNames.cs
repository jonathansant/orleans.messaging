namespace Odin.Messaging;

public static class OdinMessageBrokerNames
{
	public static string Default => Platform;
	public const string Platform = "messageBroker";
	public const string Bifrost = "bifrostMessageBroker";
	public const string JobScheduler = "jobSchedulerMessageBroker";
	public const string Reconciliation = "reconciliationMessageBroker";
	public const string NotificationEvents = "notificationEventsMessageBroker";
	public const string LastCommittedExternal = "lastCommittedMessageBroker";
}
