namespace Odin.Core.Extensibility;

public interface IExtendibleProps
{
	void WithItem(string key, object? value);

	object? GetItem(string key);

	T? GetItem<T>(string key)
	{
		var item = GetItem(key);
		return item == null ? default : (T)item;
	}
}
