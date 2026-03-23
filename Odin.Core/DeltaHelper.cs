using System.Text.Json;

namespace Odin.Core;

public static class DeltaHelper
{
	public static void SetDeltaOptions(JsonSerializerOptions settings)
		=> DeltaExtensions.OptionsProvider = () => settings;
}
