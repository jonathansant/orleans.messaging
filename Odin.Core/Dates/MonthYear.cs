using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Odin.Core;

// todo: add unit tests
[GenerateSerializer]
public readonly record struct MonthYear : IComparable<MonthYear>, IFormattable
{
	public static MonthYear UtcNow => From(DateTime.UtcNow);
	public static MonthYear Now => From(DateTime.Now);

	/// <inheritdoc cref="DateTime.Year"/>
	[Id(0)]
	public int Year { get; }

	/// <inheritdoc cref="DateTime.Month"/>
	[Id(1)]
	public int Month { get; }

	public MonthYear(int year, int month)
	{
		if (year < 1 || year > DateTime.MaxValue.Year)
			throw new ArgumentOutOfRangeException(nameof(month), $"Year must be between 1 and {DateTime.MaxValue.Year}.");
		if (month is < 1 or > 12)
			throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
		Year = year;
		Month = month;
	}

	/// <inheritdoc cref="DateTime.DaysInMonth"/>
	public int DaysInMonth() => DateTime.DaysInMonth(Year, Month);

	/// <summary>
	/// Converts the MonthYear instance to a <see cref="DateOnly"/> instance with the specified <paramref name="day"/>.
	/// </summary>
	/// <param name="day">Day for the date only.</param>
	/// <returns></returns>
	public DateOnly ToDateOnly(int day) => new(Year, Month, day);

	public override string ToString() => ToString("D", null);

	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		format ??= "D";

		if (formatProvider == null || formatProvider == CultureInfo.CurrentCulture)
		{
			return format.ToUpperInvariant() switch
			{
				"D" => $"{Year:D4}-{Month:D2}",
				"Y" => $"{Year:D4}",
				"M" => $"{Month:D2}",
				_ => throw new FormatException($"The '{format}' format specifier is not supported."),
			};
		}

		throw new FormatException($"The '{format}' format specifier is not supported.");
	}

	public int CompareTo(MonthYear other)
	{
		var yearComparison = Year.CompareTo(other.Year);
		return yearComparison != 0
			? yearComparison
			: Month.CompareTo(other.Month);
	}

	/// <summary>
	/// Returns a MonthYear instance that is set to the date part of the specified dateTime.
	/// </summary>
	/// <param name="dateTime">The DateTime instance.</param>
	/// <returns>The MonthYear instance composed of the date part of the specified input time dateTime instance.</returns>
	public static MonthYear From(DateTime dateTime) => new(dateTime.Year, dateTime.Month);
	public static MonthYear From(DateOnly date) => new(date.Year, date.Month);

	/// <summary>
	/// Simple parsing from string. e.g. '2021-12'
	/// </summary>
	/// <param name="value">Value to parse.</param>
	/// <returns></returns>
	/// <exception cref="FormatException"></exception>
	public static MonthYear Parse(string? value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(value);
		if (value.Length != 7)
			throw new FormatException($"Invalid format for MonthYear: '{value}' must be 7 chars long");

		var parts = value.Split('-');
		if (parts.Length != 2)
			throw new FormatException($"Invalid format for MonthYear: '{value}'");

		return new(int.Parse(parts[0]), int.Parse(parts[1]));
	}
}

// NOTE: System.Text.Json serializer is not tested
public sealed class MonthYearJsonConverter : System.Text.Json.Serialization.JsonConverter<MonthYear>
{
	public override MonthYear Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> MonthYear.Parse(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, MonthYear value, JsonSerializerOptions options)
	{
		var isoDate = value.ToString();
		writer.WriteStringValue(isoDate);
	}
}

public sealed class MonthYearNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
{
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		var transformedValue = ((MonthYear)value!).ToString();
		writer.WriteValue(transformedValue);
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		=> reader.Value is string valueStr ? MonthYear.Parse(valueStr) : null;

	public override bool CanConvert(Type objectType)
		=> objectType == typeof(MonthYear) || Nullable.GetUnderlyingType(objectType) == typeof(MonthYear);
}
