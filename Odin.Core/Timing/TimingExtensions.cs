using System.Xml;

namespace Odin.Core.Timing;

public static class TimingExtensions
{
	public static TimeSpan? ToTimeSpanFromDuration(this string duration)
	{
		if (duration.IsNullOrEmpty())
			return null;

		try
		{
			return XmlConvert.ToTimeSpan(duration);
		}
		catch (Exception) // fail silently if XmlConvert fails to convert, for instance having a week duration
		{
			return null;
		}
	}

	public static string ToDurationString(this TimeSpan timeSpan)
		=> XmlConvert.ToString(timeSpan);

	public static string ToDurationString(this TimeSpan? timeSpan)
		=> timeSpan?.ToDurationString();
}
