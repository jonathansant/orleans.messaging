using FluentValidation;
using FluentValidation.Results;

namespace Odin.Core.Validation;

public class ValidationContextableResult<T>
{
	public ValidationContext<T> Context { get; set; }
	public ValidationResult Result { get; set; }
}
