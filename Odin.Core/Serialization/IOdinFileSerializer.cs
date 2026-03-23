namespace Odin.Core.Serialization;

public interface IOdinFileSerializerOptions;

public interface IOdinFileSerializer
{
	Task<string> Serialize<TModel>(
		List<TModel> rows,
		List<IOdinFileSerializerField<TModel>> fields,
		IOdinFileSerializerOptions? options = null
	);

	string GetFileFormat();
}
