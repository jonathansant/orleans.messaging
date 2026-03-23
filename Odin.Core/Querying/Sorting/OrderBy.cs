using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Odin.Core.Querying.Sorting;

/// <summary>
/// Represents a sort order specification for query results.
/// </summary>
/// <param name="Field">The field to sort by.</param>
/// <param name="Direction">The sort direction (ascending or descending).</param>
/// <param name="Values">Optional values for priority sorting (e.g. ["valla", "diablo", "kerrigan"]).</param>
/// <param name="Aggregate">Optional aggregate function for collection properties (e.g. "Max", "Min", "Sum", "Average", "Count").</param>
[GenerateSerializer]
public record struct OrderBy(
	string Field,
	SortDirection Direction = SortDirection.Asc,
	object[]? Values = null,
	string? Aggregate = null
);

[JsonConverter(typeof(StringEnumConverter))]
public enum SortDirection
{
	[EnumMember(Value = "asc")]
	Asc,
	[EnumMember(Value = "desc")]
	Desc
}
