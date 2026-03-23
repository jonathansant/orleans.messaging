using Odin.Core.Utils;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Odin.Core.Key;

public interface IKeyBuilderFactory<T>
	where T : class
{
	OdinKeyBuilder<T> Get(Expression<Func<T, object>> keySelector);
}

public class KeyBuilderFactory<T> : IKeyBuilderFactory<T>
	where T : class
{
	private readonly ConcurrentDictionary<string, OdinKeyBuilder<T>> _keyBuilders = new();

	public OdinKeyBuilder<T> Get(Expression<Func<T, object>> keySelector)
	{
		var props = keySelector.Props();
		var indexer = props.Select(x => x.Name).OrderBy(x => x).JoinTokens();
		return _keyBuilders.TryGetValue(indexer, out var keyBuilder) ? keyBuilder : Create(indexer);
	}

	private OdinKeyBuilder<T> Create(string indexer)
	{
		var keyBuilder = new OdinKeyBuilder<T>();
		_keyBuilders.TryAdd(indexer, keyBuilder);
		return keyBuilder;
	}
}

public class OdinKeyBuilder<T>
	where T : class
{
	private Func<string, Task<bool>>? _existsCheck;
	private Expression<Func<T, object>>? _indexerExpression;
	private Func<T, object>? _indexerSelector;
	private Func<string[], string> _keyGen = null!;
	private int _maxLength;
	private int _suffixLength;

#pragma warning disable CS8618
	public OdinKeyBuilder()
#pragma warning restore CS8618
	{
		WithGenerator(BuildKey)
			.WithSuffixLength(4);
	}

	public OdinKeyBuilder<T> WithGenerator(Func<string[], string> keyGen)
	{
		_keyGen = keyGen;
		return this;
	}

	/// <summary>
	/// Max Length of the property that is going to be set.
	/// </summary>
	/// <param name="maxLength"></param>
	public OdinKeyBuilder<T> WithMaxLength(int maxLength)
	{
		_maxLength = maxLength;
		return this;
	}

	/// <summary>
	/// Suffix length that will be generated.
	/// </summary>
	/// <param name="suffixLength"></param>
	public OdinKeyBuilder<T> WithSuffixLength(int suffixLength)
	{
		_suffixLength = suffixLength;
		return this;
	}

	public OdinKeyBuilder<T> WithExistsCheck(Func<string, Task<bool>> existsCheck)
	{
		_existsCheck = existsCheck;
		return this;
	}

	/// <summary>
	/// Index on which the item is to be checked against.
	/// </summary>
	public OdinKeyBuilder<T> WithIndexer(Expression<Func<T, object>> indexerExpression)
	{
		_indexerExpression = indexerExpression;
		_indexerSelector = indexerExpression.Compile();
		return this;
	}

	/// <summary>
	/// Generate the unique key built using the items array.
	/// </summary>
	public async Task<string> Generate(params string[] items)
	{
		var key = _keyGen.Invoke(items);
		key = Transform(key);

		var exists = _existsCheck != null && await _existsCheck.Invoke(key);
		if (!exists)
			return key;

		key = _keyGen.Invoke(items);
		return Transform(key, true);
	}

	/// <summary>
	/// Generate the composite key built using the items array and the model (according to the OdinKeyBuilder Indexer).
	/// </summary>
	public async Task<string> GenerateComposite(T model, params string[] items)
	{
		if (model == null)
			throw new ArgumentNullException(nameof(model));
		if (_indexerExpression == null)
			throw new InvalidOperationException($"{nameof(_indexerExpression)} must be set up with the builder before calling this method.");
		if (_indexerSelector == null)
			throw new InvalidOperationException($"{nameof(_indexerSelector)} must be set up with the builder before calling this method.");

		var key = _keyGen.Invoke(items);
		key = Transform(key);

		var propKeyValueMap = new Dictionary<string, string>();
		var propNames = _indexerExpression.Props()
			.Select(x => x.Name)
			.ToList();

		var partialModel = _indexerSelector.Invoke(model);

		foreach (var propName in propNames)
		{
			var propInfo = partialModel.GetType().GetProperty(propName);

			var value = propInfo?.GetValue(partialModel, null);
			if (value == null)
			{
				propKeyValueMap.Add(propName, key);
				continue;
			}
			propKeyValueMap.Add(propName, value.ToString());
		}

		var indexKey = propKeyValueMap.OrderBy(x => x.Key).Select(x => x.Value).JoinTokens();

		var exists = indexKey != null && _existsCheck != null && await _existsCheck.Invoke(indexKey);
		if (!exists)
			return key;

		key = _keyGen.Invoke(items);
		return Transform(key, true);
	}

	private string BuildKey(params string[] items)
		=> string.Join(" ", items)
			.ToSlugify()
		;

	private string Transform(string input, bool isGeneratedKey = false)
	{
		if (isGeneratedKey)
		{
			if (_maxLength > 0 && input.Length >= _maxLength)
				input = input[..(_maxLength - (_suffixLength + 1))];

			input += $"-{RandomUtils.GenerateStringLower(_suffixLength, _suffixLength)}";
		}
		else
		{
			if (_maxLength > 0 && input.Length >= _maxLength)
				input = input[.._maxLength];
		}

		return input;
	}
}
