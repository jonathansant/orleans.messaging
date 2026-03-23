namespace Odin.Core.Validation;

public static class OdinPropFields
{
	public const int GuidLength = 36;
	public const int IdLength = 36;
	public const int CompositeIdLength = 125;

	/// <summary>
	/// Used for naming stuff e.g. company name, person name, brand name, dog name etc...
	/// </summary>
	public const int NamingLength = 65;
	public const int FieldNameLength = 65;
	public const int PersonaNamingLength = 50;

	/// <summary>
	/// Used for key like names e.g. key, slug etc...
	/// </summary>
	public const int KeyLength = 65;
	public const int ExternalKeyLength = KeyLength;
	public const int SlugLength = KeyLength;

	public const int UrlLength = 2084;
	public const int DomainLength = 255;
	public const int EmailLength = 320;
	public const int PasswordHashLength = 1500;
	public const int PhoneLength = 15;
	public const int PhonePrefixLength = 4;
	public const int ClaimValueLength = 1500;
	public const int CurrencyCodeLength = 36;
	public const int CountryCodeLength = 2;
	public const int LocaleCodeLength = 11;
	public const int JsonLength = 6000;
	public const int CorrelationIdLength = 100;
	public const int ReasonLength = 255;
	public const int CommentLength = 500;
	public const int DescriptionLength = 255;
	public const int ContentShortLength = 500;
	public const int ContentLongLength = 3000;
	public const int FilePathLength = 250;
	public const int FileNameLength = 100;
	public const int AddressLineLength = 100;
	public const int CityLength = 100;
	public const int PostcodeLength = 10;
}
