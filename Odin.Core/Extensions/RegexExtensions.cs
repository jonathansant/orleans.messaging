
// ReSharper disable once CheckNamespace

using Odin.Core;

namespace System.Text.RegularExpressions;

public static class RegexExtensions
{
	private static readonly Regex InterpolationRegex = new Regex(@"\{(\w+(?:\.\w+)*)\}", RegexOptions.Compiled);

	/// <summary>
	/// Replaces string tokens with arguments (interpolation).
	/// </summary>
	/// <param name="re">regex instance.</param>
	/// <param name="template">Template used for replacement/interpolation. e.g. <c>"/person/{id}"</c></param>
	/// <param name="args">Arguments to interpolate with template.</param>
	/// <param name="ignoreMissingTokens">Ignores tokens that are not found</param>
	/// <returns>Returns string with tokens replaced.</returns>
	public static string ReplaceTokens(this Regex re, string template, IDictionary<string, object> args, bool ignoreMissingTokens)
	{
		return re.Replace(template, Evaluator);

		string Evaluator(Match match)
		{
			var paramName = match.Groups[1].Value;
			var found = args.TryGetValue(paramName, out var paramValue);
			if (!found && !ignoreMissingTokens)
				throw new ArgumentNullException(nameof(args), $"Template has a param which its value is not provided. Param: '{paramName}'");

			return paramValue?.ToString() ?? match.Value;
		}
	}

	/// <summary>
	/// Replaces string tokens with arguments (interpolation).
	/// </summary>
	/// <param name="template">Template used for replacement/interpolation. e.g. <c>"/person/{id}"</c></param>
	/// <param name="args">Arguments to interpolate with template.</param>
	/// <param name="ignoreMissingTokens">Ignores tokens that are not found</param>
	/// <returns>Returns string with tokens replaced.</returns>
	public static string FromTemplate(this string template, IDictionary<string, object> args, bool ignoreMissingTokens = false)
		=> InterpolationRegex.ReplaceTokens(template, args, ignoreMissingTokens);

	public static IEnumerable<string> GetTokens(this string template)
		=> InterpolationRegex.Matches(template).Select(x => x.Groups[1].Value);

	public static string ToOrPattern(this IEnumerable<string> values)
	{
		var valuesList = values.ToList();

		if (valuesList.IsNullOrEmpty())
			return string.Empty;

		if (valuesList.Count == 1)
			return valuesList[0];

		return "(" + valuesList.JoinTokens("|") + ")";
	}
}
