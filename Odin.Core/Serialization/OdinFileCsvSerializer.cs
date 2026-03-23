using nietras.SeparatedValues;

namespace Odin.Core.Serialization;

public record OdinFileCsvSerializerOptions : IOdinFileSerializerOptions
{
	public bool IncludeHeader { get; init; } = true;
	public char Delimiter { get; init; } = ',';
}

public class OdinFileCsvSerializer : IOdinFileSerializer
{
	private readonly OdinFileCsvSerializerOptions _defaults = new()
	{
		IncludeHeader = false
	};

	public Task<string> Serialize<TModel>(
		List<TModel> rows,
		List<IOdinFileSerializerField<TModel>> fields,
		IOdinFileSerializerOptions? options = null
	)
	{
		if (rows.IsNullOrEmpty())
			return Task.FromResult(string.Empty);

		var opts = options as OdinFileCsvSerializerOptions ?? _defaults;

		var sepOptions = new SepWriterOptions
		{
			WriteHeader = opts.IncludeHeader,
			Sep = new(opts.Delimiter),
			Escape = true
		};

		using var writer = Sep.Writer(o => sepOptions).ToText();

		foreach (var item in rows)
		{
			using var row = writer.NewRow();
			foreach (var field in fields)
			{
				var rawPropValue = field.PropSelector.Func.Invoke(item);
				var rawPropName = field.PropSelector.Name;
				var transformedValue = field.ModifyPropValue(rawPropValue);
				var columnName = field.RenameProp(rawPropName);

				row[columnName].Set(transformedValue.ToString());
			}
		}

		return Task.FromResult(writer.ToString());
	}

	public string GetFileFormat()
		=> ".csv";
}
