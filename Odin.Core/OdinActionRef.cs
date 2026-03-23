using Odin.Core.Error;
using Odin.Core.Json;

namespace Odin.Core;

public interface IActionRefable<T> : IActionRefable
	where T : OdinActionRef
{
	new T? ActionRef { get; set; }

	OdinActionRef? IActionRefable.ActionRef
	{
		get => ActionRef;
		set => ActionRef = (T)value!;
	}
}

public interface IActionRefable
{
	OdinActionRef? ActionRef { get; set; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record OdinActionRef
{
	protected virtual string DebuggerDisplay => $"CorrelationId: '{CorrelationId}', UserId: '{UserId}', ClientId: '{ClientId}', Tags: {Tags.ToDebugString()}";

	[Id(0)]
	[HashIgnore]
	public string? CorrelationId { get; set; }

	[Id(1)]
	[HashIgnore]
	public string? ClientId { get; set; }

	[Id(2)]
	public string? UserId { get; set; }

	[Id(3)]
	public HashSet<string>? Tags { get; set; }

	[Id(4)]
	public ServiceActionRef? ServiceActionRef { get; set; } // todo: remove duplicate props from OdinActionRef but even more breaking changes

	public void EnsureAuthenticated()
	{
		if (!IsAuthenticated())
			throw new ErrorResult()
				.WithUnauthorized()
				.AsApiErrorException();
	}

	public void EnsureUser()
	{
		if (!IsUser())
			throw new ErrorResult()
				.WithForbidden()
				.AsApiErrorException();
	}

	public bool IsAuthenticated()
		=> !UserId.IsNullOrEmpty() || !ClientId.IsNullOrEmpty();

	/// <summary>
	/// Checks whether user is assigned and its invoked on user's behalf e.g. <see cref="UserId"/> is specified.
	/// NOTE: Can be 'client' or 'user' ('client' when client-credentials) or system when directly called internally without any.
	/// </summary>
	public bool IsUser()
		=> !UserId.IsNullOrEmpty();

	public virtual OdinActionRef RemoveUserRef()
	{
		if (IsUser())
			return this with { UserId = null };

		return this;
	}
}

public static class OdinActionRefExtensions
{
	public static bool IsAuthenticated<TAction>(this TAction action)
		where TAction : IActionRefable
		=> action.ActionRef?.IsAuthenticated() == true;

	/// <inheritdoc cref="OdinActionRef.IsUser"/>
	public static bool IsUser<TAction>(this TAction action)
		where TAction : IActionRefable
		=> action.ActionRef?.IsUser() == true;

	public static TAction RemoveUserRef<TAction>(this TAction action)
		where TAction : IActionRefable
	{
		action.ActionRef = action.ActionRef?.RemoveUserRef();
		return action;
	}
}
