using HeyRed.Mime;
using Humanizer;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Odin.Core.Json;
using System.Collections.Concurrent;
using System.Data.HashFunction;
using System.Data.HashFunction.xxHash;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using StableHash = Odin.Core.Utils.StableHash;

namespace Odin.Core;

public static class StringExtensions
{
	private const string DefaultMimeExtension = "application/octet-stream";

	public static string SubstringLastIndexOf(this string key, char value)
		=> key[(key.LastIndexOf(value) + 1)..];

	/// <summary>
	/// Convert key to key normalized. Used for Orleans key or url to escape '/'.
	/// </summary>
	/// <param name="key">Key to normalize.</param>
	public static string? ToKeyEncode(this string? key)
		=> key.IsNullOrEmpty() ? key : key.Replace("/", "$$");

	/// <summary>
	/// Convert key to decode. Used for Orleans key or url.
	/// </summary>
	/// <param name="key">Key to decode.</param>
	public static string? ToKeyDecode(this string? key)
		=> key.IsNullOrEmpty() ? key : key.Replace("$$", "/");

	/// <summary>
	/// Hyphenate (kebab/dashed) value e.g. 'ChickenWings' => 'Chicken-wings'.
	/// </summary>
	/// <param name="value">Value to Hyphenate.</param>
	/// <returns></returns>
	public static string ToHyphenCase(this string value)
	{
		var chars = value.ToList();
		for (var i = 0; i < chars.Count - 1; i++)
		{
			if (char.IsWhiteSpace(chars[i]) || !char.IsUpper(chars[i + 1]))
				continue;
			chars[i + 1] = char.ToLower(chars[i + 1]);
			chars.Insert(i + 1, '-');
		}

		return new string(chars.ToArray());
	}

	/// <summary>
	/// Determine whether string is null or empty.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <returns>Returns true when null or empty.</returns>
	public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
		=> string.IsNullOrEmpty(value);

	/// <summary>
	/// Determine whether string is null or empty or matches the expected value.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="expectedValue">Value to check.</param>
	/// <returns></returns>
	public static bool IsNullOrEmptyOrEqual(this string? value, string expectedValue)
		=> value.IsNullOrEmpty() || value == expectedValue;

	/// <summary>
	/// Invokes an action when value is null or empty.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="action">Action to invoke when null/empty.</param>
	public static void IfNullOrEmptyThen(this string? value, Action action)
	{
		if (value.IsNullOrEmpty())
			action();
	}

	/// <summary>
	/// Invokes an action when value is not null or empty.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="action">Action to invoke when not null/empty.</param>
	public static void IfNotNullOrEmptyThen(this string? value, Action<string> action)
	{
		if (!value.IsNullOrEmpty())
			action(value);
	}

	/// <summary>
	/// Invokes an action and return a new value, when value is null/empty, else return original value.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="func">Action to invoke when null/empty which returns a new value.</param>
	/// <returns>Returns new value from func when null/empty or original value.</returns>
	public static string IfNullOrEmptyReturn(this string? value, Func<string> func)
		=> value.IsNullOrEmpty() ? func() : value;

	/// <summary>
	/// Returns defaultValue when null/empty, else return original value.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="defaultValue">Value to return when null/empty.</param>
	public static string IfNullOrEmptyReturn(this string? value, string defaultValue) => value.IsNullOrEmpty() ? defaultValue : value;

	/// <summary>
	/// Trims all leading occurrences of a string from the string.
	/// </summary>
	/// <param name="value">Current value.</param>
	/// <param name="trimString">Value to trim.</param>
	/// <returns></returns>
	public static string TrimStart(this string value, string trimString)
	{
		var result = value;
		while (result.StartsWith(trimString))
		{
			result = result.Substring(trimString.Length);
		}

		return result;
	}

