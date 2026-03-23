namespace Odin.Core.Config;

public record AwsConfig
{
	public string AccessKey { get; set; }
	public string SecretKey { get; set; }
	public string ProfileName { get; set; }
	public string Service { get; set; }
	public string CloudWatchGroupName { get; set; }
	public S3StorageConfig S3Storage { get; set; }
}

public record S3StorageConfig
{
	public string BucketName { get; set; }
	public int FileExpiryNoOfDays { get; set; }
	public S3UrlSignerConfig UrlSigner { get; set; }
}

public record S3UrlSignerConfig
{
	public string AccessKey { get; set; }
	public string SecretKey { get; set; }
}
