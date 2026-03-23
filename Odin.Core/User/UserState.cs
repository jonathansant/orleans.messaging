using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Odin.Core.User;

// todo: change to camelized instead but contentful UserState is used as kebabed
[JsonConverter(typeof(StringEnumConverter))]
public enum UserState
{
	[EnumMember(Value = "logged-in")]
	LoggedIn,

	[EnumMember(Value = "logged-out")]
	LoggedOut
}

public interface IUserStateFilterable
{
	UserState? UserState { get; set; }
}

public static class UserStateExt
{
	public static bool IsUserStateMatch(this IUserStateFilterable state, bool isAuthenticated)
		=> IsMatch(state.UserState, isAuthenticated);

	private static bool IsMatch(UserState? state, bool isAuthenticated)
	{
		if (!state.HasValue)
			return true;

		if (isAuthenticated)
			return state == UserState.LoggedIn;

		return state == UserState.LoggedOut;
	}
}
