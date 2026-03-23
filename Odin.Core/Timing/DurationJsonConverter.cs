using Newtonsoft.Json;

namespace Odin.Core.Timing;

public class DurationJsonConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		var transformedValue = ((TimeSpan)value).ToDurationString();
		writer.WriteValue(transformedValue);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		=> ((string)reader.Value).ToTimeSpanFromDuration();

	public override bool CanConvert(Type objectType)
		=> objectType == typeof(TimeSpan);
}
