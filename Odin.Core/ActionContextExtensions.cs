using FluentlyHttpClient;
using Odin.Core.Http;

namespace Odin.Core;

public static class ActionContextExtensions
{
	public const string ActionContextKey = "ACTION_CONTEXT";

	/// <summary>
	/// Converts from <see cref="IActionContext"/> to <see cref="HttpRequestClientContext"/>
	/// </summary>
	public static HttpRequestClientContext ToHttpRequestClientContext(this IActionContext actionContext)
		=> new()
		{
			Items = new()
			{
				{ActionContextKey, actionContext}
			}
		};

	/// <summary>
	/// Get action context from the items.
	/// </summary>
	/// <param name="request">Request to get time from.</param>
	/// <returns>Returns errors from response.</returns>
	public static TAction? GetActionContext<TAction>(this FluentHttpRequest request)
		where TAction : class, IActionContext
	{
		if (request.Items.TryGetValue(ActionContextKey, out var actionContext))
			return (TAction)actionContext;

		return null;
	}
}
