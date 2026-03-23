namespace Odin.Core.Countries;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public class IsoCountry
{
	protected string DebuggerDisplay => $"Iso2: {Iso2Code}, Iso3: {Iso3Code}, Name: {Name}, IsoCurrencySymbol: {IsoCurrencySymbol}, IsoCurrency: {CurrencySymbol}";

	/// <summary>
	/// Gets or sets the Iso2 country code e.g. 'MT'
	/// </summary>
	[Id(0)]
	public string Iso2Code { get; set; }

	/// <summary>
	/// Gets or sets the Iso3 country code e.g. 'MLT'
	/// </summary>
	[Id(1)]
	public string Iso3Code { get; set; }

	/// <summary>
	/// Gets or sets the country name.
	/// </summary>
	[Id(2)]
	public string Name { get; set; }

	public override string ToString() => Iso2Code;

	/// <summary>
	/// Gets or sets the iso currency symbol e.g. "USD"
	/// </summary>
	[Id(3)]
	public string IsoCurrencySymbol { get; set; }

	/// <summary>
	/// Gets or sets the currency symbol e.g. "$"
	/// </summary>
	[Id(4)]
	public string CurrencySymbol { get; set; }

	/// <summary>
	/// Gets or sets the calling code of the country e.g. "356"
	/// </summary>
	[Id(5)]
	public string? PhonePrefix { get; set; }

	/// <summary>
	/// Gets or sets hasState - true if country has states e.g. Canada
	/// </summary>
	[Id(6)]
	public bool HasState { get; set; }
}
