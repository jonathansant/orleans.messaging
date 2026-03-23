using System.IO.Hashing;
using System.Text;

namespace Odin.Messaging.Memory;

public class SimpleRingHash
{
	public static uint Calculate(uint nodes, string key)
	{
		var intHash = BitConverter.ToUInt32(XxHash64.Hash(Encoding.UTF8.GetBytes(key)));
		return intHash % nodes;
	}
}
