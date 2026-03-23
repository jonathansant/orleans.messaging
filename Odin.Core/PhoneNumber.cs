using PhoneNumbers;
using System.Runtime.Serialization;

namespace Odin.Core;

/// <summary>
/// Phone number which validates and parses phone numbers.
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[Serializable]
public record struct PhoneNumber : ISerializable
{
	private string DebuggerDisplay => $"Prefix: '{Prefix}', Number: '{Number}'";

	private PhoneNumberUtil _phoneUtil;
	private PhoneNumberUtil PhoneUtil => _phoneUtil ??= PhoneNumberUtil.GetInstance();
	private readonly PhoneNumbers.PhoneNumber _phone;
	private const string PhoneSerializeKey = "phone";
	private const string RegionCodeSerializeKey = "regionCode";

	/// <summary>
	/// Get an empty instance of phone number
	/// </summary>
	public static readonly PhoneNumber Empty = new();

	/// <summary>
	/// Gets the countrycode e.g '356'.
	/// </summary>
	public int CountryCode => _phone?.CountryCode ?? 0;

	public string RegionCode { get; }

	/// <summary>
	/// Gets the prefix e.g. '+356'.
	/// </summary>
	public string Prefix { get; }

	/// <summary>
	/// Gets the phone number e.g. '99665544'.
	/// </summary>
	public string Number { get; }

	/// <summary>
	/// Initializes a new PhoneNumber instance.
	/// </summary>
	/// <param name="phone">Full phone number to use e.g. '+356 99665544'</param>
	public PhoneNumber(string phone) : this(phone, null)
	{
	}

	/// <summary>
	/// Initializes a new PhoneNumber instance.
	/// </summary>
	/// <param name="phone">Full phone number to use e.g. '+356 99665544'</param>
	/// <param name="regionCode">Optional region code to use e.g. 'US'</param>
	public PhoneNumber(string phone, string regionCode)
	{
		_phone = null;
		Prefix = null;
		Number = null;
		RegionCode = regionCode;

		if (phone.IsNullOrEmpty())
			return;

		try
		{
			_phone = PhoneUtil.Parse(phone, regionCode);
			Prefix = $"+{_phone.CountryCode}";
			Number = _phone.NationalNumber.ToString();
		}
		catch
		{
			// ignored
		}
	}

	// this constructor is used for deserialization
	private PhoneNumber(SerializationInfo info, StreamingContext text)
		: this(info.GetString(PhoneSerializeKey), info.GetString(RegionCodeSerializeKey))
	{
	}

	/// <summary>
	/// Determine whether the phone is valid.
	/// </summary>
	public bool IsValid()
	{
		if (PhoneUtil == null || _phone == null)
			return false;

		return PhoneUtil.IsValidNumber(_phone);
	}

	/// <summary>
	/// Determines whether the phone is valid for the specific region.
	/// </summary>
	/// <param name="regionCode">Region to check e.g. 'MT'.</param>
	public bool IsValidForRegion(string regionCode)
	{
		if (PhoneUtil == null || _phone == null)
			return false;
		return PhoneUtil.IsValidNumberForRegion(_phone, regionCode);
	}

	/// <summary>
	/// Determines whether the phone is empty.
	/// </summary>
	/// <returns></returns>
	public bool IsEmpty() => Prefix.IsNullOrEmpty() || Number.IsNullOrEmpty();

	public bool Equals(PhoneNumber y) => CountryCode == y.CountryCode && Number == y.Number;

	public override string ToString() => $"{Prefix}{Number}";

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue(PhoneSerializeKey, ToString());
		info.AddValue(RegionCodeSerializeKey, RegionCode);
	}

	public override int GetHashCode() => CountryCode + Number.GetHashCode();

	/// <summary>
	/// Create a new instance from prefix, phone number and region code.
	/// </summary>
	/// <param name="countryCode">Countrycode to use e.g. '+356'</param>
	/// <param name="phoneNumber">Phone number to use e.g. '99665544'</param>
	public static PhoneNumber From(string countryCode, string phoneNumber)
		=> From(countryCode, phoneNumber, null);

	/// <summary>
	/// Create a new instance from prefix, phone number and region code.
	/// </summary>
	/// <param name="countryCode">Countrycode to use e.g. '+356'</param>
	/// <param name="phoneNumber">Phone number to use e.g. '99665544'</param>
	/// <param name="regionCode">Optional region code to use e.g. 'US'.</param>
	public static PhoneNumber From(string countryCode, string phoneNumber, string regionCode)
		=> new($"{countryCode}{phoneNumber}", regionCode);

	/// <summary>
	/// Try parses phone number.
	/// </summary>
	/// <param name="phone"></param>
	/// <param name="result"></param>
	public static bool TryParse(string phone, out PhoneNumber result)
		=> TryParse(phone, null, out result);

	/// <summary>
	/// Try parses phone number.
	/// </summary>
	/// <param name="phone"></param>
	/// <param name="regionCode"></param>
	/// <param name="result"></param>
	public static bool TryParse(string phone, string regionCode, out PhoneNumber result)
	{
		if (!IsViable(phone))
		{
			result = default;
			return false;
		}

		try
		{
			result = new(phone, regionCode);

			if (result.IsEmpty())
				return false;
		}
		catch (Exception)
		{
			result = default;
			return false;
		}
		return true;
	}

	/// <summary>
	/// Determines whether the values are viable for phone number.
	/// <param name="number">Phone number to test e.g. '+35699665544'</param>
	/// </summary>
	private static bool IsViable(string number)
		=> PhoneNumberUtil.IsViablePhoneNumber(number);
}

[GenerateSerializer]
public struct PhoneNumberSurrogate
{
	[Id(0)]
	public string Prefix;

	[Id(1)]
	public string Number;

	[Id(2)]
	public string RegionCode;
}

[RegisterConverter]
public sealed class PhoneNumberSurrogateConverter : IConverter<PhoneNumber, PhoneNumberSurrogate>
{
	public PhoneNumberSurrogate ConvertToSurrogate(in PhoneNumber value)
		=> new()
		{
			Prefix = value.Prefix,
			Number = value.Number,
			RegionCode = value.RegionCode,
		};

	public PhoneNumber ConvertFromSurrogate(in PhoneNumberSurrogate surrogate)
		=> PhoneNumber.From(countryCode: surrogate.Prefix, phoneNumber: surrogate.Number, regionCode: surrogate.RegionCode);
}
