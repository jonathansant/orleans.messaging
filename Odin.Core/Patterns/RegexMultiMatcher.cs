using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Odin.Core.Patterns;

/// <summary>
/// Collection regex patterns.
/// </summary>
public class RegexMultiMatcher
{
	private readonly ConcurrentDictionary<string, bool> _resolved = new ConcurrentDictionary<string, bool>();
	private readonly List<Regex> _patterns = new List<Regex>();

	public int PatternsCount => _patterns.Count;

	public RegexMultiMatcher()
	{
	}

	public RegexMultiMatcher(IEnumerable<string> patterns)
	{
		AddRange(patterns);
	}

	public void Add(string pattern)
	{
		_patterns.Add(new Regex(pattern, RegexOptions.Compiled));
		_resolved.Clear();
	}

	public void AddRange(IEnumerable<string> values)
	{
		foreach (var value in values)
			Add(value);
	}

	/// <summary>
	/// Determines whether specified value matches to any regex specified.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool Matches(string value)
		=> _resolved.GetOrAdd(value, _ =>
		{
			foreach (var pattern in _patterns)
			{
				if (!pattern.IsMatch(value))
					continue;

				return true;
			}
			return false;
		});
}
