namespace Odin.Core.DeviceDetection;

public static class DeviceTypeExtensions
{
	public static IEnumerable<IDeviceFiltering> FilterByDevice(this IEnumerable<IDeviceFiltering> list, DeviceType deviceType)
		=> list.Where(item => item.IsAvailableForDevice(deviceType));

	public static bool IsAvailableForDevice(this IDeviceFiltering obj, DeviceType deviceType)
		=> obj.DeviceAvailability.IsNullOrEmpty() || obj.DeviceAvailability.Contains(deviceType);
}
