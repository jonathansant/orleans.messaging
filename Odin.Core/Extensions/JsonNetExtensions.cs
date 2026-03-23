using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Odin.Core;

public static class JsonNetExtensions
{
	// todo: move non extensions to JsonUtils
	/// <summary>
	/// Read JObject from json file.
	/// </summary>
	/// <param name="filePath"></param>
	/// <returns>Returns <see cref="JObject"/> or null when file does not exists.</returns>
	public static async Task<JObject?> ReadFromFileAsync(string filePath)
	{
		if (!File.Exists(filePath))
			return null;

		var fileContent = await File.ReadAllTextAsync(filePath);
		return JObject.Parse(fileContent);
	}

	/// <summary>
	/// Read JObject from json file.
	/// </summary>
	/// <param name="filePath"></param>
	/// <returns>Returns <see cref="JObject"/> or null when file does not exists.</returns>
	public static JObject? ReadFromFile(string filePath)
	{
		if (!File.Exists(filePath))
			return null;

		var fileContent = File.ReadAllText(filePath);
		return JObject.Parse(fileContent);
	}

	/// <summary>
	/// Converts a <see cref="JObject"/> to <see cref="JsonDocument"/>.
	/// </summary>
	/// <param name="jObject">Object to transform.</param>
	public static JsonDocument ToJsonDocument(this JObject jObject)
	{
		var objectStr = jObject.ToString();
		var document = JsonDocument.Parse(objectStr);
		return document;
	}

	/// <summary>
	/// Converts json content/string to <see cref="JsonDocument"/>.
	/// </summary>
	/// <param name="jsonContent">Json content to transform.</param>
	public static JsonDocument ToJsonDocument(this string jsonContent)
	{
		var document = JsonDocument.Parse(jsonContent);
		return document;
	}
}
