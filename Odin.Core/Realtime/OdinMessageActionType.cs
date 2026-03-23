using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Odin.Core.Realtime;

// todo: update to use StoreEntityOpType instead of OdinMessageActionType where applicable + remove store ops e.g. hydrate, sync?
[JsonConverter(typeof(StringEnumConverter), true)]
public enum OdinMessageActionType
{
	[EnumMember(Value = "unknown")]
	Unknown = -1,
	[EnumMember(Value = "create")]
	Create = 0,
	[EnumMember(Value = "update")]
	Update,
	[EnumMember(Value = "delete")]
	Delete,
	[EnumMember(Value = "archive")]
	Archive,
	[EnumMember(Value = "unarchive")]
	Unarchive,
	[EnumMember(Value = "sync")]
	Sync,
	[EnumMember(Value = "hydrate")]
	Hydrate
}

public static class MessageTypeExtensions
{
	/// <summary>
	/// Calculates the mutation action type based on the current and new action types. e.g. Create -> Delete = null, Create -> Update = Create. etc...
	/// </summary>
	/// <param name="current">Current value for the action type (e.g. prev).</param>
	/// <param name="newType">New value/action for the action type (e.g. new/future).</param>
	/// <returns>Returns a new computed value for the action based on actions.</returns>
	public static OdinMessageActionType? CalculateMutationOpActionType(this OdinMessageActionType? current, OdinMessageActionType newType)
		=> current switch
		{
			OdinMessageActionType.Create when newType == OdinMessageActionType.Update => OdinMessageActionType.Create,
			OdinMessageActionType.Create when newType == OdinMessageActionType.Delete => null,
			OdinMessageActionType.Archive or OdinMessageActionType.Unarchive when newType == OdinMessageActionType.Update => current,
			_ => newType
		};
}
