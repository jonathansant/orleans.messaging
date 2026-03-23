using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Odin.Core.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DateAttribute : ValidationAttribute
{
	private readonly string _dateFormat;

	public DateAttribute(string? dateFormat = null)
	{
		_dateFormat = dateFormat ?? "yyyy-MM-dd";
	}

	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var valueStr = value?.ToString();
		return valueStr.IsNullOrEmpty() || DateTime.TryParseExact(valueStr, _dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
			? ValidationResult.Success
			: new ValidationResult(ErrorMessage ?? "error.invalid:date");
	}
}
