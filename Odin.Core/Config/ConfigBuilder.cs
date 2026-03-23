using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Odin.Core.Config;

public interface IConfigBuildItem
{
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class KeyConfigBuildItem : IConfigBuildItem
{
	protected string DebuggerDisplay => $"Key: '{Key}', Source: '{Source}', IsOptional: {IsOptional}";

	public string Key { get; set; } = null!;
	public bool IsOptional { get; set; }
	public string? Source { get; set; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ObjectConfigBuildItem : IConfigBuildItem
{
	protected string DebuggerDisplay => $"Config: {Config}";

	/// <summary>
	/// Configuration object to be used.
	/// </summary>
	public JObject? Config { get; set; }
}

public interface IConfigBuilder
{
	/// <summary>
	/// Set base path for config files.
	/// </summary>
	/// <param name="basePath">Base path to set.</param>
	/// <returns></returns>
	IConfigBuilder SetBasePath(string basePath);

	IConfigBuilder SetDefaultSource(string source);

	/// <summary>
	/// Add file.
	/// </summary>
	/// <param name="file"></param>
	/// <param name="source"></param>
	/// <returns></returns>
	Task AddFile(string file, string? source = null);

	/// <summary>
	/// Add all config files.
	/// </summary>
	/// <returns></returns>
	Task AddAllFiles(string? basePath = null, string? source = null);

	/// <summary>
	/// Add all config files specified.
	/// </summary>
	/// <param name="files">Files to add.</param>
	/// <param name="source"></param>
	/// <returns></returns>
	Task AddAllFiles(IEnumerable<string> files, string? source = null);

	/// <summary>
	/// Clear and add all files.
	/// </summary>
	/// <param name="basePath"></param>
	/// <param name="source"></param>
	/// <returns></returns>
	Task SetAllFiles(string? basePath = null, string? source = null);

	/// <summary>
	/// Clear all.
	/// </summary>
	void Clear();

	JObject Merge(JObject config, JObject sourceConfig);

	JObject Merge(JObject config, string targetKey, bool isOptional, string? source = null);

	/// <summary>
	/// Compose the specified configurations together.
	/// </summary>
	/// <param name="configs">Configurations to be composed together.</param>
	/// <returns>Return composed configs.</returns>
	JObject? Build(params IConfigBuildItem[] configs);

	/// <summary>
	/// Update/Add config section and merge with the specified config if already exists.
	/// </summary>
	/// <param name="key">Key to update.</param>
	/// <param name="value">Value to update with.</param>
	/// <param name="source"></param>
	void UpdateWith(string key, object value, string? source = null);

	JObject? Get(string key, string? source = null, bool isOptional = false);
	void Set(string key, JObject config, string? source = null);

	/// <summary>
	/// Configure file key parser which parses key to use from file path.
	/// </summary>
	/// <param name="configure"></param>
	/// <returns></returns>
	IConfigBuilder WithFileKeyParser(Func<string, string> configure);
}

public class ConfigBuilder : IConfigBuilder
{
	private string _defaultSource = "file";
	private string? _basePath;
	private readonly ILogger _logger;
	private readonly ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, JObject?>>> _sources = new();
	private readonly Regex _fileKeyRegex;
	private Func<string, string> _fileKeyParser;
	private readonly JsonMergeSettings _jsonMergeSettings = new()
	{
		MergeArrayHandling = MergeArrayHandling.Replace
	};

	public ConfigBuilder(
		ILogger<ConfigBuilder> logger
	)
	{
		_logger = logger;
		_fileKeyRegex = new Regex($"[^{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}]*$");
		_fileKeyParser = DefaultParseKeyFromFilePath;
	}

	public IConfigBuilder SetBasePath(string basePath)
	{
		_basePath = basePath;
		return this;
	}

	public IConfigBuilder SetDefaultSource(string source)
	{
		_defaultSource = source;
		return this;
	}

	/// <inheritdoc />
	public async Task AddFile(string file, string? source = null)
	{
		source = source.IfNullOrEmptyReturn(_defaultSource);
		var jObject = await JsonNetExtensions.ReadFromFileAsync(file);
		var key = ParseKeyFromFilePath(file);
		_logger.Debug("Adding config file {key}, {configFilePath}", key, file);
		Set(key, jObject, source);
	}

	/// <inheritdoc />
	public async Task AddAllFiles(IEnumerable<string> files, string? source = null)
	{
		source = source.IfNullOrEmptyReturn(_defaultSource);
		await files.ForEachAsync(file => AddFile(file, source));
	}

	/// <inheritdoc />
	public async Task AddAllFiles(string? basePath = null, string? source = null)
	{
		source = source.IfNullOrEmptyReturn(_defaultSource);

		basePath = basePath.IfNullOrEmptyReturn(_basePath!);
		var configFiles = Directory.GetFiles(basePath, "*.config.*", SearchOption.AllDirectories);
		_logger.Debug("Adding {count} config file(s). BasePath {basePath}", configFiles.Length, basePath);

		await AddAllFiles(configFiles, source);
	}

	public async Task SetAllFiles(string? basePath = null, string? source = null)
	{
		Clear();
		await AddAllFiles(basePath, source);
	}

	public void Clear() => _sources.Clear();

	public JObject? Get(string key, string? source = null, bool isOptional = false)
	{
		source = source.IfNullOrEmptyReturn(_defaultSource);
		if (!_sources.TryGetValue(source, out var configs))
			return null;

		var result = configs.Value!.GetValueOrDefault(key, null);

		if (result == null && !isOptional)
			throw new OdinKeyNotFoundException($"{source}:{key}", $"Config ({source}) '{key}' not found!");

		return result;
	}

	public void Set(string key, JObject? config, string? source = null)
	{
		source = source.IfNullOrEmptyReturn(_defaultSource);

		var configs = _sources.GetOrAdd(source, k => new(() => new())).Value;
		if (_logger.IsEnabled(LogLevel.Debug))
			_logger.Debug("Set - Setting config - key: {key}, source: {source}, {config}", key, source, config?.ToString()!);
		configs[key] = config;
	}

	public JObject Merge(JObject config, string targetKey, bool isOptional, string? source = null)
	{
		var configTarget = Get(targetKey, source, isOptional);
		if (configTarget == null)
			return config;

		var clonedConfig = (JObject)config.DeepClone();
		clonedConfig.Merge(configTarget, _jsonMergeSettings);
		return clonedConfig;
	}

	public JObject Merge(JObject config, JObject sourceConfig)
	{
		var clonedConfig = (JObject)config.DeepClone();
		clonedConfig.Merge(sourceConfig, _jsonMergeSettings);
		return clonedConfig;
	}

	/// <inheritdoc />
	public JObject? Build(params IConfigBuildItem[] configs)
	{
		JObject? config = null;
		foreach (var configBuildItem in configs)
		{
			var sourceConfig = configBuildItem switch
			{
				KeyConfigBuildItem keyConfigBuildItem => Get(
					keyConfigBuildItem.Key,
					keyConfigBuildItem.Source,
					keyConfigBuildItem.IsOptional
				),
				ObjectConfigBuildItem objConfigBuildItem => objConfigBuildItem.Config,
				_ => throw new ArgumentOutOfRangeException(nameof(configBuildItem))
			};

			if (sourceConfig == null)
			{
				_logger.Debug("Build - SourceConfig null {@configBuildItem}", configBuildItem);
				continue;
			}

			if (config == null)
			{
				config = sourceConfig;
				continue;
			}

			config = Merge(config, sourceConfig);
		}
		return config;
	}

	public void UpdateWith(string key, object value, string? source = null)
		=> UpdateWith(key, JObject.FromObject(value), source);

	public void UpdateWith(string key, JObject value, string? source = null)
	{
		var existingConfig = Get(key, source, isOptional: true);
		if (existingConfig == null)
		{
			Set(key, value, source);
			return;
		}

		existingConfig.Merge(value, _jsonMergeSettings);
	}

	public IConfigBuilder WithFileKeyParser(Func<string, string> configure)
	{
		_fileKeyParser = configure;
		return this;
	}

	private string ParseKeyFromFilePath(string filePath)
		=> _fileKeyParser(filePath);

	private string DefaultParseKeyFromFilePath(string filePath)
	{
		var fileNameWithExt = _fileKeyRegex.Match(filePath).Value;
		// todo: improve regex to do the below
		var key = fileNameWithExt.Replace(".config.json", "");
		return key;
	}
}
