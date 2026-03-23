using Odin.Core.Config;

namespace Odin.Core.Organizations;

public class OrganizationConfig
{
	public string Id { get; set; }
	public string ClusterId { get; set; }
	public DynamicSection Sections { get; set; }
}
