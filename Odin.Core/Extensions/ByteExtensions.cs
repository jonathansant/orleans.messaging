
namespace Odin.Core;

public static class ByteExtensions
{
	/// <summary>
	/// Converts bytes to hex string.
	/// </summary>
	/// <param name="bytes">Bytes to convert.</param>
	public static string ToHexString(this byte[] bytes)
	{
		var hash = new StringBuilder();
		foreach (var b in bytes)
			hash.Append(b.ToString("x2"));

		return hash.ToString();
	}
}
