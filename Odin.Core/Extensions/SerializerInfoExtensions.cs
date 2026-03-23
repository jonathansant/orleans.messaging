using System.Runtime.Serialization;

namespace Odin.Core;

public static class SerializationInfoExtensions
{
	public static bool TryGetValue<T>(this SerializationInfo serializationInfo, string name, out T value)
	{
		try
		{
			value = (T)serializationInfo.GetValue(name, typeof(T));
			return true;
		}
		catch (SerializationException)
		{
			value = default;
			return false;
		}
	}

	public static bool TryGetString(this SerializationInfo serializationInfo, string name, out string value)
	{
		try
		{
			value = serializationInfo.GetString(name);
			return true;
		}
		catch (Exception)
		{
			value = default;
			return false;
		}
	}

	public static T GetValueOrDefault<T>(this SerializationInfo serializationInfo, string name, Lazy<T> defaultValue)
	{
		try
		{
			return (T)serializationInfo.GetValue(name, typeof(T));
		}
		catch (SerializationException)
		{
			return defaultValue.Value;
		}
	}
}
