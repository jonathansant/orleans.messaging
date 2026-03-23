using AutoMapper;
using Odin.Core.Timing;

namespace Odin.Core.Mapping;

public class DurationMappingProfile : Profile
{
	public DurationMappingProfile()
	{
		CreateMap<string, TimeSpan?>()
			.ConvertUsing(src => src.ToTimeSpanFromDuration());

		CreateMap<TimeSpan?, string>()
			.ConvertUsing(src => src.ToDurationString());
	}
}
