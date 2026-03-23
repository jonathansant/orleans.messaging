namespace Odin.Core.Error;

public static class ErrorContextTypes
{
	public const string Global = "global";
	public const string Validation = "validation";
	public const string Authorization = "authorization";
	public const string Database = "database";
	public const string DataStore = "datastore";
}

public static class ErrorOriginNames
{
	public const string Asgard = "asgard";
	public const string JobScheduler = "job-scheduler";
	public const string Hermod = "hermod";
	public const string Mimir = "mimir";
	public const string Elysium = "elysium";
	public const string Grizzly = "grizzly";
	public const string Horizon = "horizon";
	public const string Midgard = "midgard";
	public const string Vanir = "vanir";
	public const string Heimdall = "heimdall";
	public const string Vor = "vor";
	public const string IdentityServer = "identity-server";
	public const string PaymentIQ = "paymentIQ";
	public const string Contentful = "contentful";
	public const string Hera = "hera";
	public const string Njord = "njord"; // todo: this should be moved out
}
