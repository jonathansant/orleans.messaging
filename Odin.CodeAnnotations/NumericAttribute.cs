namespace Odin.CodeAnnotations;

[AttributeUsage(AttributeTargets.Property)]
public class NumericAttribute : Attribute
{
	public readonly NumericInfo NumericInfo = new();

	public NumericAttribute(
		double min = double.MaxValue, double max = double.MaxValue, int precision = -1, int scale = -1, bool allowZero = true
	)
	{
		NumericInfo.AllowZero = allowZero;
		NumericInfo.Min = min == double.MaxValue ? null : min;
		NumericInfo.Max = max == double.MaxValue ? null : max;
		NumericInfo.Precision = precision == -1 ? null : precision;
		NumericInfo.Scale = scale == -1 ? null : scale;
	}
}

public record NumericInfo
{
	/// <summary>
	/// Determines if zero is allowed.
	/// </summary>
	public bool AllowZero { get; set; } = true;

	/// <summary>
	/// Minimum value of the number.
	/// </summary>
	public object? Min { get; set; }

	/// <summary>
	/// Maximum value of the number.
	/// </summary>
	public object? Max { get; set; }

	/// <summary>
	/// Amount of digits in the number (including after the dot). e.g. when 5 ###.##
	/// </summary>
	public int? Precision { get; set; }

	/// <summary>
	/// Amount of digits after the dot. e.g. 2 = ###.20
	/// </summary>
	public int? Scale { get; set; }

	public void Merge(NumericInfo? other)
	{
		if (other == null)
			return;

		AllowZero = other.AllowZero;
		if (other.Min != null)
			Min = other.Min;
		if (other.Max != null)
			Max = other.Max;
		if (other.Precision != null)
			Precision = other.Precision;
		if (other.Scale != null)
			Scale = other.Scale;
	}
}

public static class NumericInfoExtensions
{
	private static readonly Dictionary<DataType, NumericInfo> NumericInfoMap = new()
	{
		{DataType.MoneySmall, new() {Min = -999_999.99, Max = 999_999.99, Precision = 8, Scale = 2}},
		{DataType.Money, new() {Min = -999_999_999.99, Max = 999_999_999.99, Precision = 11, Scale = 2}},
		{DataType.Crypto, new() {Min = -999_999.999_999_99, Max = 999_999.999_999_99, Precision = 14, Scale = 8}},
		{DataType.SortOrder, new()
		{
			AllowZero = true,
			Min = -999999999999.999999999999999999,
			Max = 999999999999.999999999999999999,
			Precision = 30,
			Scale = 18
		}},
	};

	public static NumericInfo? ResolveDefaultNumericInfo(this DataType dataType)
	{
		NumericInfoMap.TryGetValue(dataType, out var value);
		return value;
	}
}
