using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace Serilog;

internal sealed class StringObfuscationEnricher : ILogEventEnricher
{
	private const string ObfuscationMask = "*****";
	private const string EscapeCharPattern = "(\\\\)+";

	private readonly IEnumerable<PropertyPatterns> _offensiveProperties;

	public StringObfuscationEnricher(IEnumerable<PropertyPatterns> offensiveProperties)
	{
		_offensiveProperties = offensiveProperties;
	}

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		foreach (var property in _offensiveProperties)
		{
			if (!logEvent.Properties.TryGetValue(property.PropertyName, out var value))
				continue;

			var sanitizedValue = Regex.Replace(value.ToString(), EscapeCharPattern, string.Empty);
			sanitizedValue = property
				.Patterns
				.Aggregate(sanitizedValue, (current, pattern) => Regex.Replace(current, pattern, ObfuscationMask, RegexOptions.Multiline));

			logEvent.AddOrUpdateProperty(new LogEventProperty(property.PropertyName, new ScalarValue(sanitizedValue)));
		}
	}
}

public class PropertyPatterns
{
	public string PropertyName { get; set; }
	public IList<string> Patterns { get; set; }

	public static PropertyPatterns Create(string name)
		=> new PropertyPatterns
		{
			PropertyName = name,
			Patterns = new List<string>()
		};
}

public static class PropertyPatternsExtensions
{
	public static PropertyPatterns AddPattern(this PropertyPatterns propertyPatterns, string pattern)
	{
		propertyPatterns.Patterns.Add(pattern);
		return propertyPatterns;
	}
}
