using FluentValidation;
using Humanizer;
using Odin.Core.Files;

namespace Odin.Core.Validation;

public class FileModelValidator : AbstractValidator<FileModel>
{
	public FileModelValidator(double maxSizeMb, HashSet<string> supportedMimeTypes, double? minSizeMb = null)
	{
		RuleFor(x => x.Content)
			.NotNull()
			.WithErrorCode(OdinErrorCodes.Required)
			.DependentRules(() =>
			{
				When(
					_ => minSizeMb.HasValue,
					() =>
					{
						var bytesMinimum = (int)minSizeMb.GetValueOrDefault().Megabytes().Bytes;
						RuleFor(x => x.Content.Length)
							.GreaterThanOrEqualTo(bytesMinimum)
							.WithErrorCode(OdinErrorCodes.Validation.SizeLimitTooSmall)
							.WithMessage($"File size must exceed {minSizeMb}mb")
							.OverridePropertyName(string.Empty)
							;
					}
				);

				var bytesLimit = (int)maxSizeMb.Megabytes().Bytes;
				RuleFor(x => x.Content.Length)
					.LessThanOrEqualTo(bytesLimit)
					.WithErrorCode(OdinErrorCodes.Validation.SizeLimitExceed)
					.WithMessage($"File exceeds the max size of {maxSizeMb}mb")
					.OverridePropertyName(string.Empty)
					;
			});

		RuleFor(x => x.MimeType)
			.Must(supportedMimeTypes.Contains).WithErrorCode(OdinErrorCodes.Validation.InvalidFileType)
			.WithMessage($"Unsupported file type, supported types: {string.Join(", ", supportedMimeTypes)}")
			.OverridePropertyName(string.Empty)
			;
	}
}
