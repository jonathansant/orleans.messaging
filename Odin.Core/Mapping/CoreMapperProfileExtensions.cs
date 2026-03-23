using Odin.Core.Mapping;

// ReSharper disable once CheckNamespace
namespace AutoMapper;

public static class CoreMapperProfileExtensions
{
	public static IMapperConfigurationExpression AddOdinCoreMappingProfiles(this IMapperConfigurationExpression mapper)
	{
		mapper.AddProfile<DurationMappingProfile>();

		return mapper;
	}
}
