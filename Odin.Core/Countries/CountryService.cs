using Microsoft.Extensions.Configuration;
using Nager.Country;
using Odin.Core.Error;
using System.Globalization;

namespace Odin.Core.Countries;

public interface ICountryService
{
	List<IsoCountry> GetAll();

	/// <summary>
	/// Gets the IsoCountry by its iso code (code can be Iso2 or 3).
	/// </summary>
	/// <param name="isoCode">The iso code e.g. 'MT' or 'MLT'.</param>
	IsoCountry GetByCode(string isoCode);

	IsoCountry? GetByCodeIso2OrDefault(string iso2Code);
	IsoCountry? GetByCodeIso3OrDefault(string iso3Code);
	bool FilterByCountry(ICountryFilterable item, string countryCode);
	bool FilterByCountryCode(ICountryFilterable item, string countryCode);
	string GetCurrencySymbolByCurrencyCode(string currencyCode);
}

public interface ICountryFilterable
{
	HashSet<string> CountryAvailability { get; set; }
	HashSet<string> CountryExclusion { get; set; }
}

public class CountryService : ICountryService
{
	private readonly IDictionary<string, IsoCountry> _iso2Countries;
	private readonly IDictionary<string, IsoCountry> _iso3Countries;
	private readonly IDictionary<string, string?> _iso3Currencies;
	private const string LocaleEnPrefix = "en-"; // note: certain regions require a specific culture name
	private static readonly CountryProvider CountryProvider = new();

	internal CountryService(Dictionary<string, HashSet<string>>? countryStates = null)
	{
		_iso2Countries = new Dictionary<string, IsoCountry>();
		_iso3Countries = new Dictionary<string, IsoCountry>();
		_iso3Currencies = new Dictionary<string, string?>();

		Initialize(countryStates ?? []);
	}

	public CountryService(IConfiguration config)
		: this(config.GetSection("countryStates").Get<Dictionary<string, HashSet<string>>>())
	{
	}

	private void Initialize(Dictionary<string, HashSet<string>> countryStates)
	{
		foreach (var isoCountry in CountryProvider.GetCountries())
		{
			var regionInfo = new RegionInfo(LocaleEnPrefix + isoCountry.Alpha2Code);

			var country = new IsoCountry
			{
				Iso2Code = isoCountry.Alpha2Code.ToString(),
				Iso3Code = isoCountry.Alpha3Code.ToString(),
				Name = isoCountry.CommonName,
				IsoCurrencySymbol = regionInfo.ISOCurrencySymbol,
				CurrencySymbol = regionInfo.CurrencySymbol,
				PhonePrefix = isoCountry.CallingCodes.FirstOrDefault(),
			};

			country.HasState = countryStates.ContainsKey(country.Iso2Code);

			_iso2Countries.Add(country.Iso2Code, country);
			_iso3Countries.Add(country.Iso3Code, country);

			foreach (var isoCountryCurrency in isoCountry.Currencies)
				_iso3Currencies.TryAdd(isoCountryCurrency.IsoCode, isoCountryCurrency.Symbol);
		}
	}

	public IsoCountry GetByCode(string isoCode)
	{
		var country = GetByCodeIso2OrDefault(isoCode)
					  ?? GetByCodeIso3OrDefault(isoCode)
			;

		if (country != null)
			return country;

		throw ApiErrorException.AsValidation(OdinErrorCodes.CountryCodeInvalid);
	}

	public IsoCountry? GetByCodeIso2OrDefault(string iso2Code)
		=> _iso2Countries.TryGetValue(iso2Code, out var code) ? code : null;

	public IsoCountry? GetByCodeIso3OrDefault(string iso3Code)
		=> _iso3Countries.TryGetValue(iso3Code, out var code) ? code : null;

	public List<IsoCountry> GetAll() => _iso2Countries.Values.ToList();

	public bool FilterByCountry(ICountryFilterable? item, string countryCode)
	{
		if (item == null)
			return false;

		if (countryCode == null)
			throw ErrorResult.AsValidationError()
				.AddField(nameof(countryCode), x => x.AsNotFound())
				.AsApiErrorException();

		if (item.CountryExclusion.IsNullOrEmpty() && item.CountryAvailability.IsNullOrEmpty())
			return true;

		var countryName = GetByCode(countryCode).Name;
		return countryName.IsEligible(item.CountryAvailability, item.CountryExclusion);
	}

	public bool FilterByCountryCode(ICountryFilterable? item, string countryCode)
	{
		if (item == null)
			return false;

		if (countryCode == null)
			throw ErrorResult.AsValidationError()
				.AddField(nameof(countryCode), x => x.AsNotFound())
				.AsApiErrorException();

		if (item.CountryExclusion.IsNullOrEmpty() && item.CountryAvailability.IsNullOrEmpty())
			return true;

		var code = GetByCode(countryCode).Iso2Code;
		return code.IsEligible(item.CountryAvailability, item.CountryExclusion);
	}

	public string? GetCurrencySymbolByCurrencyCode(string currencyCode)
	{
		ArgumentNullException.ThrowIfNull(currencyCode);

		if (_iso3Currencies.TryGetValue(currencyCode, out var currencySymbol))
			return currencySymbol;
		throw ErrorResult.AsValidationError()
			.AddField(currencyCode, x => x.AsNotFound())
			.AsApiErrorException();
	}
}
