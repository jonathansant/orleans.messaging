namespace Odin.Core.Utils;

public static class RandomUtils
{
	private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
	private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + LowerChars;
	private const string Symbols = "`!@#$%^&*()_-+={[}]|\\:;'<,>.?/";
	private static readonly Random Random = new Random();
	private static readonly HashSet<string> Data = new HashSet<string>();

	/// <summary>
	/// Generates a random string consisting of uppercase and lowercase alpha characters e.g. 'chgKr'.
	/// </summary>
	/// <param name="minLength">Minimum generated string length defaults to 3.</param>
	/// <param name="maxLength">Maximum generated string length defaults to 10.</param>
	/// <returns>string</returns>
	public static string GenerateString(int minLength = 3, int maxLength = 10)
	{
		var stringBuilder = new StringBuilder(maxLength);
		var stringLength = GenerateNumber(minLength, maxLength);

		for (var i = 0; i < stringLength; i++)
		{
			var num = GenerateNumber(0, Chars.Length - 1);
			stringBuilder.Append(Chars[num]);
		}

		return stringBuilder.ToString();
	}

	/// <summary>
	/// Generates a random string consisting of lowercase characters only e.g. 'yolo'.
	/// </summary>
	/// <param name="minLength">Minimum generated string length defaults to 3.</param>
	/// <param name="maxLength">Maximum generated string length defaults to 10.</param>
	/// <returns>string</returns>
	public static string GenerateStringLower(int minLength = 3, int maxLength = 10)
	{
		var stringBuilder = new StringBuilder(maxLength);
		var stringLength = GenerateNumber(minLength, maxLength);

		for (var i = 0; i < stringLength; i++)
		{
			var num = GenerateNumber(0, LowerChars.Length - 1);
			stringBuilder.Append(LowerChars[num]);
		}

		return stringBuilder.ToString();
	}

	/// <summary>
	/// Generate a random number e.g. "21" e.g. min: 1, max: 5 = 1-5.
	/// </summary>
	/// <param name="min">Inclusive minimum value allowed (defaults: 1)</param>
	/// <param name="max">Inclusive maximum value allowed (defaults: 10)</param>
	public static int GenerateNumber(int min = 1, int max = 10) => Random.Next(min, max + 1);

	/// <summary>
	/// Generate a random number e.g. "1200000000000000".
	/// </summary>
	/// <param name="min">Minimum value allowed (defaults: 1)</param>
	/// <param name="max">Maximum value allowed (defaults: 9,223,372,036,854,775,807)</param>
	public static long GenerateLongNumber(long min = 1, long max = long.MaxValue) => Random.NextInt64(min, max);

	/// <summary>
	/// Generates a random number with character length ranging from min to max e.g. "2157" when the passed min = 4 and exp = 4.
	/// </summary>
	/// <param name="min">The minimum value of the number in length (defaults: 1)</param>
	/// <param name="max">The maximum value of the number in length (defaults: 10)</param>
	/// <returns></returns>
	public static int GenerateNumberByExponent(int min = 1, int max = 10)
		=> GenerateNumber((int)Math.Pow(10, min - 1), (int)Math.Pow(10, max) - 1);

	/// <summary>
	/// Generate a random timespan.
	/// </summary>
	/// <param name="minSeconds">Minimum seconds to generate timespan with.</param>
	/// <param name="maxSeconds">Maximum seconds to generate timespan with.</param>
	public static TimeSpan GenerateTimeSpan(int minSeconds = 5, int maxSeconds = 30)
		=> new TimeSpan(0, 0, 0, GenerateNumber(minSeconds, maxSeconds));

	/// <summary>
	/// Generate a random unique string consisting of a timestamp and random characters.
	/// </summary>
	/// <returns>string</returns>
	public static string GetUniqueString()
	{
		lock (Data)
		{
			string result;

			do
			{
				result = $"{DateTime.UtcNow.GetEpochTimeStamp()}.{GenerateString(5).ToLower()}";
			} while (Data.Contains(result));

			Data.Add(result);

			return result;
		}
	}

	/// <summary>
	/// Generate a random unique string consisting lowercase letter, uppercase letter, number and symbol.
	/// </summary>
	/// <param name="length">Minimum generated string length defaults to 12.</param>
	/// <returns></returns>
	public static string GeneratePassword(int length = 12)
	{
		if (length <= 7)
			throw new ArgumentException("Number must be greater than 8");

		var output = new List<string>();

		var lowerCasedLetters = GenerateString(1, 1).ToLower();
		output.Add(lowerCasedLetters);
		var upperCasedLetters = GenerateString(1, 1).ToUpper();
		output.Add(upperCasedLetters);
		var letters = GenerateString(length - 4, length - 4);
		output.Add(letters);
		var numbers = GenerateNumber(1, 1).ToString();
		output.Add(numbers);
		var symbols = RandomSymbolsGenerator(1, 1);
		output.Add(symbols);
		var randomOrder = output.OrderBy(i => Guid.NewGuid());

		return string.Join(null, randomOrder);
	}

	/// <summary>
	/// Generate a random unique string consisting of symbols.
	/// </summary>
	/// <param name="minLength">Minimum generated string length defaults to 1.</param>
	/// <param name="maxLength">Maximum generated string length defaults to 2.</param>
	/// <returns></returns>
	public static string RandomSymbolsGenerator(int minLength = 1, int maxLength = 2)
	{
		var stringBuilder = new StringBuilder();
		var length = GenerateNumber(minLength, maxLength);
		while (0 < length--)
		{
			stringBuilder.Append(Symbols[Random.Next(Symbols.Length)]);
		}

		return stringBuilder.ToString();
	}
}
