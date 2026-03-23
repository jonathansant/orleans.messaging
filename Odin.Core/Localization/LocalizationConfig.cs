namespace Odin.Core.Localization;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class LocalizationConfig
{
	protected string DebuggerDisplay => $"Default: '{Default}', LocaleInfos: '{LocaleInfos.ToDebugString()}'";

	public List<LocaleInfo> LocaleInfos { get; set; }
	public string Default { get; set; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class LocaleInfo
{
	protected string DebuggerDisplay => $"Locale: '{Locale}', Default: '{Default}'";

	public string Locale { get; set; }
	public string Default { get; set; }
}
