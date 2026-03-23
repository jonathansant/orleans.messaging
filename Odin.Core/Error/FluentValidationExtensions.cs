using FluentValidation.Results;
using Odin.Core.Error;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

public static class ValidationFailureExtensions
{
	private static readonly HashSet<string> ErrorOverrides =
	[
		OdinErrorCodes.AlreadyExists,
		OdinErrorCodes.AlreadyExistsArchived
	];

	public static ErrorResult ToErrorResult(
		this IList<ValidationFailure> errors,
		string context = ErrorContextTypes.Validation,
		string? origin = null
	)
	{
		var result = new ErrorResult
		{
			ErrorCode = OdinErrorCodes.ValidationFailed,
			Context = context
		};

		foreach (var error in errors)
		{
			if (ErrorOverrides.TryGetValue(error.ErrorCode, out var errorCode))
			{
				result.ErrorCode = errorCode;
				break;
			}
		}

		result.Origin = origin ?? result.Origin;
		foreach (var entry in errors)
		{
			result.AddField(
				entry.PropertyName,
				new ErrorDataField
				{
					ErrorMessage = entry.ErrorMessage,
					ErrorCode = entry.ErrorCode,
					Value = entry.AttemptedValue,
					Data = entry.CustomState
				}
			);
		}
		return result;
	}
}

public static class ValidationResultExtensions
{
	public static void EnsureValidOrThrowApiError(this ValidationResult validation, Action<ErrorResult>? errorAction = null)
	{
		if (!validation.IsValid)
			throw validation.Errors.ToErrorResult()
				.With(err => errorAction?.Invoke(err))
				.AsApiErrorException(HttpStatusCode.BadRequest);
	}
}
