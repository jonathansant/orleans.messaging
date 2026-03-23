using System.Buffers;

namespace Odin.Core;

public static class SpanExtensions
{
	// todo: use ext from .net8 when migrating
	public static (int count, (int start, int end)[] ranges) Split(this ReadOnlySpan<char> line, char delimiter)
	{
		var ranges = ArrayPool<(int start, int end)>.Shared.Rent(line.Length);

		var count = 0;
		var startCapture = -1;

		for (var i = 0; i < line.Length; i++)
		{
			var c = line[i];

			if (c == delimiter)
			{
				if (startCapture == -1)
					continue;

				ranges[count++] = (startCapture, i);
				startCapture = -1;
			}
			else if (startCapture == -1)
				startCapture = i;
		}

		if (startCapture != -1)
			ranges[count++] = (startCapture, line.Length);

		ArrayPool<(int, int)>.Shared.Return(ranges);
		return (count, ranges);
	}
}
