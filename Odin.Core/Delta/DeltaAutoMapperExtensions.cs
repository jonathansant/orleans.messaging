using Odin.Core;

// ReSharper disable once CheckNamespace
namespace AutoMapper;

public static class DeltaAutoMapperExtensions
{
	extension(IMapperConfigurationExpression mapper)
	{
		/// <summary>
		/// Configures mapping rules between the generic Delta type and its underlying type for use with the mapper.
		/// </summary>
		/// <remarks>
		/// This method sets up bidirectional mappings between Delta&lt;T&gt; and T, enabling conversion in both
		/// directions. Use this method to ensure that the mapper can handle delta objects when mapping between types.
		/// </remarks>
		/// <typeparam name="T">The type for which delta mapping is configured.</typeparam>
		/// <returns>An object that can be used to further configure the mapper.</returns>
		public IMapperConfigurationExpression CreateDeltaMap<T>()
		{
			mapper.CreateMap<Delta<T>, T>()
				.ConvertUsing((src, dest, _) => src is null ? dest : src.Get() ?? dest);
			mapper.CreateMap<T, Delta<T>>()
				.ConvertUsing((src, _, _) => src.AsDelta());

			return mapper;
		}
	}

	extension(Profile profile)
	{
		/// <summary>
		/// Configures mapping rules between the generic Delta type and its underlying type for use with the mapper.
		/// </summary>
		/// <remarks>
		/// This method sets up bidirectional mappings between Delta&lt;T&gt; and T, enabling conversion in both
		/// directions. Use this method to ensure that the mapper can handle delta objects when mapping between types.
		/// </remarks>
		/// <typeparam name="T">The type for which delta mapping is configured.</typeparam>
		/// <returns>An object that can be used to further configure the mapper.</returns>
		public Profile CreateDeltaMap<T>()
		{
			profile.CreateMap<Delta<T>, T>()
				.ConvertUsing((src, dest, _) => src is null ? dest : src.Get() ?? dest);
			profile.CreateMap<T, Delta<T>>()
				.ConvertUsing((src, _, _) => src.AsDelta());

			return profile;
		}
	}
}
