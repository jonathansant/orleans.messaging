using Odin.CodeAnnotations;

namespace Odin.Core.Primitives;

[GenerateSerializer]
public struct NumericInfoSurrogate
{
	[Id(0)]
	public object Min;

	[Id(1)]
	public object Max;

	[Id(2)]
	public int? Precision;

	[Id(3)]
	public int? Scale;

	[Id(4)]
	public bool AllowZero;
}

[RegisterConverter]
public sealed class NumericInfoSurrogateConverter : IConverter<NumericInfo, NumericInfoSurrogate>
{
	public NumericInfoSurrogate ConvertToSurrogate(in NumericInfo value)
		=> new()
		{
			Min = value.Min,
			Max = value.Max,
			Precision = value.Precision,
			Scale = value.Scale,
			AllowZero = value.AllowZero,
		};

	public NumericInfo ConvertFromSurrogate(in NumericInfoSurrogate value)
		=> new()
		{
			Min = value.Min,
			Max = value.Max,
			Precision = value.Precision,
			Scale = value.Scale,
			AllowZero = value.AllowZero,
		};
}
