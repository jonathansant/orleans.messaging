using FluentValidation.Validators;

namespace Odin.Core.Error;

public abstract class AsyncPropertyBaseValidator<T, TProperty> : AsyncPropertyValidator<T, TProperty>
{
	protected override string GetDefaultMessageTemplate(string errorCode)
		=> $"{{{ValidatorContextExtensions.ErrorMessage}}}";
}
