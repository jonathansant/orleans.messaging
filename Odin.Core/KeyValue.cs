namespace Odin.Core;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public class KeyValue<TKey, TValue>
{
	private string DebuggerDisplay => $"Key: '{Key}', Value: '{Value}'";

	[Id(0)]
	public TKey Key { get; set; }
	[Id(1)]
	public TValue Value { get; set; }
}

public static class KeyValueExtensions
{
	public static IEnumerable<KeyValue<TKey, TValue>> ToKeyValues<TKey, TValue>(this IDictionary<TKey, TValue> dic)
		=> dic.Select(x => new KeyValue<TKey, TValue>
		{
			Key = x.Key,
			Value = x.Value
		});
}
