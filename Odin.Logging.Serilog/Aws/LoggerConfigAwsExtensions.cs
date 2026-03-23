using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Odin.Core.Config;
using Serilog;
using Serilog.Sinks.AwsCloudWatch;

namespace Odin.Logging.Serilog.Aws;

public static class LoggerConfigAwsExtensions
{
	public static LoggerConfiguration AddAwsCloudWatch(this LoggerConfiguration loggerConfig, AwsConfig config)
	{
		var options = new CloudWatchSinkOptions
		{
			LogGroupName = config.CloudWatchGroupName
		};

		IAmazonCloudWatchLogs client = new AmazonCloudWatchLogsClient(
			GetCredentials(config),
			RegionEndpoint.GetBySystemName(config.Service)
		);

		loggerConfig.WriteTo.AmazonCloudWatch(options, client);

		return loggerConfig;
	}

	private static AWSCredentials GetCredentials(AwsConfig awsConfig)
	{
		if (!string.IsNullOrEmpty(awsConfig.ProfileName) && new SharedCredentialsFile().TryGetProfile(awsConfig.ProfileName, out var credentialProfile))
		{
			return new SessionAWSCredentials(
				credentialProfile.Options.AccessKey,
				credentialProfile.Options.SecretKey,
				credentialProfile.Options.Token
			);
		}

		if (!string.IsNullOrEmpty(awsConfig.AccessKey) && !string.IsNullOrEmpty(awsConfig.SecretKey))
			return new BasicAWSCredentials(awsConfig.AccessKey, awsConfig.SecretKey);

		try
		{
			var credentials = FallbackCredentialsFactory.GetCredentials();
			return credentials;
		}
		catch (AmazonServiceException)
		{
			throw new ArgumentException($"{nameof(AwsConfig.AccessKey)} and {nameof(AwsConfig.SecretKey)} must be provided.");
		}
	}
}
