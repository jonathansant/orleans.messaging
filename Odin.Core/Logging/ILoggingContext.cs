namespace Odin.Core.Logging;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public struct LogContextData : IEquatable<LogContextData>
{
	private string DebuggerDisplay => $"PropertyName: '{PropertyName}', Value: '{Value}', DestructureObject: '{DestructureObject}'";

	[Id(0)]
	public string PropertyName { get; set; }
	[Id(1)]
	public object Value { get; set; }
	[Id(2)]
	public bool DestructureObject { get; set; }

	public bool Equals(LogContextData other)
		=> PropertyName == other.PropertyName && Equals(Value, other.Value) && DestructureObject == other.DestructureObject;

	public override bool Equals(object obj) => obj is LogContextData other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = (PropertyName != null ? PropertyName.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ DestructureObject.GetHashCode();
			return hashCode;
		}
	}

	public static bool operator ==(LogContextData a, LogContextData b) => a.Equals(b);

	public static bool operator !=(LogContextData a, LogContextData b) => !(a == b);
}

public interface ILoggingContext : IDisposable
{
	IEnumerable<LogContextData> GetAll();

	void Set(LogContextData data);

	/// <summary>
	/// Bind log context data as scoped.
	/// </summary>
	IDisposable BindScope();

	void Rebind();
}

public static class LoggingContextExtension
{
	public static void Set(this ILoggingContext ctx, string propName, object value, bool destructureObject = false)
		=> ctx.Set(new LogContextData { PropertyName = propName, Value = value, DestructureObject = destructureObject });
}
