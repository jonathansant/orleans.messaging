namespace Odin.CodeAnnotations;

public enum DataType
{
	Undefined = 0,
	Id,
	CompositeId,
	Naming,
	PersonaNaming,
	Key,
	ExternalKey,
	Guid,
	Url,
	Domain,
	Email,
	PasswordHash,
	Phone,
	PhonePrefix,
	ClaimValue,
	Locale,
	CurrencyCode,
	CountryCode,
	Slug,
	CorrelationId,
	Reason,
	Comment,
	ContentShort,
	ContentLong,
	Json,
	Duration,
	FilePath,
	FileName,
	City,
	Postcode,
	AddressLine,
	Description,
	FieldName,

	// numeric
	MoneySmall,
	Money,
	Crypto,
	SortOrder,

	DateOnly,
	TimeOnly,
	Enum
}

[AttributeUsage(AttributeTargets.Property)]
public class DataTypeAttribute(
	DataType dataType
) : Attribute
{
	public DataType DataType { get; set; } = dataType;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class DataTypeIgnoreAttribute : Attribute;
