namespace Odin.Orleans.Core.Tenancy;

/// <summary>
/// Used to specify the Grain (or so) is not tenant specific, and is shared across all tenants.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SharedTenantAttribute : Attribute
{
}
