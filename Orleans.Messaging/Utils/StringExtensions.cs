using System.Text.RegularExpressions;

namespace Orleans.Messaging.Utils;

public static class StringExtensions
{
	private static readonly Regex InterpolationRegex = new(@"\{(\w+(?:\.\w+)*)\}", RegexOptions.Compiled);

	/// <summary>
	/// Replaces string tokens with arguments (interpolation).
	/// </summary>
	/// <param name="template">Template used for replacement/interpolation. e.g. <c>"/person/{id}"</c></param>
	/// <param name="args">Arguments to interpolate with template.</param>
	/// <param name="ignoreMissingTokens">Ignores tokens that are not found</param>
	/// <returns>Returns string with tokens replaced.</returns>
	public static string FromTemplate(this string template, IDictionary<string, object> args, bool ignoreMissingTokens = false)
	{
		return InterpolationRegex.Replace(template, Evaluator);

		string Evaluator(Match match)
		{
			var paramName = match.Groups[1].Value;
			var found = args.TryGetValue(paramName, out var paramValue);
			if (!found && !ignoreMissingTokens)
				throw new ArgumentNullException(nameof(args), $"Template has a param which its value is not provided. Param: '{paramName}'");

			return paramValue?.ToString() ?? match.Value;
		}
	}
}
