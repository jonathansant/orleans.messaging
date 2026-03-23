using Humanizer;

namespace Odin.Core.Error;

/// <summary>
/// Represents the errors for the fields.
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public class FieldErrorState : Dictionary<string, List<ErrorDataField>>
{
	[Id(0)]
	public ErrorResult? ErrorResult { get; set; }

	[Id(1)]
	private readonly List<IDictionary<string, string>> _fieldMappings = new(1);
	[Id(2)]
	private readonly List<IDictionary<string, string>> _errorMappings = new(1);

	protected string DebuggerDisplay => $"Errors: {this.ToDebugString()}";

	public FieldErrorState()
	{
	}

	public FieldErrorState(ErrorResult? errorResult = null)
	{
		ErrorResult = errorResult;
	}

	/// <summary>
	/// Add an error to a field.
	/// </summary>
	/// <param name="field">Field to add error for.</param>
	/// <param name="error">Error to add to the field.</param>
	/// <param name="fieldFormatter">Field formatter to be used. default to use <see cref="StringExtensions.ToCamelCase"/></param>
	public FieldErrorState AddError(string field, ErrorDataField error, Func<string, string>? fieldFormatter = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(field, nameof(field));
		ArgumentNullException.ThrowIfNull(error, nameof(error));

		fieldFormatter ??= (x => x.ToCamelCase());
		field = MapFieldOrDefault(fieldFormatter(field));

		if (!TryGetValue(field, out var fieldErrors))
		{
			fieldErrors = new();
			Add(field, fieldErrors);
		}

		error.ErrorCode = MapErrorOrDefault(error.ErrorCode);
		fieldErrors.Add(error);
		return this;
	}

	/// <summary>
	/// Add errors to a field.
	/// </summary>
	/// <param name="field">Field to add error for.</param>
	/// <param name="errors">Errors to add to the field.</param>
	public FieldErrorState AddErrors(string field, IEnumerable<ErrorDataField> errors)
	{
		ArgumentException.ThrowIfNullOrEmpty(field, nameof(field));
		ArgumentNullException.ThrowIfNull(errors, nameof(errors));

		field = MapFieldOrDefault(field.Camelize());

		if (!TryGetValue(field, out var fieldErrors))
		{
			fieldErrors = new();
			Add(field, fieldErrors);
		}

		fieldErrors.AddRange(_errorMappings.IsNullOrEmpty() ? errors : MapErrors(errors, _errorMappings));
		return this;
	}

	public FieldErrorState AddErrors(IDictionary<string, List<ErrorDataField>> errors)
	{
		if (errors.IsNullOrEmpty())
			return this;
		foreach (var fieldError in errors)
			AddErrors(fieldError.Key, fieldError.Value);
		return this;
	}

	/// <summary>
	/// Register mappings for fields, to use instead of the original fields.
	/// </summary>
	/// <param name="mapping">Field mappings.</param>
	public FieldErrorState WithFieldMappings(IDictionary<string, string>? mapping)
	{
		if (mapping == null)
			return this;
		_fieldMappings.Insert(0, mapping);
		return this;
	}

	/// <summary>
	/// Register mappings for errors, to use instead of the original error.
	/// </summary>
	/// <param name="mapping">Error mappings.</param>
	public FieldErrorState WithErrorMappings(IDictionary<string, string>? mapping)
	{
		if (mapping == null)
			return this;
		_errorMappings.Insert(0, mapping);
		return this;
	}

	/// <summary>
	/// Register mappings for fields, to use instead of the original fields.
	/// </summary>
	/// <param name="mapping">Field mappings.</param>
	public FieldErrorState WithFieldMappings(IEnumerable<IDictionary<string, string>>? mapping)
	{
		if (mapping == null)
			return this;
		_fieldMappings.InsertRange(0, mapping);
		return this;
	}

	/// <summary>
	/// Register mappings for errors, to use instead of the original error.
	/// </summary>
	/// <param name="mapping">Error mappings.</param>
	public FieldErrorState WithErrorMappings(IEnumerable<IDictionary<string, string>>? mapping)
	{
		if (mapping == null)
			return this;
		_errorMappings.InsertRange(0, mapping);
		return this;
	}

	public new List<ErrorDataField> this[string field]
	{
		get => base[field];
		set => base[field] = value;
	}

	/// <summary>
	/// Rebuilds fields and their errors with the mappings.
	/// </summary>
	public void Rebuild()
	{
		if (Count == 0) return;
		RemapFields();
		RemapErrors();
	}

	/// <summary>
	/// Get mapped field or get specified as default.
	/// </summary>
	/// <param name="field"></param>
	public string MapFieldOrDefault(string field) => MapOrDefault(field, _fieldMappings);

	/// <summary>
	/// Get mapped error or get specified as default.
	/// </summary>
	/// <param name="error">Error to get map for.</param>
	public string MapErrorOrDefault(string error) => MapOrDefault(error, _errorMappings);

	private void RemapFields()
	{
		if (_fieldMappings.IsNullOrEmpty()) return;

		foreach (var fieldMapping in _fieldMappings)
		{
			foreach (var fieldMap in fieldMapping)
			{
				if (!TryGetValue(fieldMap.Key, out var errors))
					continue;

				Remove(fieldMap.Key);
				Add(fieldMap.Value, errors);
			}
		}
	}

	private void RemapErrors()
	{
		if (_errorMappings.IsNullOrEmpty()) return;

		foreach (var key in Keys.ToList())
		{
			var errors = this[key];
			this[key] = MapErrors(errors, _errorMappings).ToList();
		}
	}

	private static string MapOrDefault(string error, ICollection<IDictionary<string, string>>? mappings)
		=> mappings.IsNullOrEmpty() ? error : MapError(error, mappings) ?? error;

	private static IEnumerable<ErrorDataField> MapErrors(IEnumerable<ErrorDataField> errors, IReadOnlyCollection<IDictionary<string, string>> errorMappings)
	{
		foreach (var originalError in errors)
		{
			foreach (var errorMapping in errorMappings)
			{
				if (!errorMapping.TryGetValue(originalError.ErrorCode, out var error))
					continue;
				originalError.ErrorCode = error;
				yield return originalError;
				yield break;
			}
			yield return originalError;
		}
	}

	private static string? MapError(string error, IEnumerable<IDictionary<string, string>> errorMappings)
	{
		foreach (var errorMapping in errorMappings)
		{
			if (errorMapping.TryGetValue(error, out var mappedError))
				return mappedError;
		}

		return null;
	}
}

public static class FieldErrorStateExtensions
{
	/// <summary>
	/// Add an error to a field.
	/// </summary>
	/// <param name="fieldErrors"></param>
	/// <param name="field">Field to add error for.</param>
	/// <param name="error">Error to add to the field.</param>
	public static FieldErrorState AddError(this FieldErrorState fieldErrors, string field, string error)
		=> fieldErrors.AddError(field, new() { ErrorCode = error });

	/// <summary>
	/// Add an errors to a field.
	/// </summary>
	/// <param name="fieldErrors"></param>
	/// <param name="field">Field to add error for.</param>
	/// <param name="errors">Error codes to add to the field.</param>
	public static FieldErrorState AddErrors(this FieldErrorState fieldErrors, string field, IEnumerable<string> errors)
		=> fieldErrors.AddErrors(field, errors.Select(errorCode => new ErrorDataField { ErrorCode = errorCode }));
}
