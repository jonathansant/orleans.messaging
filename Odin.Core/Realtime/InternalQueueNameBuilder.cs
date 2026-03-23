using Odin.Core.App;
using System.Text.RegularExpressions;

namespace Odin.Core.Realtime;

public interface IInternalQueueNameBuilder
{
	string Build(string microservice, string queueName, string version);
}

public class InternalQueueNameBuilder : IInternalQueueNameBuilder
{
	private readonly IAppInfo _appInfo;
	private readonly QueueNameBuilderOptions _options;

	public InternalQueueNameBuilder(
		IAppInfo appInfo,
		QueueNameBuilderOptions options
	)
	{
		_appInfo = appInfo;
		_options = options;
	}

	public string Build(string microservice, string queueName, string version)
		=> _options.Template.FromTemplate(new Dictionary<string, object>
			{
				{ "env", _appInfo.Environment},
				{ "serviceName", microservice},
				{ "name", queueName},
				{ "version", version}
			})
			.ToLowerInvariant()
	;
}

public class QueueNameBuilderOptions
{
	public string Template { get; set; }
}
