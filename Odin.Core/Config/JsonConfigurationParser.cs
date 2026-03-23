using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Odin.Core.Config;

public class JsonConfigurationParser
{
	private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Stack<string> _context = new();
	private string? _currentPath;

	public static IDictionary<string, string> Parse(JObject input)
		=> new JsonConfigurationParser().ParseJObject(input);

	private IDictionary<string, string> ParseJObject(JObject input)
	{
		_data.Clear();
		VisitJObject(input);
		return _data;
	}

	private void VisitJObject(JObject jObject)
	{
		foreach (var property in jObject.Properties())
		{
			EnterContext(property.Name);
			VisitProperty(property);
			ExitContext();
		}
	}

	private void VisitProperty(JProperty property) => VisitToken(property.Value);

	private void VisitToken(JToken token)
	{
		switch (token.Type)
		{
			case JTokenType.Object:
				VisitJObject(token.Value<JObject>()!);
				break;

			case JTokenType.Array:
				VisitArray(token.Value<JArray>()!);
				break;

			case JTokenType.Integer:
			case JTokenType.Float:
			case JTokenType.String:
			case JTokenType.Boolean:
			case JTokenType.Bytes:
			case JTokenType.Raw:
			case JTokenType.Null:
				VisitPrimitive(token.Value<JValue>()!);
				break;
		}
	}

	private void VisitArray(JArray array)
	{
		for (int index = 0; index < array.Count; index++)
		{
			EnterContext(index.ToString());
			VisitToken(array[index]);
			ExitContext();
		}
	}

	private void VisitPrimitive(JValue data)
	{
		var key = _currentPath;
		_data[key] = data.ToString(CultureInfo.InvariantCulture);
	}

	private void EnterContext(string context)
	{
		_context.Push(context);
		_currentPath = ConfigurationPath.Combine(_context.Reverse());
	}

	private void ExitContext()
	{
		_context.Pop();
		_currentPath = ConfigurationPath.Combine(_context.Reverse());
	}
}
