namespace Odin.Core;

[GenerateSerializer]
public class OdinKeyNotFoundException : Exception
{
	/// <summary>
	/// Gets or sets the key which was not found.
	/// </summary>
	[Id(0)]
	public string Key { get; set; }

	public OdinKeyNotFoundException() : base("Key not found!")
	{
	}

	public OdinKeyNotFoundException(string key) : base($"Key '{key}' not found!")
	{
		Key = key;
	}

	public OdinKeyNotFoundException(string key, string message) : base(message)
	{
		Key = key;
	}

	public OdinKeyNotFoundException(string message, Exception innerException) : base(message, innerException)
	{
	}

	public OdinKeyNotFoundException(string key, string message, Exception inner) : base(message, inner)
	{
		Key = key;
	}
}
