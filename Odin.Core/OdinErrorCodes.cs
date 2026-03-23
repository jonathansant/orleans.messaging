using Odin.Core.Error;
using System.Text.RegularExpressions;

namespace Odin.Core;

[ErrorExplorer(Context = "odin.general")]
public static class OdinErrorCodes
{
	public const string ValidationFailed = "error.failed:validation";
	public const string InvalidRequest = "error.invalid:request";
	public const string InvalidBrand = "error.invalid:brand";
	public const string CountryCodeRequired = "error.required:country-code";
	public const string CountryCodeInvalid = "error.invalid:country-code";
	public const string CurrencyCodeInvalid = "error.invalid:currency-code";
	public const string GqlInvalidValue = "error.invalid:gql.value";
	public const string GqlInvalidQuery = "error.invalid:gql.query";
	public const string InternalServerError = "error.internal-server-error";
	public const string Maintenance = "error.maintenance";
	public const string InvalidRemoteIp = "error.invalid:remote-ip";
	public const string ModelBinding = "error.model-binding";
	public const string LocaleUnsupported = "error.unsupported:locale";
	public const string Unhealthy = "error.unhealthy";
	public const string UserIdRequired = "error.required:user.id";
	public const string SamePassword = "error.matches:current-password";

	// generic
	public const string NotFound = "error.not-found";
	public const string Required = "error.required";
	public const string IndexerOrFilterRequired = "error.required:indexer-or-filter";
	public const string AlreadyExists = "error.exists";
	public const string AlreadyExistsArchived = "error.exists:archived";
	public const string Archived = "error.archived";
	public const string AlreadyVerified = "error.already-verified";
	public const string Duplicate = "error.duplicate";
	public const string Invalid = "error.invalid";
	public const string InvalidFormat = "error.invalid:format";
	public const string Unsupported = "error.invalid:unsupported";
	public const string UnrelatedEntity = "error.invalid:unrelated-entity";
	public const string Ineligible = "error.ineligible";
	public const string Forbidden = "error.forbidden";
	public const string LinksExist = "error.linked";
	public const string ChildLinksExist = "error.child-linked";
	public const string Mismatch = "error.mismatch";
	public const string Cooldown = "error.cooldown";
	public const string ArgumentNull = "error.arg-null";
	public const string MissingConfig = "error.missing-config";
	public const string Immutable = "error.immutable";
	public const string InvalidAmount = "error.invalid-amount";
	public const string InvalidOperatorAmount = "error.invalid-operator-amount";
	public const string Insufficient = "error.insufficient";
	public const string InactiveSession = "error.inactive:session";
	public const string InactiveAccount = "error.inactive:account";
	public const string Expired = "error.expired";

	[ErrorExplorer(Context = "odin.validation")]
	public static class Validation
	{
		public const string MinLength = "error.validation.min-length";
		public const string MaxLength = "error.validation.max-length";
		public const string InvalidUrl = "error.validation.url";
		public const string InvalidDuration = "error.validation.duration";
		public const string InvalidPhonePrefix = "error.validation.phone-prefix";
		public const string InvalidEmail = "error.validation.email";
		public const string SizeLimitExceed = "error.validation.size-limit-exceeded";
		public const string SizeLimitTooSmall = "error.validation.size-limit-small";
		public const string InvalidFileType = "error.validation.file-type";
		public const string ExpiredFile = "error.validation.file-expiration";
		public const string WhitespaceNotAllowed = "error.validation.whitespaces";
		public const string ZeroNotAllowed = "error.validation.zero";
		public const string NumericNotAllowed = "error.validation.alpha-chars-only";
		public const string LeadingNumericNotAllowed = "error.validation.digit.leading";
		public const string TrailingNumericNotAllowed = "error.validation.digit.trailing";
		public const string SymbolsNotAllowed = "error.validation.symbols";
		public const string LeadingSymbolsNotAllowed = "error.validation.symbols.leading";
		public const string TrailingSymbolsNotAllowed = "error.validation.symbols.trailing";
		public const string LeadingWhitespaceNotAllowed = "error.validation.whitespaces.leading";
		public const string TrailingWhitespaceNotAllowed = "error.validation.whitespaces.trailing";
		public const string UpperCaseCharactersNotAllowed = "error.validation.uppercase";
		public const string MimeTypeExtensionLikeFormat = "error.validation.format.mime-type";
		public const string DateOfBirthUnderAge = "error.validation.date.underage";
		public const string GreaterThanUpperBound = "error.validation.greater-than-upperbound";
		public const string LessThanLowerBound = "error.validation.less-than-lowerbound";
		public const string NumericRange = "error.validation.digit.range";
		public const string NumericPrecisionExceeded = "error.validation.digit.precision";

