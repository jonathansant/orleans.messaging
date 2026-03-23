using System.Globalization;

namespace Odin.Core;

public static class NumericExtensions
{
	/// <summary>
	/// Enables foreach loops and linq on an int
	/// </summary>
	/// <param name="input"></param>
	public static IEnumerator<int> GetEnumerator(this int input)
	{
		for (var i = 0; i < input; i++)
			yield return i;
	}

	/// <summary>
	/// Returns the length of a decimal number excluding the decimal point, sign and trailing zeros following decimal point
	/// </summary>
	public static int GetNumberOfDigits(this decimal value)
	{
		var stringRepresentation = Normalize(value).ToString(CultureInfo.InvariantCulture);

		var digitCount = 0;

		foreach (var c in stringRepresentation)
			if (char.IsDigit(c))
				digitCount++;

		return digitCount;

		static decimal Normalize(decimal value)
			=> value / 1.000000000000000000000000000000000m;
	}

	public static int? NullifyDefault(this int value)
		=> value == default ? null : value;
	public static int? NullifyDefault(this int? value)
		=> value == 0 ? null : value;

	public static decimal? NullifyDefault(this decimal value)
		=> value == default ? null : value;
	public static decimal? NullifyDefault(this decimal? value)
		=> value == decimal.Zero ? null : value;
}
