namespace Odin.Core.DeviceDetection;

public interface IDeviceFiltering
{
	HashSet<DeviceType> DeviceAvailability { get; set; }
}
