using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace Odin.Core.Config;

public interface IConfigMerger
{
	T? Get<T>(string key, string sectionPath, string? parentSectionPath = null, params DynamicSection[] sections)
		where T : new();

	T? Get<T>(string key, Func<IEnumerable<T>> getConfigs)
		where T : new();

	T? Get<T>(string key, List<string> sectionPaths, params DynamicSection[] sections)
		where T : new();
}

public class ConfigMerger : IConfigMerger
{
	private readonly IConfiguration _configuration;
	private readonly ConcurrentDictionary<string, object?> _mergedConfigs = new();
	private readonly JsonMergeSettings _jsonMergeSettings = new JsonMergeSettings
	{
		MergeArrayHandling = MergeArrayHandling.Replace,
	};

	public ConfigMerger(
		IConfiguration configuration
	)
	{
		_configuration = configuration;
	}

	public T? Get<T>(string key, string sectionPath, string? parentSectionPath = null, params DynamicSection[] sections)
		where T : new()
	{
		var paths = new List<string>(2) { sectionPath };
		if (!parentSectionPath.IsNullOrEmpty())
			paths.Add($"{parentSectionPath}:{sectionPath}");
		return Get<T>(key, paths, sections);
	}

	public T? Get<T>(string key, List<string> sectionPaths, params DynamicSection?[] sections)
		where T : new()
		=> Get($"{key}/{sectionPaths.ToDebugString()}", () =>
		{
			var mergeConfigs = new List<T>(sectionPaths.Select(x => _configuration.GetSection(x).Get<T>()!));

			foreach (var sectionPath in sectionPaths)
				foreach (var dynamicSection in sections.Where(section => section != null))
				{
					var config = dynamicSection!.Get<T>(sectionPath);

					if (config != null)
						mergeConfigs.Add(config);
				}

			return mergeConfigs;
		});

	public T? Get<T>(string key, Func<IEnumerable<T>> getConfigs)
		where T : new()
	{
		if (_mergedConfigs.TryGetValue(key, out var configObj))
			return (T?)configObj;

		var configJObj = JObject.FromObject(new T());

		foreach (var section in getConfigs())
		{
			if (section == null) continue;

			var sectionJObj = JObject.FromObject(section);

			configJObj.Merge(sectionJObj, _jsonMergeSettings);
			//config.InjectFromExcludingNulls(dynamicSection);
		}

		var result = configJObj.ToObject<T>();
		_mergedConfigs[key] = result;
		return result;
	}
}
