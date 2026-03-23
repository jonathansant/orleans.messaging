namespace Odin.Core;

public static class MathExtensions
{
	/// <summary>
	/// Clamps (limits) the value between min and max values.
	/// </summary>
	/// <param name="value">Value to clamp.</param>
	/// <param name="min">Min value specified.</param>
	/// <param name="max">Max value specified.</param>
	/// <returns>Returns either the value as is if its between min and max or else, use the min or max.</returns>
	public static int Clamp(this int value, int min, int max)
		=> value < min
			? min
			: value > max
				? max : value;

	/// <summary>
	/// Divides and returns the ceiling.
	/// </summary>
	public static int DivideAndCeil(this int value, int divisor)
		=> (int)Math.Ceiling((float)value / divisor);
}
