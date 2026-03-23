using FluentValidation;
using FluentValidation.Internal;

namespace Odin.Core.Validation;

public class CompositeValidatorSelector : IValidatorSelector
{
	private readonly IEnumerable<IValidatorSelector> _selectors;

	public CompositeValidatorSelector(IEnumerable<IValidatorSelector> selectors)
	{
		_selectors = selectors;
	}

	public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context)
		=> _selectors.All(s => s.CanExecute(rule, propertyPath, context));
}
