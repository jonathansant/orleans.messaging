using FluentValidation;
using Odin.Core.Error;
using Odin.Core.Validation;

namespace Odin.Core;

public static class ValidatorContextExtensions
{
	public const string ErrorMessage = "ErrorMessage";

	public static IValidationContext WithItem<T>(this IValidationContext context, T value, string? key = null)
	{
		key ??= typeof(T).GetDemystifiedName();
		context.RootContextData[key] = value;
		return context;
	}

	public static T? GetItemOrDefault<T>(this IValidationContext context, string? key = null)
	{
		key ??= typeof(T).GetDemystifiedName();
		return context.RootContextData.TryGetValue(key, out var value) ? (T)value : default;
	}

	public static T GetItem<T>(this IValidationContext context, string? key = null)
	{
		key ??= typeof(T).GetDemystifiedName();
		var item = context.GetItemOrDefault<T>(key) ?? throw new OdinKeyNotFoundException(key, $"ValidationContext requires Key '{key}' which is not found.");
		return item;
	}

	public static async Task<T> GetOrSetItem<T>(this IValidationContext context, Func<Task<T>> setter, string? key = null)
	{
		key ??= typeof(T).GetDemystifiedName();
		var item = context.GetItemOrDefault<T>(key);
		if (item != null)
			return item;

		item = await setter();

		context.WithItem(item, key);

		return item;
	}

	public static void EnsureValidOrThrowApiError<T>(
		this ValidationContextableResult<T> validation,
		Action<ErrorResult>? errorAction = null
	) => validation.Result.EnsureValidOrThrowApiError(errorAction);

	public static void AddErrorMessage<T>(this ValidationContext<T> context, string errorMessage)
		=> context.MessageFormatter.AppendArgument(ErrorMessage, errorMessage);
}
