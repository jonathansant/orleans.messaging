using Humanizer;

namespace Odin.Core.Error;

// todo: remove this class when all usages are removed (replaced with FieldErrorState)
/// <summary>
/// Represents the errors for the fields.
/// </summary>
[Obsolete("Use FieldErrorState instead.")]
[GenerateSerializer]
public class FieldErrorDictionary : Dictionary<string, List<string>>
{
	[Id(0)]
	private readonly List<IDictionary<string, string>> _fieldMappings = new List<IDictionary<string, string>>(1);
	[Id(1)]
	private readonly List<IDictionary<string, string>> _errorMappings = new List<IDictionary<string, string>>(1);

	public FieldErrorDictionary()
	{
	}

	/// <summary>
	/// Add an error to a field.
	/// </summary>
	/// <param name="field">Field to add error for.</param>
	/// <param name="error">Error to add to the field.</param>
	/// <param name="fieldFormatter">Field formatter to be used. default to use <see cref="StringExtensions.ToCamelCase"/></param>
	public FieldErrorDictionary AddError(string field, string error, Func<string, string>? fieldFormatter = null)
	{
		if (field.IsNullOrEmpty())
			return this;
		fieldFormatter ??= (x => x.ToCamelCase());
		field = MapFieldOrDefault(fieldFormatter(field));

		if (!TryGetValue(field, out var fieldErrors))
		{
			fieldErrors = new List<string>();
			Add(field, fieldErrors);
		}
		fieldErrors.Add(MapErrorOrDefault(error));
		return this;
	}

	/// <summary>
	/// Add errors to a field.
	/// </summary>
	/// <param name="field">Field to add error for.</param>
	/// <param name="errors">Errors to add to the field.</param>
	public FieldErrorDictionary AddErrors(string field, IEnumerable<string> errors)
	{
		if (field.IsNullOrEmpty())
			return this;
		field = MapFieldOrDefault(field.Camelize());

		if (!TryGetValue(field, out var fieldErrors))
		{
			fieldErrors = new List<string>();
			Add(field, fieldErrors);
		}

		fieldErrors.AddRange(_errorMappings.IsNullOrEmpty() ? errors : MapErrors(errors, _errorMappings));
		return this;
	}

	public FieldErrorDictionary AddErrors(IDictionary<string, List<string>> errors)
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
	/// <returns></returns>
	public FieldErrorDictionary WithFieldMappings(IDictionary<string, string>? mapping)
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
	public FieldErrorDictionary WithErrorMappings(IDictionary<string, string>? mapping)
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
	public FieldErrorDictionary WithFieldMappings(IEnumerable<IDictionary<string, string>>? mapping)
	{
		if (mapping == null)
			return this;
		_fieldMappings.InsertRange(0, mapping);
		return this;
	}

	/// <summary>
	/// Register mappings for errors, to use instead of the original error.
	/// </summary>
	/// <param name="errorMapping">Error mappings.</param>
	public FieldErrorDictionary WithErrorMappings(IEnumerable<IDictionary<string, string>>? errorMapping)
	{
		if (errorMapping == null)
			return this;
		_errorMappings.InsertRange(0, errorMapping);
		return this;
	}

	public new List<string> this[string field]
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

	private static string MapOrDefault(string error, ICollection<IDictionary<string, string>> mappings)
		=> mappings.IsNullOrEmpty() ? error : MapError(error, mappings) ?? error;

	private static IEnumerable<string> MapErrors(IEnumerable<string> errors, IReadOnlyCollection<IDictionary<string, string>> errorMappings)
	{
		foreach (var originalError in errors)
		{
			foreach (var errorMapping in errorMappings)
			{
				if (!errorMapping.TryGetValue(originalError, out var error)) continue;
				yield return error;
				yield break;
			}
			yield return originalError;
		}
	}

	private static string MapError(string error, IEnumerable<IDictionary<string, string>> errorMappings)
	{
		foreach (var errorMapping in errorMappings)
		{
			if (errorMapping.TryGetValue(error, out var mappedError))
				return mappedError;
		}

		return null;
	}
}