		public const string InvalidCompositeId = "error.invalid:composite-id";
		public const string InvalidIdPattern = "error.invalid:id-pattern";
		public const string InvalidRefId = "error.invalid:ref-id";
		public const string InvalidFilter = "error.invalid:filter";

	}

	[ErrorExplorer(Context = "odin.auth")]
	public static class Auth
	{
		public const string ClaimRequired = "error.claim:required";
		public const string ClaimRequiredValue = "error.claim:required-value";

		public const string TokenInvalid = "error.invalid:auth.token";
		public const string TokenRequired = "error.required:auth.token";
		public const string TokenExpired = "error.expired:auth.token";
		public const string Unauthorized = "error.unauthorized:auth";
		public const string PermissionDenied = "error.permission-denied:auth";
		public const string AccessDenied = "error.access-denied:auth";
		public const string BrandInvalid = "error.invalid-brand:auth";
		public const string DeviceInvalid = "error.invalid-device:auth";
		public const string UserInvalid = "error.invalid-user:auth";
	}

	[ErrorExplorer(Context = "odin.paging")]
	public static class Paging
	{
		public const string Invalid = "error.invalid:paging.size";
		public const string InvalidPageParams = "error.invalid:paging";
	}
}

public static class OdinErrorMessages
{
	private const string RequiredFields = "fields are required";
	public const string SomeRequired = $"some {RequiredFields}";
	public const string AllRequired = $"all {RequiredFields}";
	public const string Reason = "reason";
	public const string ConflictingFields = "only one field from [{Fields}] is allowable.";

	public const string RequiredFieldTemplate = "'{PropertyName}' is required.";
	public const string NotSupportedFieldTemplate = "'{PropertyName}' is not supported.";
	public const string RequiredFieldGroupsTemplate = "'{PropertyName}' has required {RequiredFieldGroups}.";
	public const string ImmutableTemplate = "'{PropertyName}' is immutable.";
	public const string InvalidTemplate = "'{PropertyName}' value '{Value}' is not valid.";
	public const string InvalidItemTemplate = "'{PropertyName}' value '{Value}' is not in the available items.";
	public const string MismatchTemplate = "'{PropertyName}' value '{Value}' does not match with compared value '{ComparedValue}'.";

	public const string EntityNotFound = "Entity not found.";
	public const string InvalidData = "Invalid data.";
	public const string EntityAlreadyExists = "Entity already exists.";
	public const string Archived = "Entity is archived.";
	public const string ForbiddenAccess = "Forbidden access.";
	public const string UnrelatedEntity = "Entity is unrelated.";
	public const string Unsupported = "Unsupported.";

	public const string FileContentMismatch = "File content does not match '{FileExtension}' file extension.";
	public const string MissingHeaders = "Missing headers.";
	public const string CsvParseFailed = "Failed to parse CSV file.";
	public const string UnknownHeaders = "Unknown headers.";
	public const string DuplicateHeaders = "Duplicate headers.";
	public const string DuplicateDataTemplate = "Duplicate data {PropertyName}.";
	public const string InvalidHeaderTemplate = "Invalid '{HeaderName}' type.";
	public const string InconsistentNumberOfColumnsTemplate =
		"Inconsistent number of columns, expected {ExpectedColumnCount} found {FoundColumnCount}.";

	public const string UnknownError = "An unknown error occurred.";

	public const string IndexMustBeTemplate = "Index must be '{AllowedIndex}'.";
	public static string IndexMustBe(string allowedIndex) => IndexMustBeTemplate.FromTemplate(
		new Dictionary<string, object>
		{
			["AllowedIndex"] = allowedIndex
		}
	);

	public const string IndexMustBeOneOfTemplate = "Index must be one of {AllowedIndexes}.";
	public static string IndexMustBeOneOf(IEnumerable<string> allowedIndexes) => IndexMustBeOneOfTemplate.FromTemplate(
		new Dictionary<string, object>
		{
			["AllowedIndexes"] = allowedIndexes.ToDebugString()
		}
	);
}

public static class OdinErroneousParts
{
	public const string Query = "query";
	public const string RequestType = "requestType";
	public const string Filter = "filter";
}
