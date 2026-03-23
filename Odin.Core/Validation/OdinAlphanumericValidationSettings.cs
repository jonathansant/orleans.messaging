namespace Odin.Core.Validation;

public class OdinAlphanumericValidationSettings
{
	public bool AllowWhitespace { get; set; }

	public bool AllowNumeric { get; set; }

	public bool AllowLeadingNumeric { get; set; }

	public bool AllowTrailingNumeric { get; set; }

	public bool AllowSymbols { get; set; }

	public List<string> AllowedSymbols { get; set; }

	public List<string> NonAllowedSymbols { get; set; } = new();

	public bool AllowLeadingSymbols { get; set; }

	public bool AllowTrailingSymbols { get; set; }

	// will only be effective if no trimming is done
	public bool AllowLeadingWhitespace { get; set; }

	// will only be effective if no trimming is done
	public bool AllowTrailingWhitespace { get; set; }

	public bool AllowUpperCasing { get; set; }

	public bool AllowMimeTypeExtensionLike { get; set; }

	public int MaxLength { get; set; }

	public int MinLength { get; set; }
	public bool AllowEmpty { get; set; } = true;

	public bool AllowDiacriticSymbol { get; set; }

	public OdinAlphanumericValidationSettings WithAllowWhitespace(bool allowWhitespace = true)
	{
		AllowWhitespace = allowWhitespace;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowNumeric(bool allowNumeric = true)
	{
		AllowNumeric = allowNumeric;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowLeadingNumeric(bool allowLeadingNumeric = true)
	{
		AllowLeadingNumeric = allowLeadingNumeric;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowTrailingNumeric(bool allowTrailingNumbers = true)
	{
		AllowTrailingNumeric = allowTrailingNumbers;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowSymbols(bool allowSymbols = true)
	{
		AllowSymbols = allowSymbols;
		return this;
	}

	public OdinAlphanumericValidationSettings WithNonAllowedSymbols(List<string> nonAllowedSymbols)
	{
		NonAllowedSymbols = nonAllowedSymbols;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowedSymbols(List<string> allowedSymbols)
	{
		AllowedSymbols = allowedSymbols;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowLeadingSymbols(bool allowLeadingSymbols = true)
	{
		AllowLeadingSymbols = allowLeadingSymbols;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowTrailingSymbols(bool allowTrailingSymbols = true)
	{
		AllowTrailingSymbols = allowTrailingSymbols;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowLeadingWhitespace(bool allowLeadingWhitespace = true)
	{
		AllowLeadingWhitespace = allowLeadingWhitespace;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowTrailingWhitespace(bool allowTrailingWhitespace = true)
	{
		AllowTrailingWhitespace = allowTrailingWhitespace;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowMimeTypeExtensionLike(bool allowMimeTypeExtensionLike = true)
	{
		AllowMimeTypeExtensionLike = allowMimeTypeExtensionLike;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowUpperCasing(bool allowUpperCasing = true)
	{
		AllowUpperCasing = allowUpperCasing;
		return this;
	}

	public OdinAlphanumericValidationSettings WithMaxLength(int maxLength)
	{
		MaxLength = maxLength;
		return this;
	}

	public OdinAlphanumericValidationSettings WithMinLength(int minLength)
	{
		MinLength = minLength;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowEmpty(bool allowEmpty)
	{
		AllowEmpty = allowEmpty;
		return this;
	}

	public OdinAlphanumericValidationSettings WithAllowDiacriticSymbol(bool withDiacritic = true)
	{
		AllowDiacriticSymbol = withDiacritic;
		return this;
	}
}
