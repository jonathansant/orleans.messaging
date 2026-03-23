using Microsoft.Extensions.Configuration;

namespace Odin.Core.Config;

public class InMemoryJsonConfigurationProvider : ConfigurationProvider
{
	/// <inheritdoc />
	/// <summary>
	/// Initialize a new instance from the source.
	/// </summary>
	/// <param name="source">The source settings.</param>
	public InMemoryJsonConfigurationProvider(InMemoryJsonConfigurationSource source)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));

		Data = JsonConfigurationParser.Parse(source.InitialData);
	}
}