	/// <summary>
	/// Trims all trailing occurrences of a string from the string.
	/// </summary>
	/// <param name="value">Current value.</param>
	/// <param name="trimValue">Value to trim.</param>
	/// <returns></returns>
	public static string TrimEnd(this string value, string trimValue)
	{
		var result = value;
		while (result.EndsWith(trimValue))
		{
			result = result.Substring(0, result.Length - trimValue.Length);
		}

		return result;
	}

	/// <summary>
	/// Trims all spaces from the string.
	/// </summary>
	/// <param name="value">Current value.</param>
	public static string? TrimAll(this string? value)
		=> value.IsNullOrEmpty() ? value : Regex.Replace(value, @"\s", "");

	public static string? RemoveWhiteSpacing(this string? value) => value.IsNullOrEmpty() ? value : value.Replace(" ", string.Empty);

	public static string? ReplaceStringWithRegex(this string? text, string pattern, string value)
		=> text.IsNullOrEmpty() ? text : Regex.Replace(text, pattern, value);

	// todo: move to be reusable
	internal static readonly IxxHash HashFunction = xxHashFactory.Instance.Create();

	public static async Task<string> ComputeHash(this string text)
		=> (await ToHashValue(text)).AsBase64String();

	public static async Task<string> ComputeHashAsHex(this string text)
		=> (await ToHashValue(text)).AsHexString();

	public static string ComputeHashSync(this string text)
		=> ToHashValueSync(text).AsBase64String();

	public static string ComputeHashAsHexSync(this string text)
		=> ToHashValueSync(text).AsHexString();

