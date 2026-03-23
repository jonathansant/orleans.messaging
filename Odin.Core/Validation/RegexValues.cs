using System.Text.RegularExpressions;

namespace Odin.Core.Validation;

public static class RegexValues
{
	public const string PhonePrefixNumber = @"^(\+)?([0-9]){1,3}$";
	public const string PhoneNumber = "^[0-9-() ]{5,15}$";

	public const string Email = @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])";

	public const string LettersOnly = @"^[\p{L}\p{M}*]+$";
	public const string FullName = @"^(?!'|-|`| )[\p{L}\p{M}* \-\'\`’]{1,}$";
	public const string Address = @"^(?!'|-|`| )[0-9\p{L}\p{M}* \-\'\`’,]{1,}$";
	public const string CurrencyCode = "^[a-zA-Z]{3}$";
	public const string CountryCode = "^[a-zA-Z]{2,3}$";
	public const string DgaCpr = "^[0-9]{6}-[0-9]{4}$";
	public const string Password = "^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^\\p{L}\\d])(?=\\S+$).{8,}$";
	public const string SensitiveDataJsonProperties = "\"(email|firstName|lastName|birthDate|passport|phoneNumber|addressLine)\":\"[^\"]*\"";
	public const string SensitiveDataFormattedJsonProperties = "\"(email|firstName|lastName|birthDate|passport|phoneNumber|addressLine)\": \".*\"";
	public const string WhiteSpace = @"\s";
	public const string Numeric = @"\d";
	public const string NumericLeading = @"\A\d";
	public const string NumericTrailingSingle = @"\d\z";
	public const string NumericTrailing = @"\d+$";
	public const string Symbols = @"[^A-Za-z\d\s]";
	public const string SymbolsLeading = @"\A[^A-Za-z\d\s]";
	public const string SymbolsLeadingWithDiacritics = @"\A[^A-Za-zÀ-ÿ\d\s]";
	public const string SymbolsTrailing = @"[^A-Za-z\d\s]\z";
	public const string WhitespaceLeading = @"\A\s.*";
	public const string WhitespaceTrailing = @".*\s\z";
	public const string UpperCase = @"\p{Lu}";
	public const string NumericOnly = "^[0-9]+$";
	public const string EglIdNumber = @"^[0-9]{11}$";
}

public static class Regexes
{
	public static readonly Regex WhiteSpace = new(RegexValues.WhiteSpace, RegexOptions.Compiled);
	public static readonly Regex Numeric = new(RegexValues.Numeric, RegexOptions.Compiled);
	public static readonly Regex NumericLeading = new(RegexValues.NumericLeading, RegexOptions.Compiled);
	public static readonly Regex NumericTrailingSingle = new(RegexValues.NumericTrailingSingle, RegexOptions.Compiled);
	public static readonly Regex NumericTrailing = new(RegexValues.NumericTrailing, RegexOptions.Compiled);
	public static readonly Regex Symbols = new(RegexValues.Symbols, RegexOptions.Compiled);
	public static readonly Regex SymbolsLeading = new(RegexValues.SymbolsLeading, RegexOptions.Compiled);
	public static readonly Regex SymbolsLeadingAllowDiacritic = new(RegexValues.SymbolsLeadingWithDiacritics, RegexOptions.Compiled);
	public static readonly Regex SymbolsTrailing = new(RegexValues.SymbolsTrailing, RegexOptions.Compiled);
	public static readonly Regex WhitespaceLeading = new(RegexValues.WhitespaceLeading, RegexOptions.Compiled);
	public static readonly Regex WhitespaceTrailing = new(RegexValues.WhitespaceTrailing, RegexOptions.Compiled);
	public static readonly Regex UpperCase = new(RegexValues.UpperCase, RegexOptions.Compiled);
	public static readonly Regex NumericOnly = new(RegexValues.NumericOnly, RegexOptions.Compiled);

	public static Regex StringWith(List<string> symbols)
	{
		var symbolsString = string.Join("", symbols);
		var escapedSymbols = Regex.Escape(symbolsString);
		var pattern = @"^[A-Za-zÀ-ÿ\d\s" + escapedSymbols + "]*$";
		return new(pattern, RegexOptions.Compiled);
	}
}
