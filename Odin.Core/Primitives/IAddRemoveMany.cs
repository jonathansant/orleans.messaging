using System.Diagnostics.CodeAnalysis;

namespace Odin.Core.Primitives;

public interface IChanges<TAdded, TUpdated, TRemoved> : IAddRemoveMany<TAdded, TRemoved>
{
	HashSet<TUpdated> Updated { get; set; }
}

public interface ICrudChangesInput<TAdded, TUpdated, TRemoved> : IChanges<TAdded, TUpdated, TRemoved>;

public interface ICrudChangesInput<TAddedOrUpdated, TRemoved> : ICrudChangesInput<TAddedOrUpdated, TAddedOrUpdated, TRemoved>;

public interface IAddRemoveMany<TAdded, TRemoved>
{
	HashSet<TAdded> Added { get; set; }
	HashSet<TRemoved> Removed { get; set; }
}

public interface IAddRemoveMany<T> : IAddRemoveMany<T, T>;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record CrudChangesInput<TAdded, TUpdated, TRemoved> : ICrudChangesInput<TAdded, TUpdated, TRemoved>
{
	private string DebuggerDisplay
		=> $"Added: {Added.ToDebugString()}, Updated: {Updated.ToDebugString()}, Removed: {Removed.ToDebugString()}";

	[Id(0)]
	public HashSet<TAdded> Added { get; set; } = [];
	[Id(1)]
	public HashSet<TUpdated> Updated { get; set; } = [];
	[Id(2)]
	public HashSet<TRemoved> Removed { get; set; } = [];

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}


[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record CrudChangesInput<TAdded, TUpdated> : ICrudChangesInput<TAdded, TUpdated, string>
{
	private string DebuggerDisplay
		=> $"Added: {Added.ToDebugString()}, Updated: {Updated.ToDebugString()}, Removed: {Removed.ToDebugString()}";

	[Id(0)]
	public HashSet<TAdded> Added { get; set; } = [];
	[Id(1)]
	public HashSet<TUpdated> Updated { get; set; } = [];
	[Id(2)]
	public HashSet<string> Removed { get; set; } = [];

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record CrudChangesInput<TAddedOrUpdated> : CrudChangesInput<TAddedOrUpdated, TAddedOrUpdated>
{
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record AddRemoveMany<TAdded> : IAddRemoveMany<TAdded, string>
{
	private string DebuggerDisplay
		=> $"Added: {Added.ToDebugString()}, Removed: {Removed.ToDebugString()}";

	[Id(0)]
	public HashSet<TAdded> Added { get; set; } = [];
	[Id(1)]
	public HashSet<string> Removed { get; set; } = [];

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record AddRemoveMany<TAdded, TRemoved> : IAddRemoveMany<TAdded, TRemoved>
{
	private string DebuggerDisplay
		=> $"Added: {Added.ToDebugString()}, Removed: {Removed.ToDebugString()}";

	[Id(0)]
	public HashSet<TAdded> Added { get; set; } = [];
	[Id(1)]
	public HashSet<TRemoved> Removed { get; set; } = [];

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}

public static class AddRemoveManyExtensions
{
	public static bool HasAddedOrRemoved<TAdded, TRemoved>([NotNullWhen(true)] this IAddRemoveMany<TAdded, TRemoved>? changes)
		=> changes != null && (!changes.Added.IsNullOrEmpty() || !changes.Removed.IsNullOrEmpty());

	public static bool IsNullOrEmpty<TAdded, TUpdated>(this IChanges<TAdded, TUpdated, string>? changes)
		=> changes == null || (changes.Added.IsNullOrEmpty() && changes.Updated.IsNullOrEmpty() && changes.Removed.IsNullOrEmpty());

	public static List<T> GetAddedRemovedCombined<T>(this IAddRemoveMany<T, T> changes)
	{
		if (!changes.HasAddedOrRemoved())
			return null;

		var combined = new List<T>();
		if (!changes.Added.IsNullOrEmpty())
			combined.AddRange(changes.Added);

		if (!changes.Removed.IsNullOrEmpty())
			combined.AddRange(changes.Removed);
		return combined;
	}
}
