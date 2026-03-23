using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Odin.Core.DeviceDetection;

// this GenerateSerializer attribute is used by Orleans serialization in the request.Context
[GenerateSerializer]
[JsonConverter(typeof(StringEnumConverter), true)]
public enum DeviceType
{
	Desktop,
	Mobile,
	Tablet,
	Unknown,
}
