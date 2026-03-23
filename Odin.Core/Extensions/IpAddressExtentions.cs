// ReSharper disable once CheckNamespace
namespace System.Net;

public static class IpAddressExtensions
{
	public static bool IsInternal(this IPAddress ipAddress)
	{
		if (ipAddress.ToString() == "::1")
			return true;

		var ip = ipAddress.GetAddressBytes();
		switch (ip[0])
		{
			case 10:
			case 127:
				return true;
			case 172:
				return ip[1] >= 16 && ip[1] < 32;
			case 192:
				return ip[1] == 168;
			default:
				return false;
		}
	}
}
