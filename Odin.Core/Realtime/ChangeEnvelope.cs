using System.Diagnostics.CodeAnalysis;

namespace Odin.Core.Realtime;

[GenerateSerializer]
public abstract record ChangeEnvelope
{
	[Id(0)]
	public required string Key { get; set; }
	[Id(1)]
	public required string Type { get; set; }
	[Id(2)]
	public required OdinMessageActionType? ActionType { get; set; }
	[Id(3)]
	public string MessageId { get; set; }
	[Id(4)]
	public string CorrelationId { get; set; }
	[Id(5)]
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MinValue;
}

[GenerateSerializer]
public record ChangeEnvelope<TPayload> : ChangeEnvelope
{
	[Id(0)]
	public TPayload? Payload { get; set; }
}

[GenerateSerializer]
public record ClientChangeEnvelope<TPayload> : ChangeEnvelope<TPayload>
{
	public ClientChangeEnvelope()
	{
		MessageId = Guid.NewGuid().ToString();
		Timestamp = DateTimeOffset.UtcNow;
	}

	[SetsRequiredMembers]
	public ClientChangeEnvelope(OdinMessageActionType actionType, TPayload payload, string? type = null)
		: this()
	{
		ActionType = actionType;
		Payload = payload;
		Type = type ?? typeof(TPayload).GetDemystifiedName().ToLower();
	}
}

public static class ClientChangeEnvelope
{
	public static ClientChangeEnvelope<TPayload> Create<TPayload>(
		string key,
		OdinMessageActionType messageType,
		TPayload payload,
		string? type = null
	)
		=> new(messageType, payload, type)
		{
			Key = key,
		};

	public static ClientChangeEnvelope<TPayload> ToClientChangeEnvelope<TPayload>(this ChangeEnvelope envelope, TPayload payload, string? key = null)
		=> new()
		{
			Key = key ?? envelope.Key,
			Timestamp = envelope.Timestamp,
			Type = envelope.Type,
			ActionType = envelope.ActionType,
			Payload = payload,
			MessageId = envelope.MessageId,
			CorrelationId = envelope.CorrelationId,
		};

	public static ClientChangeEnvelope<TPayload> ToClientChangeEnvelope<TPayload>(this ChangeEnvelope<TPayload> envelope)
		=> ToClientChangeEnvelope(envelope, envelope.Payload);
}

[GenerateSerializer]
public record InternalChangeEnvelope<TPayload> : ChangeEnvelope<TPayload>
{
	[Id(0)]
	public string MessageSourceSignature { get; set; }

	[SetsRequiredMembers]
	public InternalChangeEnvelope()
	{
		MessageId = Guid.NewGuid().ToString();
		Timestamp = DateTimeOffset.UtcNow;
		Type = typeof(TPayload).GetDemystifiedName().ToLower();
	}
}

public static class InternalChangeEnvelope
{
	public static InternalChangeEnvelope<TPayload> Create<TPayload>(string key, TPayload payload)
		=> new()
		{
			Key = key,
			Payload = payload,
			ActionType = null
		};

	public static InternalChangeEnvelope<TPayload> Create<TPayload>(
		string key,
		TPayload payload,
		OdinMessageActionType messageType,
		string correlationId
	)
		=> new()
		{
			Key = key,
			ActionType = messageType,
			Payload = payload,
			CorrelationId = correlationId,
		};

	public static InternalChangeEnvelope<TPayload> ToInternalChangeEnvelope<TPayload>(
		this ChangeEnvelope envelope,
		TPayload payload
	) where TPayload : IChange
		=> new()
		{
			Key = envelope.Key,
			Timestamp = envelope.Timestamp,
			Type = envelope.Type,
			Payload = payload,
			ActionType = envelope.ActionType,
			MessageId = envelope.MessageId,
			CorrelationId = envelope.CorrelationId,
		};
}