	private static Task<IHashValue> ToHashValue(this string text)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		return HashFunction.ComputeHashAsync(stream);
	}

	private static IHashValue ToHashValueSync(this string text)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		return HashFunction.ComputeHash(stream);
	}

	/// <summary>
	/// Get a consistent hashcode number based on a string.
	/// </summary>
	/// <param name="text">Value to generate hashcode for.</param>
	public static int ToHashCode(this string text)
	{
		var value = HashFunction.ComputeHash(Encoding.UTF8.GetBytes(text));
		return BitConverter.ToInt32(value.Hash, 0);
	}

	/// <summary>
	/// Get a consistent hashcode number based on a string.
	/// </summary>
	/// <param name="text">Value to generate hashcode for.</param>
	public static async Task<int> ToHashCodeAsync(this string text)
	{
		var value = await ToHashValue(text);
		return BitConverter.ToInt32(value.Hash, 0);
	}

	/// <summary>
	/// Get a consistent number between 0-<paramref name="maxValue"/> based on a string value.
	/// </summary>
	/// <param name="value">String value to generate consistent partition index.</param>
	/// <param name="maxValue">The exclusive upper bound of the number returned e.g. 12 (0-11)</param>
	public static int ToPartitionIndex(this string value, uint maxValue)
		=> (int)(StableHash.ComputeHash(value) % (int)maxValue);

	/// <summary>
	/// Converts a string to an enum value of the type T.
	/// </summary>
	/// <typeparam name="T">Enum Type</typeparam>
	/// <param name="value">Current value</param>
	/// <returns></returns>
	public static T ToEnum<T>(this string value) => (T)Enum.Parse(typeof(T), value, true);

	/// <summary>
	/// Converts a string to a bool value.
	/// </summary>
	/// <param name="value">Current value</param>
	/// <returns></returns>
	public static bool ToBool(this string value) => bool.Parse(value);

	/// <summary>
	/// Coerces a data-bound value to a boolean, to work as flag based e.g. null/false = false else true.
	/// </summary>
	/// <param name="value">Current value</param>
	public static bool ToBoolCoerce(this string? value)
		=> value != null && !string.Equals(value, bool.FalseString, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Converts a string to decimal or null.
	/// </summary>
	/// <param name="value">Value to convert.</param>
	public static decimal? ToDecimalOrDefault(this string value)
		=> decimal.TryParse(value, out var result) ? result : null;

	/// <summary>
	/// Converts an epoch unix timestamp string to date time value.
	/// </summary>
	/// <param name="value">Current value</param>
	/// <returns></returns>
	public static DateTime ToDateTimeFromEpochTimestamp(this string value)
		=> long.Parse(value).ToDateTimeFromUnix();

	public static DateTimeOffset ToUtcDateTimeOffsetFromEpochTimestamp(this string value)
		=> DateTimeOffset.FromUnixTimeSeconds(long.Parse(value));

	/// <summary>
	/// Converts string to base64 encoded.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static string ToBase64Encode(this string value)
	{
		var bytes = Encoding.UTF8.GetBytes(value);
		return Convert.ToBase64String(bytes);
	}

	/// <summary>
	/// Converts a byte array into a Base64-encoded string representation of its hexadecimal values.
	/// </summary>
	/// <param name="hashBytes">The byte array to convert into a Base64-encoded hexadecimal string.</param>
	/// <returns>A Base64-encoded string of the input byte array's hexadecimal representation.</returns>
	public static string ToBase64EncodeHex(this byte[] hashBytes)
	{
		var hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
		var bytes = Encoding.UTF8.GetBytes(hex);
		return Convert.ToBase64String(bytes);
	}

	/// <summary>
	/// Decodes a base64 encoded string to string.
	/// </summary>
	/// <param name="value">Encoded String</param>
	/// <returns>Decoded string</returns>
	public static string DecodeBase64String(this string value)
	{
		var data = Convert.FromBase64String(value);
		return Encoding.UTF8.GetString(data);
	}

	/// <summary>
	/// Removes all currency and alpha characters from the string.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static string RemoveCurrency(this string value)
		=> Regex.Replace(value, "[^0-9]", "", RegexOptions.Compiled);

	public static string GetCurrency(this string value)
		=> Regex.Replace(value, "[0-9,.]", "", RegexOptions.Compiled);

	public static string? Truncate(this string? value, int maxLength)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return value.Length <= maxLength ? value : value.Substring(0, maxLength);
	}

	/// <summary>
	/// Retrieve an array of strings from a comma separated string (removing any whitespaces).
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static string[] GetCommaSeparatedValues(this string value)
		=> value.GetDelimiterSeparatedValues(',');

	private static string[] GetDelimiterSeparatedValues(this string value, char delimiter)
		=> value.Split(delimiter, StringSplitOptions.TrimEntries);

	private static readonly Regex ToKebabCaseSplitChars = new(
		@"[^\d\w]+|(?<=[a-zA-Z])(?=[0-9])|(?<=[0-9])(?=[a-zA-Z])",
		RegexOptions.Compiled
	);

	/// <summary>
	/// Separates groups of alpha chars from numeric chars with an underscore then converts to kebab case e.g. abc123 => abc-123
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static string ToKebabCase(this string value)
	{
		// separate alpha from numeric characters
		var substrings = ToKebabCaseSplitChars.Split(value);

		// join with an underscore and convert to kebab case
		return string.Join("_", substrings).Kebaberize();
	}

	/// <summary>
	/// Joins an IEnumerable with a specified token
	/// </summary>
	/// <param name="tokens"></param>
	/// /// <param name="joinToken"></param>
	public static string? JoinTokens(this IEnumerable<string>? tokens, string joinToken = "+")
	{
		var tokenList = tokens?.ToList();

		return tokenList.IsNullOrEmpty()
			? null
			: string.Join(joinToken, tokenList);
	}

	public static bool HasToken(this string value, string token = "+")
		=> value.Contains(token);

	// todo: move string transforms extensions as separate e.g. kebab, slug, camel, pascal etc...
	private static readonly Regex ToSlugifyTrimChars = new(@"[^\d\w\s-.]+", RegexOptions.Compiled);
	private static readonly string ToSlugifyRemoveConsecutiveChars = "-.";

	public static string ToSlugify(this string value)
		=> ToSlugifyTrimChars.Replace(value, string.Empty)
			.Kebaberize()
			.RemoveMime()
			.RemoveConsecutiveChars(ToSlugifyRemoveConsecutiveChars)
			.ToLower()
			.Trim('-', '.');

	/// <summary>
	/// Remove Mime - this is a recursive call which removes mime from the end of the string
	/// </summary>
	/// <param name="str">String to remove mime at the end</param>
	/// <returns>string excluding the mime extension</returns>
	public static string RemoveMime(this string str)
	{
		if (MimeTypesMap.GetMimeType(str).Equals(DefaultMimeExtension, StringComparison.OrdinalIgnoreCase))
			return str;

		var num = str.LastIndexOf('.');
		if (num == -1)
			return str;

		var key = str[num..].ToLower();
		var ext = str[(num + 1)..].ToLower();

		return RemoveMime(Regex.Replace(str, $"{key}$", $"-{ext}"));
	}

	private static readonly ConcurrentDictionary<string, Regex> RemoveConsecutiveCharsCache = new();

	/// <summary>
	/// Replaces consecutive characters to single e.g. -- => -
	/// </summary>
	/// <param name="value">Value to change.</param>
	/// <param name="chars">Characters to remove consecutive</param>
	/// <returns>String without containing consecutive dashes.</returns>
	public static string RemoveConsecutiveChars(this string value, string chars)
	{
		var pattern = RemoveConsecutiveCharsCache.GetOrAdd(chars, _ => new($"([{chars}])\\1+", RegexOptions.Compiled));
		return pattern.Replace(value, "$1");
	}

	/// <summary>
	/// Serializes an object to Json.
	/// </summary>
	/// <param name="value">Object to serialize.</param>
	/// <returns></returns>
	[Obsolete("Use ToJsonString instead (NOTE: It uses System.Text instead of Newtonsoft).")]
	public static string ToStringJson(this object value)
		=> JsonConvert.SerializeObject(value);

	/// <summary>
	/// Deserialize an object from json string to object.
	/// </summary>
	/// <param name="value">Json string to deserialize.</param>
	public static T? FromJson<T>(this string value)
		=> JsonSerializer.Deserialize<T>(value, JsonUtils.JsonBasicSettings);

	/// <summary>
	/// Deserialize an object from json string to object.
	/// </summary>
	/// <param name="value">Json string to deserialize.</param>
	/// <param name="returnType">Return type to cast to.</param>
	/// <returns></returns>
	public static object? FromJson(this string value, Type returnType)
		=> JsonSerializer.Deserialize(value, returnType, JsonUtils.JsonBasicSettings);

	public static bool EqualsIgnoreCase(this string value, string b)
		=> string.Equals(value, b, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Camelize strings split words by <paramref name="token"/>.
	/// </summary>
	/// <param name="value">Value to transform.</param>
	/// <param name="token">Token separator.</param>
	/// <returns></returns>
	public static string ToCamelCase(this string value, char token = '.')
	{
		if (value.Length == 0)
			return value;

		var words = value.Split(token);
		var sb = new StringBuilder();

		for (var i = 0; i < words.Length; i++)
		{
			if (i != 0)
				sb.Append(token);

			sb.Append(words[i].Titleize().Camelize());
		}

		return sb.ToString();
	}

	/// <summary>
	/// Pascalize strings split words by <paramref name="token"/>.
	/// </summary>
	/// <param name="value">Value to transform.</param>
	/// <param name="token">Token separator.</param>
	/// <returns></returns>
	public static string ToPascalCase(this string value, char token = '.')
	{
		if (value.Length == 0)
			return value;

		var words = value.Split(token);
		var sb = new StringBuilder();

		for (var i = 0; i < words.Length; i++)
		{
			if (i != 0)
				sb.Append(token);

			sb.Append(words[i].Titleize().Pascalize());
		}

		return sb.ToString();
	}

	public static string ToSnakeCase(this string value)
		=> value.Underscore();

	/// <summary>
	/// Checks whether any value exists.
	/// </summary>
	/// <param name="value">Value(s) to perform checks for.</param>
	/// <param name="valuesToCheck">Value(s) to compare with.</param>
	/// <param name="comparer">Comparer to use.</param>
	/// <returns></returns>
	public static bool Any(this StringValues value, StringValues valuesToCheck, IEqualityComparer<string?>? comparer = null)
		=> valuesToCheck.Any(x => value.Contains(x, comparer));

	/// <summary>
	/// Transforms first character of string to upper case and the rest to lower case.
	/// </summary>
	/// <param name="value">Value to transform.</param>
	/// <returns></returns>
	public static string ToUpperFirst(this string value)
		=> value[0].ToString().ToUpper() + value[1..].ToLower();

	public static string ToSeparateWords(this string value)
		=> InsertSpaceBetweenWordsRegex.Replace(value, " $&").TrimStart();

	private static readonly Regex ReplaceSpecialCharsBySpaceRegex = new(@"[^a-zA-Z0-9]+", RegexOptions.Compiled);
	private static readonly Regex InsertSpaceBetweenWordsRegex = new("[A-Z](?=[a-z0-9]+)|[A-Z]+(?![a-z0-9])", RegexOptions.Compiled);

	/// <summary>
	/// Replace special characters with the specified string (defaults to whitespace). e.g. "hello-world" => "hello world"
	/// </summary>
	/// <param name="value">Value to transform.</param>
	/// <param name="replaceWith">String to replace with.</param>
	/// <returns></returns>
	public static string ReplaceSpecialCharsWith(this string value, string replaceWith = " ")
		=> ReplaceSpecialCharsBySpaceRegex.Replace(value, replaceWith);

	public static string ReplaceSpecialCharsWithExcept(this string value, string exceptChars, string replaceWith = "")
	{
		var pattern = $"[^a-zA-Z0-9{Regex.Escape(exceptChars)}]";
		return Regex.Replace(value, pattern, replaceWith);
	}

	private const string PathSeparator = "/";

	/// <summary>
	/// Normalize path start with "/".
	/// </summary>
	/// <param name="path">Path to normalize.</param>
	public static string? NormalizePathStart(this string? path)
	{
		if (path == null)
			return null;

		if (!path.StartsWith(PathSeparator))
			path = PathSeparator + path;

		return path;
	}

	public static TEnum ToEnumFromJson<TEnum>(this string stringValue) where TEnum : struct
		=> JsonConvert.DeserializeObject<TEnum>(stringValue.ToJsonString());

	public static TEnum? ToEnumFromJsonOrDefault<TEnum>(this string? stringValue) where TEnum : struct
		=> stringValue?.ToEnumFromJson<TEnum>();

	public static string? ToUrlEncode(this string? value)
		=> value == null ? null : Uri.EscapeDataString(value);

	public static string? ToUrlDecode(this string? value)
		=> value == null ? null : Uri.UnescapeDataString(value);

	private static readonly Regex FieldNamePrefixRegex = new(@"^([$_]+)(.*)", RegexOptions.Compiled);
	private static readonly Regex LeadingDigitsRegex = new(@"^[0-9]+", RegexOptions.Compiled);

	/// <summary>
	/// Converts a string to a valid field name using camelCase convention.
	/// Preserves leading $ or _ prefixes.
	/// </summary>
	/// <param name="value">Value to convert.</param>
	/// <returns>A camelCased field name.</returns>
	public static string ToFieldName(this string? value)
	{
		if (value.IsNullOrEmpty())
			return string.Empty;

		var trimmed = value.Trim();
		var match = FieldNamePrefixRegex.Match(trimmed);

		if (match.Success)
		{
			var prefix = match.Groups[1].Value;
			var rest = match.Groups[2].Value;
			var camelCased = rest.Humanize().Camelize();
			return prefix + camelCased;
		}

		var withoutLeadingDigits = LeadingDigitsRegex.Replace(trimmed, string.Empty);
		return withoutLeadingDigits.Humanize().Camelize();
	}
}
