using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Odin.Core.Config;

public class InMemoryJsonConfigurationSource : IConfigurationSource
{
	/// <summary>
	/// The initial key value configuration pairs.
	/// </summary>
	public JObject InitialData { get; set; }

	/// <inheritdoc />
	/// <summary>
	/// Builds the <see cref="T:Odin.Core.Config.InMemoryJsonConfigurationSource" /> for this source.
	/// </summary>
	/// <param name="builder">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationBuilder" />.</param>
	/// <returns>A <see cref="T:Odin.Core.Config.InMemoryJsonConfigurationSource" /></returns>
	public IConfigurationProvider Build(IConfigurationBuilder builder) => new InMemoryJsonConfigurationProvider(this);
}
