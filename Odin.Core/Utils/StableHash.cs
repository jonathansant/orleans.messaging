using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Odin.Core.Utils;

// taken from : https://github.com/dotnet/orleans/blob/2d21b3f9afcdd491ded8246ec9ce2401ccbbd388/src/Orleans.Core.Abstractions/IDs/StableHash.cs
public static class StableHash
{
	/// <summary>
	/// Computes a hash digest of the input.
	/// </summary>
	/// <param name="data">
	/// The input data.
	/// </param>
	/// <returns>
	/// A hash digest of the input.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe uint ComputeHash(ReadOnlySpan<byte> data)
	{
		uint hash;
		XxHash32.TryHash(data, new Span<byte>((byte*)&hash, sizeof(uint)), out _);
		return BitConverter.IsLittleEndian ? hash : BinaryPrimitives.ReverseEndianness(hash);
	}

	/// <summary>
	/// Computes a hash digest of the input.
	/// </summary>
	/// <param name="data">
	/// The input data.
	/// </param>
	/// <returns>
	/// A hash digest of the input.
	/// </returns>
	public static uint ComputeHash(string data) => ComputeHash(
		BitConverter.IsLittleEndian ? MemoryMarshal.AsBytes(data.AsSpan()) : Encoding.Unicode.GetBytes(data)
	);
}
