using System.ComponentModel;
using System.Globalization;

namespace Odin.Core.Versioning;

public class SemanticVersionTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		=> sourceType == typeof(string);

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string stringValue && SemanticVersion.TryParse(stringValue, out var semVer))
			return semVer;
		return null;
	}
}
