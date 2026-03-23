using FluentValidation;
using FluentValidation.Validators;

namespace Odin.Core.Validation;

public class OdinMinLengthValidator<T> : MinimumLengthValidator<T>
{
	private readonly bool _allowEmpty;

	public OdinMinLengthValidator(int min, bool allowEmpty) : base(min)
	{
		_allowEmpty = allowEmpty;
	}

	public OdinMinLengthValidator(Func<T, int> min) : base(min)
	{
	}

	public override bool IsValid(ValidationContext<T> context, string? value)
	{
		if (value == null || (_allowEmpty && value.Length == 0))
			return true;

		return base.IsValid(context, value);
	}
}
